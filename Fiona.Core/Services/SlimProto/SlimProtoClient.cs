using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fiona.Core.Services.SlimProto
{
    /// <summary>
    /// Registers this machine with a Logitech Media Server as a player, and then does what the
    /// server tells it. Once connected the server lists it alongside any real hardware, so every
    /// LMS controller - Fiona included - can drive it with no further work.
    ///
    /// This speaks SlimProto on TCP 3483, a different protocol from the JSON-RPC the rest of the
    /// app uses for control. The framing is asymmetric, which is easy to get wrong:
    ///
    ///   server -> client:  [u16 length][4-byte opcode][payload]   length counts opcode + payload
    ///   client -> server:  [4-byte opcode][u32 length][payload]   length counts payload only
    ///
    /// Every multi-byte field is big-endian. The struct layouts in the comments below are taken
    /// field for field from squeezelite's slimproto.h, which is the reference implementation;
    /// the byte offsets are what matter, so check them against that file before changing any.
    ///
    /// Audio is not handled here - see <see cref="ILocalAudioSink"/>. The server hands us an HTTP
    /// request to fetch the audio with, and we pass a URL built from it to the sink.
    /// </summary>
    public class SlimProtoClient
    {
        public const int SlimProtoPort = 3483;

        // The server being briefly away is normal, so reconnecting stays quiet and keeps trying.
        private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

        // Nothing here is a real buffer: the sink owns its own buffering and will not say how big
        // it is. The server only uses these to judge whether we are keeping up, so a plausible
        // constant and a percentage of it answers the question well enough.
        private const uint NominalBufferSize = 3 * 1024 * 1024;

        private readonly string _serverHost;
        private readonly byte[] _mac;
        private readonly ILocalAudioSink _sink;
        private readonly string _firmwareVersion;

        // One writer at a time: the heartbeat, the sink's events and the command handlers all send
        // from different contexts, and a half-written packet would desynchronise the server.
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        // Monotonic milliseconds since this client started, which is what the server means by
        // "jiffies". The absolute value does not matter, only that it advances steadily.
        private readonly Stopwatch _jiffies = Stopwatch.StartNew();

        private TcpClient _tcp;
        private NetworkStream _stream;
        private CancellationTokenSource _run;

        // 0 = start as soon as we are ready, anything else = wait for the server's strm 'u'.
        // Taken from the autostart field of the strm that began the current track.
        private int _autostart;

        private string _playerName;

        public SlimProtoClient(string serverHost, byte[] mac, string playerName, string firmwareVersion, ILocalAudioSink sink)
        {
            if (mac == null || mac.Length != 6)
            {
                throw new ArgumentException("A MAC address is six bytes", nameof(mac));
            }

            if (serverHost == null) throw new ArgumentNullException(nameof(serverHost));
            if (sink == null) throw new ArgumentNullException(nameof(sink));

            _serverHost = serverHost;
            _mac = mac;
            _playerName = playerName;
            _firmwareVersion = firmwareVersion;
            _sink = sink;

            _sink.Started += OnSinkStarted;
            _sink.Ended += OnSinkEnded;
            _sink.Failed += OnSinkFailed;
        }

        /// <summary>
        /// The port the server's web interface is on, used to fetch audio when the server does not
        /// name one itself. Set from whatever address the app is already talking to.
        /// </summary>
        public int ServerWebPort { get; set; } = 9000;

        public bool IsConnected { get; private set; }

        /// <summary>Raised on connect and on disconnect, so the UI can say which it is.</summary>
        public event EventHandler ConnectionChanged;

        /// <summary>
        /// Renames the player. The server accepts a name pushed this way - it is how a software
        /// player reports the name it was configured with - so this takes effect without
        /// reconnecting. The server also asks for the name itself when we connect.
        /// </summary>
        public void SetPlayerName(string name)
        {
            _playerName = name;
            if (IsConnected) SendSetdName();
        }

        /// <summary>
        /// Connects, and keeps reconnecting until stopped. Returns immediately; everything after
        /// this happens on background tasks.
        /// </summary>
        public void Start()
        {
            if (_run != null) return;

            _run = new CancellationTokenSource();
            CancellationToken token = _run.Token;
            Task.Run(() => RunAsync(token));
        }

        /// <summary>
        /// Says goodbye properly, so the server drops the player from its list at once rather than
        /// leaving a dead entry behind until it times out.
        /// </summary>
        public async Task StopAsync()
        {
            CancellationTokenSource run = _run;
            _run = null;
            if (run == null) return;

            run.Cancel();

            try
            {
                if (_stream != null) await SendAsync("BYE!", new byte[] { 0 });
            }
            catch (Exception)
            {
                // The socket is on its way out regardless, so there is nothing to recover here.
            }

            CloseConnection();
            run.Dispose();
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            bool reconnect = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Cancelled when this particular connection ends, which is what stops the
                    // heartbeat. Tying the heartbeat to the outer token instead would leave it
                    // running against a dead socket until the whole client was stopped.
                    using (var connection = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        _tcp = new TcpClient();
                        await _tcp.ConnectAsync(_serverHost, SlimProtoPort);
                        _stream = _tcp.GetStream();

                        await SendHeloAsync(reconnect);
                        SetConnected(true);

                        Task heartbeat = HeartbeatAsync(connection.Token);
                        try
                        {
                            await ReadLoopAsync(connection.Token);
                        }
                        finally
                        {
                            connection.Cancel();
                        }

                        await heartbeat;
                    }
                }
                catch (Exception)
                {
                    // Refused, reset, server restarted: all handled the same way, by dropping
                    // everything and trying again in a moment.
                }

                SetConnected(false);
                CloseConnection();
                _sink.Stop();
                reconnect = true;

                if (cancellationToken.IsCancellationRequested) return;

                try
                {
                    await Task.Delay(ReconnectDelay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task ReadLoopAsync(CancellationToken cancellationToken)
        {
            var lengthPrefix = new byte[2];

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!await ReadExactlyAsync(lengthPrefix, 2, cancellationToken)) return;

                int length = BigEndian.ReadUInt16(lengthPrefix, 0);

                // Too short to carry even an opcode, which means we have lost our place in the
                // stream. Dropping the connection resynchronises; guessing would not.
                if (length < 4) return;

                var packet = new byte[length];
                if (!await ReadExactlyAsync(packet, length, cancellationToken)) return;

                Dispatch(Encoding.ASCII.GetString(packet, 0, 4), packet);
            }
        }

        private async Task<bool> ReadExactlyAsync(byte[] buffer, int count, CancellationToken cancellationToken)
        {
            int read = 0;
            while (read < count)
            {
                int got = await _stream.ReadAsync(buffer, read, count - read, cancellationToken);
                if (got <= 0) return false; // the server closed the connection
                read += got;
            }

            return true;
        }

        private void Dispatch(string opcode, byte[] packet)
        {
            switch (opcode)
            {
                case "strm": HandleStrm(packet); break;
                case "audg": HandleAudg(packet); break;
                case "aude": HandleAude(packet); break;
                case "setd": HandleSetd(packet); break;

                default:
                    // The rest is for hardware we are not pretending to be: display updates
                    // (grfe, grfb, visu), LEDs (ledc), firmware pushes (updn) and so on.
                    break;
            }
        }

        #region strm - the command that does nearly everything

        // struct strm_packet, offsets from the start of the packet, opcode included:
        //   0  opcode[4]          13 transition_period
        //   4  command            14 transition_type
        //   5  autostart          15 flags
        //   6  format             16 output_threshold
        //   7  pcm_sample_size    17 slaves
        //   8  pcm_sample_rate    18 replay_gain (u32)
        //   9  pcm_channels       22 server_port (u16)
        //  10  pcm_endianness     24 server_ip (u32)
        //  11  threshold          28 request string, to the end of the packet
        //  12  spdif_enable
        private const int StrmHeaderLength = 28;
        private const int StrmReplayGainOffset = 18;

        private void HandleStrm(byte[] packet)
        {
            if (packet.Length < StrmHeaderLength) return;

            switch ((char)packet[4])
            {
                case 's':
                    StartStream(packet);
                    break;

                case 'q':
                case 'f':
                    _sink.Stop();
                    SendStat("STMf");
                    break;

                case 'p':
                    // replay_gain doubles as the interval to pause for. Only an immediate pause is
                    // acknowledged, which is the case the server waits on.
                    _sink.Pause();
                    if (BigEndian.ReadUInt32(packet, StrmReplayGainOffset) == 0) SendStat("STMp");
                    break;

                case 'u':
                    _sink.Play();
                    SendStat("STMr");
                    break;

                case 't':
                    // A ping. replay_gain carries the server's own timestamp, which has to come
                    // back untouched or the server cannot measure the round trip.
                    SendStat("STMt", BigEndian.ReadUInt32(packet, StrmReplayGainOffset));
                    break;

                case 'a':
                    // Skip ahead inside the current track. Not supported: the server uses it to
                    // trim for sync, which we have already declined by way of AccuratePlayPoints=0.
                    break;
            }
        }

        private void StartStream(byte[] packet)
        {
            // The server expects the previous stream acknowledged as gone before a new one starts.
            SendStat("STMf");

            _autostart = packet[5] - '0';

            Uri stream = BuildStreamUri(packet);
            if (stream == null)
            {
                SendStat("STMn"); // nothing we can fetch, so do not leave the server waiting
                return;
            }

            _sink.Load(stream);
            SendStat("STMc");

            // Told to the server straight away rather than waiting for the sink to report itself
            // ready. The sink cannot: it will not finish opening a stream it has not been asked to
            // play, so waiting for it meant each side waiting on the other and nothing playing.
            //
            // Starting the clock a moment early is the cost, and a small one - the STMt heartbeat
            // reports our real position every second, which the server follows.
            if (_autostart == 0)
            {
                SendStat("STMl"); // the server decides when to start, and replies with strm 'u'
            }
            else
            {
                _sink.Play();
            }
        }

        /// <summary>
        /// Turns the HTTP request the server embedded in a strm into a URL the sink can fetch. The
        /// request looks like "GET /stream.mp3?player=xx HTTP/1.0" followed by headers we have no
        /// use for, since the sink makes its own request.
        /// </summary>
        private Uri BuildStreamUri(byte[] packet)
        {
            string request = Encoding.ASCII.GetString(packet, StrmHeaderLength, packet.Length - StrmHeaderLength);

            int endOfLine = request.IndexOf('\r');
            string requestLine = endOfLine >= 0 ? request.Substring(0, endOfLine) : request;

            string[] parts = requestLine.Split(' ');
            if (parts.Length < 2) return null;

            string target = parts[1];

            Uri absolute;
            if (target.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                && Uri.TryCreate(target, UriKind.Absolute, out absolute))
            {
                return absolute;
            }

            // A zero address means "the server you are already talking to".
            string host = _serverHost;
            if (packet[24] != 0 || packet[25] != 0 || packet[26] != 0 || packet[27] != 0)
            {
                host = packet[24] + "." + packet[25] + "." + packet[26] + "." + packet[27];
            }

            int port = BigEndian.ReadUInt16(packet, 22);
            if (port == 0) port = ServerWebPort;

            Uri uri;
            string url = "http://" + host + ":" + port.ToString() + target;
            return Uri.TryCreate(url, UriKind.Absolute, out uri) ? uri : null;
        }

        #endregion

        #region audg, aude, setd

        // struct audg_packet: 0 opcode[4], 4 old_gainL, 8 old_gainR, 12 adjust, 13 preamp,
        //                     14 gainL, 18 gainR. Gains are 16.16 fixed point, 65536 = unity.
        private void HandleAudg(byte[] packet)
        {
            if (packet.Length < 22) return;

            bool adjust = packet[12] != 0;
            if (!adjust)
            {
                _sink.SetVolume(1.0);
                return;
            }

            uint left = BigEndian.ReadUInt32(packet, 14);
            uint right = BigEndian.ReadUInt32(packet, 18);

            // One output, so the two channels collapse to the louder of the pair. That loses a
            // balance setting, which we have already told the server we do not have.
            double gain = Math.Max(left, right) / 65536.0;
            _sink.SetVolume(Math.Min(1.0, Math.Max(0.0, gain)));
        }

        // struct aude_packet: 0 opcode[4], 4 enable_spdif, 5 enable_dac.
        private void HandleAude(byte[] packet)
        {
            if (packet.Length < 6) return;

            // Either flag means "make sound". Reading only one of them is a trap: the reference
            // implementation keys off enable_spdif and ignores enable_dac, but we told the server
            // we have no digital out, so that is the flag it is entitled to leave clear.
            _sink.SetOutputEnabled(packet[4] != 0 || packet[5] != 0);
        }

        // struct setd_packet: 0 opcode[4], 4 id, 5 data. id 0 is the player name: with no data the
        // server is asking what we call ourselves, with data it is renaming us.
        private void HandleSetd(byte[] packet)
        {
            if (packet.Length < 5 || packet[4] != 0) return;

            if (packet.Length > 5)
            {
                string name = Encoding.UTF8.GetString(packet, 5, packet.Length - 5).TrimEnd('\0');
                if (!string.IsNullOrEmpty(name)) _playerName = name;
            }

            SendSetdName();
        }

        private void SendSetdName()
        {
            byte[] name = Encoding.UTF8.GetBytes(_playerName ?? string.Empty);

            var payload = new byte[1 + name.Length + 1]; // id, name, terminator
            payload[0] = 0;
            Array.Copy(name, 0, payload, 1, name.Length);

            FireAndForget(SendAsync("SETD", payload));
        }

        #endregion

        #region Sink events

        private void OnSinkStarted(object sender, EventArgs e)
        {
            SendStat("STMs");
        }

        private void OnSinkEnded(object sender, EventArgs e)
        {
            // Both are needed: STMd says the track has finished decoding, which is the server's
            // cue to hand us the next one, and STMu says we have actually run dry.
            SendStat("STMd");
            SendStat("STMu");
        }

        private void OnSinkFailed(object sender, EventArgs e)
        {
            SendStat("STMn");
        }

        #endregion

        #region HELO and STAT

        /// <summary>
        /// Announces the player and what it can play. The capability list is how the server decides
        /// whether to transcode: it converts anything not named here, so only formats the sink can
        /// actually decode belong in it.
        /// </summary>
        private async Task SendHeloAsync(bool reconnect)
        {
            // AccuratePlayPoints=0 is deliberate and load-bearing. Our timing comes from a media
            // pipeline we do not control, which is good enough for a progress bar but not for
            // multi-room sync; claiming otherwise makes the server sync against us and drift.
            //
            // The codec list is everything Windows can decode from a plain HTTP stream. Raw pcm is
            // left out on purpose: the server would send headerless samples, which the sink has no
            // container to make sense of. Anything else - ogg, opus, wma - the server transcodes.
            string capabilities =
                "Model=squeezelite,ModelName=Fiona,AccuratePlayPoints=0,HasDigitalOut=0,"
                + "HasPolarityInversion=0,Balance=0,Firmware=" + _firmwareVersion
                + ",MaxSampleRate=48000,mp3,flc,aac,alc";

            byte[] caps = Encoding.ASCII.GetBytes(capabilities);

            // struct HELO_packet is 44 bytes, so the payload is 36 plus the capability string:
            //   0  opcode[4]   10 mac[6]              34 bytes_received_H
            //   4  length      16 uuid[16]            38 bytes_received_L
            //   8  deviceid    32 wlan_channellist    42 lang[2]
            //   9  revision
            // Offsets below are payload-relative, so 8 less than the struct's.
            var payload = new byte[36 + caps.Length];

            payload[0] = 12; // deviceid 12 = squeezeplay, which is what software players use
            payload[1] = 0;  // revision
            Array.Copy(_mac, 0, payload, 2, 6);
            // uuid stays zero: the server does not require one
            BigEndian.Write(payload, 24, (ushort)(reconnect ? 0x4000 : 0x0000)); // wlan_channellist
            // bytes_received stays zero: no audio has been received on this connection
            payload[34] = (byte)'E';
            payload[35] = (byte)'N';
            Array.Copy(caps, 0, payload, 36, caps.Length);

            await SendAsync("HELO", payload);
        }

        private void SendStat(string eventCode, uint serverTimestamp = 0)
        {
            // struct STAT_packet. Offsets are payload-relative, so 8 less than the struct's:
            //   0  event[4]                 25 jiffies
            //   4  num_crlf                 29 output_buffer_size
            //   5  mas_initialized          33 output_buffer_fullness
            //   6  mas_mode                 37 elapsed_seconds
            //   7  stream_buffer_size       41 voltage
            //  11  stream_buffer_fullness   43 elapsed_milliseconds
            //  15  bytes_received_H         47 server_timestamp
            //  19  bytes_received_L         51 error_code
            //  23  signal_strength
            var payload = new byte[53];

            Encoding.ASCII.GetBytes(eventCode, 0, 4, payload, 0);

            double fraction = Math.Min(1.0, Math.Max(0.0, _sink.BufferFill));
            var fill = (uint)(NominalBufferSize * fraction);

            BigEndian.Write(payload, 7, NominalBufferSize);
            BigEndian.Write(payload, 11, fill);
            // bytes_received stays zero: the sink fetches the audio itself, so we never count it
            BigEndian.Write(payload, 23, (ushort)0xFFFF); // wired, so full signal strength
            BigEndian.Write(payload, 25, (uint)_jiffies.ElapsedMilliseconds);
            BigEndian.Write(payload, 29, NominalBufferSize);
            BigEndian.Write(payload, 33, fill);

            TimeSpan position = _sink.Position;
            BigEndian.Write(payload, 37, (uint)position.TotalSeconds);
            BigEndian.Write(payload, 43, (uint)position.TotalMilliseconds);
            BigEndian.Write(payload, 47, serverTimestamp);

            FireAndForget(SendAsync("STAT", payload));
        }

        /// <summary>
        /// A steady STMt is what keeps the server's progress bar and remaining time honest; it
        /// stops trusting our position if we go quiet.
        /// </summary>
        private async Task HeartbeatAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (_sink.IsPlaying) SendStat("STMt");
            }
        }

        #endregion

        private async Task SendAsync(string opcode, byte[] payload)
        {
            var packet = new byte[8 + payload.Length];
            Encoding.ASCII.GetBytes(opcode, 0, 4, packet, 0);
            BigEndian.Write(packet, 4, (uint)payload.Length);
            Array.Copy(payload, 0, packet, 8, payload.Length);

            await _sendLock.WaitAsync();
            try
            {
                NetworkStream stream = _stream;
                if (stream == null) return;

                await stream.WriteAsync(packet, 0, packet.Length);
                await stream.FlushAsync();
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// Status packets are sent from event handlers that cannot await. A send failing means the
        /// connection has gone, which the read loop is already about to notice and act on, so there
        /// is nothing useful to do with the exception beyond not letting it escape.
        /// </summary>
        private static void FireAndForget(Task send)
        {
            send.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void SetConnected(bool connected)
        {
            if (IsConnected == connected) return;

            IsConnected = connected;

            EventHandler handler = ConnectionChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void CloseConnection()
        {
            try { if (_stream != null) _stream.Dispose(); } catch (Exception) { }
            try { if (_tcp != null) _tcp.Dispose(); } catch (Exception) { }

            _stream = null;
            _tcp = null;
        }
    }
}
