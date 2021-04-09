using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fiona.Core.Services;
using Fiona.Helpers;
using Fiona.Services;

using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;

namespace Fiona
{
    public sealed partial class App : Application
    {
        private Lazy<ActivationService> _activationService;
        private string server;

        private ActivationService ActivationService
        {
            get { return _activationService.Value; }
        }

        public App()
        {
            InitializeComponent();

            AppCenter.Start(Fiona.Core.Helpers.APIKeys.AppCenter, typeof(Analytics), typeof(Crashes));
            UnhandledException += OnAppUnhandledException;

            // Get server and port from local settings
            server = Windows.Storage.ApplicationData.Current.LocalSettings.Values["ServerIP"]?.ToString();
            if (server != null)
                server = server.Replace("\"", "");

            int port = 9000;
            var portobject = Windows.Storage.ApplicationData.Current.LocalSettings.Values["ServerPort"];
            if (portobject != null)
                port = int.Parse(portobject.ToString());

            if (string.IsNullOrEmpty(server))
            { // likely the first run
                server = GetSlimServerIP().Result;
            }

            if (string.IsNullOrEmpty(server))
            {
                //TODO - we have a problem, let's not keep it a secret
                Application.Current.Exit();
            }

            FionaDataService.ServerIP = server;
            FionaDataService.ServerPort = port;

            // Save server and port to local settings
            Windows.Storage.ApplicationData.Current.LocalSettings.Values["ServerIP"] = server;
            Windows.Storage.ApplicationData.Current.LocalSettings.Values["ServerPort"] = port;

            //HACK - is this the right place to load ALL the large data?
            FionaDataService.GetAllAlbums();
            FionaDataService.GetAllArtists();

            // Deferred execution until used. Check https://docs.microsoft.com/dotnet/api/system.lazy-1 for further info on Lazy<T> class.
            _activationService = new Lazy<ActivationService>(CreateActivationService);
        }

        private async Task<string> GetSlimServerIP()
        {
            PortSweep ps = new PortSweep();
            await ps.RunPortSweep_Async();
            string s = ps.GetServer();
            return s;
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (!args.PrelaunchActivated)
            {
                await ActivationService.ActivateAsync(args);
            }
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            await ActivationService.ActivateAsync(args);
        }

        private void OnAppUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // TODO WTS: Please log and handle the exception as appropriate to your scenario
            // For more info see https://docs.microsoft.com/uwp/api/windows.ui.xaml.application.unhandledexception
        }

        private ActivationService CreateActivationService()
        {
            return new ActivationService(this, typeof(Views.AlbumsPage), new Lazy<UIElement>(CreateShell));
        }

        private UIElement CreateShell()
        {
            return new Views.ShellPage();
        }


    }

    public class PortSweep
    {
        private string BaseIP = "192.168.1.";
        private int StartIP = 1;
        private int StopIP = 255;
        private string ip;

        private int timeout = 50;
        private int slimserverport = 3483;

        private string slimServer = "";

        public string GetServer()
        {
            return slimServer;
        }

        public async Task RunPortSweep_Async()
        {
            var tasks = new List<Task>();

            // find the LAN 
            string localip = GetLocalIPAddress();
            BaseIP = localip.Substring(0, localip.LastIndexOf('.') + 1);

            for (int i = StartIP; i <= StopIP; i++)
            {
                ip = BaseIP + i.ToString();
                var task = CheckPort_Async(ip, slimserverport, timeout);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        private async Task CheckPort_Async(string ip, int port, int timeout)
        {
            bool b = IsPortOpen(ip, port, TimeSpan.FromMilliseconds(timeout));
            if (b) Interlocked.Exchange<string>(ref slimServer, ip);
        }

        private string GetLocalIPAddress()
        {
            string localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
            return localIP;
        }

        private bool IsPortOpen(string host, int port, TimeSpan timeout)
        {
            TcpClient client = null;
            bool result = false;

            try
            {
                client = new TcpClient();
                Task task = client.ConnectAsync(host, port);
                if (task.Wait(timeout))
                {//if fails within timeout, task.Wait still returns true.
                    if (client.Connected)
                    {
                        // port reachable
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // connection failed
                result = false;
            }
            finally
            {
                client.Close();
            }

            return result;
        }

    }
}
