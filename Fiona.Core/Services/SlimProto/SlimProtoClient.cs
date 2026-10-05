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
    /// app uses for control, and a binary one. The packet layouts live in
    /// <see cref="SlimProtoPackets"/>, which is where to look before changing any of them.
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

        private void HandleStrm(byte[] packet)
        {
            if (packet.Length < SlimProtoPackets.StrmHeaderLength) return;

            switch (SlimProtoPackets.StrmCommand(packet))
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
                    if (SlimProtoPackets.StrmReplayGain(packet) == 0) SendStat("STMp");
                    break;

                case 'u':
                    _sink.Play();
                    SendStat("STMr");
                    break;

                case 't':
                    // A ping. replay_gain carries the server's own timestamp, which has to come
                    // back untouched or the server cannot measure the round trip.
                    SendStat("STMt", SlimProtoPackets.StrmReplayGain(packet));
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

            _autostart = SlimProtoPackets.StrmAutostart(packet);

            Uri stream = SlimProtoPackets.StreamUri(packet, _serverHost, ServerWebPort);
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

        #endregion

        #region audg, aude, setd

        private void HandleAudg(byte[] packet)
        {
            _sink.SetVolume(SlimProtoPackets.AudgVolume(packet));
        }

        private void HandleAude(byte[] packet)
        {
            _sink.SetOutputEnabled(SlimProtoPackets.AudeOutputEnabled(packet));
        }

        /// <summary>
        /// The server either asks what we are called or tells us what to be called; a name it sends
        /// is kept, and either way it wants the answer back.
        /// </summary>
        private void HandleSetd(byte[] packet)
        {
            string name;
            if (!SlimProtoPackets.TryReadSetdName(packet, out name)) return;

            if (name != null) _playerName = name;

            SendSetdName();
        }

        private void SendSetdName()
        {
            FireAndForget(SendAsync("SETD", SlimProtoPackets.SetdName(_playerName)));
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

            await SendAsync("HELO", SlimProtoPackets.Helo(_mac, reconnect, capabilities));
        }

        private void SendStat(string eventCode, uint serverTimestamp = 0)
        {
            double fraction = Math.Min(1.0, Math.Max(0.0, _sink.BufferFill));
            var fill = (uint)(NominalBufferSize * fraction);

            byte[] payload = SlimProtoPackets.Stat(
                eventCode,
                NominalBufferSize,
                fill,
                (uint)_jiffies.ElapsedMilliseconds,
                _sink.Position,
                serverTimestamp);

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
            byte[] packet = SlimProtoPackets.Frame(opcode, payload);

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
