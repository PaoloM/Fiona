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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Fiona
{
    public sealed partial class App : Application
    {
        private const string ServerIPSetting = "ServerIP";
        private const string ServerPortSetting = "ServerPort";
        private const int DefaultServerPort = 9000;

        // Spent before the window opens, so it has to stay well inside the few seconds
        // Windows allows for activation. A server on the LAN answers in well under this.
        private static readonly TimeSpan StartupProbeTimeout = TimeSpan.FromSeconds(1.5);

        // Once the window is up we can afford to wait properly on a server that is merely busy.
        private static readonly TimeSpan ServerProbeTimeout = TimeSpan.FromSeconds(5);

        private Lazy<ActivationService> _activationService;

        // Set when startup finished without a server to talk to, so the window can come up
        // first and ask for one instead of the app disappearing without explanation.
        private bool _serverUnresolved;

        // The address we tried and could not reach, so the prompt can open on it.
        private string _promptSeedServer;
        private int _promptSeedPort = DefaultServerPort;

        private ActivationService ActivationService
        {
            get { return _activationService.Value; }
        }

        public App()
        {
            InitializeComponent();

            UnhandledException += OnAppUnhandledException;

            // A task nobody awaited must not take the process down. Reported rather than
            // merely swallowed, so these still show up somewhere we can see them.
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Crashes.TrackError(e.Exception);
                e.SetObserved();
            };

            StartAppCenter();

            // Deferred execution until used. Check https://docs.microsoft.com/dotnet/api/system.lazy-1 for further info on Lazy<T> class.
            _activationService = new Lazy<ActivationService>(CreateActivationService);
        }

        // Kept out of the constructor and un-inlined on purpose: referencing AppCenter and
        // Fiona.Core from the constructor means the runtime has to resolve both before the
        // constructor's first line runs, so a load failure there takes the app down before
        // any of our own code has had a chance to run.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void StartAppCenter()
        {
            try
            {
                AppCenter.Start(Fiona.Core.Helpers.APIKeys.AppCenter, typeof(Analytics), typeof(Crashes));
            }
            catch (Exception)
            {
                // Telemetry is not worth failing startup over, and there is nowhere to
                // report a reporting failure to.
            }
        }

        private static void LoadSavedServer(out string server, out int port)
        {
            // Get server and port from local settings
            server = ApplicationData.Current.LocalSettings.Values[ServerIPSetting]?.ToString();
            if (server != null)
                server = server.Replace("\"", "");

            port = DefaultServerPort;
            var portobject = ApplicationData.Current.LocalSettings.Values[ServerPortSetting];
            int savedPort;
            if (portobject != null && int.TryParse(portobject.ToString(), out savedPort))
                port = savedPort;
        }

        /// <summary>
        /// Settles on a server before the window opens, but only by way of work that is
        /// certain to be quick: the address given on the command line, or the one we used
        /// last time, each checked against a short timeout.
        ///
        /// Anything slower - the subnet sweep above all - is left to
        /// <see cref="ResolveServerInteractivelyAsync"/> once the window is up. Windows
        /// terminates an app that has not activated its window within a few seconds, so
        /// nothing open-ended may happen on this path.
        /// </summary>
        private async Task ResolveServerAsync(string commandLineServer, int? commandLinePort)
        {
            string server;
            int port;
            LoadSavedServer(out server, out port);

            if (commandLinePort.HasValue)
                port = commandLinePort.Value;

            // Overriding discovery is the whole point of -s, so it beats the saved address.
            // We still check it answers rather than saving a typo for good.
            if (!string.IsNullOrEmpty(commandLineServer))
                server = commandLineServer;

            _promptSeedPort = port;

            if (string.IsNullOrEmpty(server))
            {
                _serverUnresolved = true;
                return;
            }

            // A saved address goes stale as soon as the server moves or is switched off, and
            // trusting it blindly is what leaves the app sitting on an empty library.
            if (!await FionaDataService.IsServerReachableAsync(server, port, StartupProbeTimeout))
            {
                _promptSeedServer = server;
                _serverUnresolved = true;
                return;
            }

            await SetAndSaveServerAsync(server, port);
        }

        private static async Task SetAndSaveServerAsync(string server, int port)
        {
            if (string.IsNullOrEmpty(server))
                return;

            FionaDataService.ServerIP = server;
            FionaDataService.ServerPort = port;

            // Anything cached belongs to whichever server we were talking to before.
            FionaDataService.InvalidateLibrary();

            // Save server and port to local settings
            ApplicationData.Current.LocalSettings.Values[ServerIPSetting] = server;
            ApplicationData.Current.LocalSettings.Values[ServerPortSetting] = port;

            //HACK - is this the right place to load ALL the large data?
            // These are blocking web calls, so they stay off the UI thread: on a slow server
            // they used to freeze activation for long enough for Windows to kill the app.
            await Task.Run(() =>
            {
                FionaDataService.GetAllAlbums();
                FionaDataService.GetAllArtists();
                FionaDataService.GetAllFavorites();
            });

            // A player belongs to one server at a time, so if this is a change of server the old
            // registration has to be given up before the new one is made.
            await LocalPlayerService.RestartAsync();
        }

        private async Task<string> GetSlimServerIPAsync()
        {
            PortSweep ps = new PortSweep();
            await ps.RunPortSweep_Async();
            return ps.GetServer();
        }

        private static void ParseCommandLine(IActivatedEventArgs args, out string server, out int? port)
        {
            server = null;
            port = null;

            var commandLine = args as CommandLineActivatedEventArgs;
            if (commandLine == null)
                return;

            var arguments = commandLine.Operation.Arguments;
            if (string.IsNullOrEmpty(arguments))
                return;

            string[] argsList = arguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Stop one short of the end: a switch with nothing after it has no value to read.
            for (int i = 0; i < argsList.Length - 1; i++)
            {
                switch (argsList[i].ToLower())
                {
                    case "-s":
                        server = argsList[i + 1];
                        break;

                    case "-p":
                        int parsedPort;
                        if (int.TryParse(argsList[i + 1], out parsedPort))
                            port = parsedPort;
                        break;
                }
            }
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            await ResolveServerAsync(null, null);
            await ActivationService.ActivateAsync(args);
            await ResolveServerInteractivelyAsync();
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            // Read the command line before anything else: on a first run discovery would
            // otherwise run, and fail, without ever looking at the -s we were given.
            string commandLineServer;
            int? commandLinePort;
            ParseCommandLine(args, out commandLineServer, out commandLinePort);

            await ResolveServerAsync(commandLineServer, commandLinePort);
            await ActivationService.ActivateAsync(args);
            await ResolveServerInteractivelyAsync();
        }

        /// <summary>
        /// Everything slow about finding a server: the subnet sweep, and then asking the user
        /// if that comes up empty. Runs with the window already on screen, so it is free to
        /// take as long as it takes. Keeps asking until we can reach a server, or until the
        /// user gives up and closes the app.
        /// </summary>
        private async Task ResolveServerInteractivelyAsync()
        {
            if (!_serverUnresolved)
                return;

            int port = _promptSeedPort;

            string found = await GetSlimServerIPAsync();

            // The sweep only proves something is listening on the slimproto port, so make sure
            // it also serves the web API we actually talk to.
            if (await TryUseServerAsync(found, port))
                return;

            string message = "ServerPrompt_Message".GetLocalized();

            while (_serverUnresolved)
            {
                // Open on whatever we last tried, so a near miss only needs a character fixing.
                string seed = _promptSeedServer ?? string.Empty;

                var input = new TextBox
                {
                    PlaceholderText = "ServerPrompt_Placeholder".GetLocalized(),
                    Text = seed
                };

                var panel = new StackPanel();
                panel.Children.Add(new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12)
                });
                panel.Children.Add(input);

                var dialog = new ContentDialog
                {
                    Title = "ServerPrompt_Title".GetLocalized(),
                    Content = panel,
                    PrimaryButtonText = "ServerPrompt_Connect".GetLocalized(),
                    SecondaryButtonText = "ServerPrompt_SearchAgain".GetLocalized(),
                    CloseButtonText = "ServerPrompt_Close".GetLocalized(),
                    DefaultButton = ContentDialogButton.Primary
                };

                ContentDialogResult result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    string entered;
                    ParseServerAddress(input.Text, port, out entered, out port);

                    // Keep what they typed, port and all, in front of them on a retry.
                    if (!string.IsNullOrEmpty(entered))
                        _promptSeedServer = input.Text.Trim();

                    if (await TryUseServerAsync(entered, port))
                        return;

                    message = "ServerPrompt_NotReachable".GetLocalized();
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    string retry = await GetSlimServerIPAsync();

                    if (await TryUseServerAsync(retry, port))
                        return;

                    message = "ServerPrompt_NotFound".GetLocalized();
                }
                else
                {
                    Exit();
                    return;
                }
            }
        }

        /// <summary>
        /// Splits a typed address into host and port, so a server on a web port other than
        /// the usual one can be reached by entering "host:port".
        /// </summary>
        private static void ParseServerAddress(string text, int fallbackPort, out string server, out int port)
        {
            server = text == null ? string.Empty : text.Trim();
            port = fallbackPort;

            int separator = server.LastIndexOf(':');
            if (separator <= 0)
                return;

            int parsedPort;
            if (int.TryParse(server.Substring(separator + 1), out parsedPort))
            {
                port = parsedPort;
                server = server.Substring(0, separator);
            }
        }

        private async Task<bool> TryUseServerAsync(string server, int port)
        {
            if (!await FionaDataService.IsServerReachableAsync(server, port, ServerProbeTimeout))
                return false;

            await SetAndSaveServerAsync(server, port);
            _serverUnresolved = false;
            _promptSeedServer = null;

            // The first page has already loaded against no server, so send it round again.
            NavigationService.Frame?.Navigate(typeof(Views.AlbumsPage));

            // The shell sits outside that frame, so navigating does not touch it: without
            // this its player picker keeps the empty list it read before we had a server.
            (Window.Current?.Content as Views.ShellPage)?.ViewModel.RefreshPlayers();
            return true;
        }

        private void OnAppUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                Crashes.TrackError(e.Exception);
            }

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
