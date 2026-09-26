using System;
using System.Threading.Tasks;

using Fiona.Core.Services;
using Fiona.Core.Services.SlimProto;

using Windows.ApplicationModel;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;

namespace Fiona.Services
{
    /// <summary>
    /// Turns this machine into a Squeezebox player on the server Fiona is connected to.
    ///
    /// Everything protocol-shaped lives in <see cref="SlimProtoClient"/> and everything audio-shaped
    /// in <see cref="MediaPlayerAudioSink"/>; what is left here is the part that has to know about
    /// this app - the saved settings, the player's identity, and when to start and stop.
    ///
    /// Once running, the server lists the player like any other, so it turns up in Fiona's own
    /// picker through the existing <see cref="FionaDataService.GetAllPlayers"/> path with no UI work
    /// beyond the settings toggle.
    /// </summary>
    public static class LocalPlayerService
    {
        private const string EnabledSetting = "LocalPlayerEnabled";
        private const string NameSetting = "LocalPlayerName";
        private const string MacSetting = "LocalPlayerMac";

        private static SlimProtoClient _client;
        private static MediaPlayerAudioSink _sink;

        /// <summary>Raised when the player connects or disconnects.</summary>
        public static event EventHandler ConnectionChanged;

        public static bool IsConnected
        {
            get { return _client != null && _client.IsConnected; }
        }

        public static bool IsEnabled
        {
            get
            {
                object value = ApplicationData.Current.LocalSettings.Values[EnabledSetting];
                return value is bool && (bool)value;
            }
        }

        /// <summary>
        /// What the player is called in the server's player list. The server is free to rename it -
        /// any LMS controller can - and the new name is saved here when it does.
        /// </summary>
        public static string PlayerName
        {
            get
            {
                var saved = ApplicationData.Current.LocalSettings.Values[NameSetting] as string;
                return string.IsNullOrEmpty(saved) ? DefaultPlayerName() : saved;
            }

            set
            {
                ApplicationData.Current.LocalSettings.Values[NameSetting] = value;

                // Pushed straight to the server rather than waiting for a reconnect, so a rename
                // shows up in every controller as soon as it is typed.
                SlimProtoClient client = _client;
                if (client != null) client.SetPlayerName(value);
            }
        }

        /// <summary>
        /// Turns the local player on or off and remembers the choice. Switching it off says goodbye
        /// to the server, so the player disappears from the list immediately rather than lingering
        /// as a dead entry.
        /// </summary>
        public static async Task SetEnabledAsync(bool enabled)
        {
            ApplicationData.Current.LocalSettings.Values[EnabledSetting] = enabled;

            if (enabled)
            {
                Start();
            }
            else
            {
                await StopAsync();
            }
        }

        /// <summary>
        /// Starts the player if it is switched on and there is a server to register with. Safe to
        /// call whenever either of those might have changed; it does nothing if already running.
        /// </summary>
        public static void Start()
        {
            if (!IsEnabled) return;
            if (string.IsNullOrEmpty(FionaDataService.ServerIP)) return;
            if (_client != null) return;

            _sink = new MediaPlayerAudioSink();
            _client = new SlimProtoClient(
                FionaDataService.ServerIP,
                GetOrCreateMac(),
                PlayerName,
                AppVersion(),
                _sink);

            // The server tells us which port to fetch audio from, but not always, so the one the
            // app is already using is the fallback.
            _client.ServerWebPort = FionaDataService.ServerPort;
            _client.ConnectionChanged += OnClientConnectionChanged;

            _client.Start();
        }

        public static async Task StopAsync()
        {
            SlimProtoClient client = _client;
            MediaPlayerAudioSink sink = _sink;

            _client = null;
            _sink = null;

            if (client != null)
            {
                client.ConnectionChanged -= OnClientConnectionChanged;
                await client.StopAsync();
            }

            if (sink != null) sink.Dispose();

            RaiseConnectionChanged();
        }

        /// <summary>
        /// Reconnects to whichever server the app is now talking to. The player belongs to one
        /// server at a time, so changing server means leaving the old one properly first.
        /// </summary>
        public static async Task RestartAsync()
        {
            await StopAsync();
            Start();
        }

        private static void OnClientConnectionChanged(object sender, EventArgs e)
        {
            RaiseConnectionChanged();
        }

        private static void RaiseConnectionChanged()
        {
            EventHandler handler = ConnectionChanged;
            if (handler != null) handler(null, EventArgs.Empty);
        }

        /// <summary>
        /// A player is identified by a MAC address, and UWP will not hand out the real one. A made
        /// up address works as well - the server only needs it to be unique and stable - so one is
        /// generated once and kept. Changing it would register as a second, separate player and
        /// leave the first behind in the server's list.
        /// </summary>
        private static byte[] GetOrCreateMac()
        {
            var saved = ApplicationData.Current.LocalSettings.Values[MacSetting] as string;
            if (!string.IsNullOrEmpty(saved))
            {
                byte[] parsed = ParseMac(saved);
                if (parsed != null) return parsed;
            }

            var mac = new byte[6];
            new Random().NextBytes(mac);

            // Mark it locally administered and not a multicast address, which is what the standard
            // reserves for addresses that were never assigned to real hardware.
            mac[0] = (byte)((mac[0] | 0x02) & 0xFE);

            ApplicationData.Current.LocalSettings.Values[MacSetting] = FormatMac(mac);
            return mac;
        }

        private static byte[] ParseMac(string text)
        {
            string[] parts = text.Split(':');
            if (parts.Length != 6) return null;

            var mac = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                byte value;
                if (!byte.TryParse(parts[i], System.Globalization.NumberStyles.HexNumber, null, out value))
                {
                    return null;
                }

                mac[i] = value;
            }

            return mac;
        }

        private static string FormatMac(byte[] mac)
        {
            return string.Join(":", mac[0].ToString("x2"), mac[1].ToString("x2"), mac[2].ToString("x2"),
                                    mac[3].ToString("x2"), mac[4].ToString("x2"), mac[5].ToString("x2"));
        }

        private static string DefaultPlayerName()
        {
            try
            {
                // The machine's friendly name is what people will recognise in a list of players.
                var device = new EasClientDeviceInformation();
                if (!string.IsNullOrEmpty(device.FriendlyName)) return device.FriendlyName;
            }
            catch (Exception)
            {
                // Falls through to something generic rather than failing to name the player at all.
            }

            return "Fiona";
        }

        private static string AppVersion()
        {
            PackageVersion version = Package.Current.Id.Version;
            return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }
    }
}
