//** LICENSE **********************************************
//
// Copyright (c) 2021 Paolo Marcucci. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using Fiona.Core.Services;
using Fiona.Helpers;
using Fiona.Services;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Threading.Tasks;
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

            // Deferred execution until used. Check https://docs.microsoft.com/dotnet/api/system.lazy-1 for further info on Lazy<T> class.
            _activationService = new Lazy<ActivationService>(CreateActivationService);
        }

        private void LoadState(out string server, out int port)
        {
            // Get server and port from local settings
            server = Windows.Storage.ApplicationData.Current.LocalSettings.Values["ServerIP"]?.ToString();
            if (server != null)
                server = server.Replace("\"", "");

            port = 9000;
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
        }

        private static void SetAndSaveServer(string server, int port)
        {
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
            string server;
            int port;

            LoadState(out server, out port);
            SetAndSaveServer(server, port);

            await ActivationService.ActivateAsync(args);          
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            string server;
            int port;

            LoadState(out server, out port);

            if (args.Kind == ActivationKind.CommandLineLaunch)
            {
                var commandLine = args as CommandLineActivatedEventArgs;
                if (commandLine != null)
                {
                    var operation = commandLine.Operation;
                    var arguments = operation.Arguments;
                    string[] argsList = arguments.Split(' ');
                    for (int i = 0; i < argsList.Length; i++)
                    {
                        if (argsList[i].ToLower() == "-s")
                        {
                            server = argsList[i + 1];
                        }

                        if (argsList[i].ToLower() == "-p")
                        {
                            port = int.Parse(argsList[i + 1]);
                        }
                    }
                }
            }

            SetAndSaveServer(server, port);

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
