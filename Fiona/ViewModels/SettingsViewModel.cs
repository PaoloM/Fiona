using System;
using System.Threading.Tasks;
using System.Windows.Input;

using Fiona.Helpers;
using Fiona.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Windows.ApplicationModel;
using Windows.UI.Xaml;

namespace Fiona.ViewModels
{
    // TODO WTS: Add other settings as necessary. For help see https://github.com/Microsoft/WindowsTemplateStudio/blob/release/docs/UWP/pages/settings.md
    public class SettingsViewModel : ObservableObject
    {
        private ElementTheme _elementTheme = ThemeSelectorService.Theme;

        public ElementTheme ElementTheme
        {
            get { return _elementTheme; }

            set { SetProperty(ref _elementTheme, value); }
        }

        private string _versionDescription;

        public string VersionDescription
        {
            get { return _versionDescription; }

            set { SetProperty(ref _versionDescription, value); }
        }

        private ICommand _switchThemeCommand;

        public ICommand SwitchThemeCommand
        {
            get
            {
                if (_switchThemeCommand == null)
                {
                    _switchThemeCommand = new RelayCommand<ElementTheme>(
                        async (param) =>
                        {
                            ElementTheme = param;
                            await ThemeSelectorService.SetThemeAsync(param);
                        });
                }

                return _switchThemeCommand;
            }
        }

        private bool _isLocalPlayerEnabled;

        /// <summary>
        /// Whether this machine registers itself with the server as a player. Toggling it connects
        /// or disconnects there and then, so the player appears in - or leaves - every controller's
        /// list without restarting anything.
        /// </summary>
        public bool IsLocalPlayerEnabled
        {
            get { return _isLocalPlayerEnabled; }

            set
            {
                if (_isLocalPlayerEnabled == value) return;

                SetProperty(ref _isLocalPlayerEnabled, value);

                // Nothing here waits on the connection: registering takes a round trip, and the
                // toggle should not sit there looking stuck while it happens.
                Task ignored = LocalPlayerService.SetEnabledAsync(value);
            }
        }

        private string _localPlayerName;

        public string LocalPlayerName
        {
            get { return _localPlayerName; }

            set
            {
                if (_localPlayerName == value) return;

                SetProperty(ref _localPlayerName, value);

                // Empty would leave the player nameless in the list, so the machine name stands in.
                if (!string.IsNullOrWhiteSpace(value)) LocalPlayerService.PlayerName = value;
            }
        }

        public SettingsViewModel()
        {
        }

        public async Task InitializeAsync()
        {
            VersionDescription = GetVersionDescription();

            // Straight to the fields: going through the properties would read as a change made here
            // and push the saved values back at the player as though they were new.
            _isLocalPlayerEnabled = LocalPlayerService.IsEnabled;
            _localPlayerName = LocalPlayerService.PlayerName;
            OnPropertyChanged(nameof(IsLocalPlayerEnabled));
            OnPropertyChanged(nameof(LocalPlayerName));

            await Task.CompletedTask;
        }

        private string GetVersionDescription()
        {
            var appName = "AppDisplayName".GetLocalized();
            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;

            return $"{appName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }
}
