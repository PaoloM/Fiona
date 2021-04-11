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

        private ActivationService ActivationService
        {
            get { return _activationService.Value; }
        }

        public App()
        {
            InitializeComponent();

            AppCenter.Start(Fiona.Core.Helpers.APIKeys.AppCenter, typeof(Analytics), typeof(Crashes));
            UnhandledException += OnAppUnhandledException;

            LoadState();

            // Deferred execution until used. Check https://docs.microsoft.com/dotnet/api/system.lazy-1 for further info on Lazy<T> class.
            _activationService = new Lazy<ActivationService>(CreateActivationService);
        }

        private void LoadState()
        {
            // Get server and port from local settings
            string server = Windows.Storage.ApplicationData.Current.LocalSettings.Values["ServerIP"]?.ToString();
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
}
