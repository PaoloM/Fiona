using System;
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

            //String server = Windows.Storage.ApplicationData.Current.LocalSettings.Values["ServerIP"]?.ToString();
            //if (server != null)
            //    server = server.Replace("\"", "");

            //int port = 9000;
            //var portobject = Windows.Storage.ApplicationData.Current.LocalSettings.Values["ServerPort"];
            //if (portobject != null)
            //    port = int.Parse(portobject.ToString());

            //FionaDataService.ServerIP = server;
            //FionaDataService.ServerPort = port;

            //HACK - should retrieve these from LocalSettings and - if not valid - do not proceed with the initialization
            FionaDataService.ServerIP = Fiona.Core.Helpers.APIKeys.LMSServerName;
            FionaDataService.ServerPort = Fiona.Core.Helpers.APIKeys.LMSServerPort;

            //HACK - is this the right place to load ALL the large data?
            FionaDataService.GetAllAlbums();
            FionaDataService.GetAllArtists();

            // Deferred execution until used. Check https://docs.microsoft.com/dotnet/api/system.lazy-1 for further info on Lazy<T> class.
            _activationService = new Lazy<ActivationService>(CreateActivationService);
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
