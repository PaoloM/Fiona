using Fiona.Core.Helpers;
using Fiona.Core.Models;
using Fiona.Core.Services;
using Fiona.Helpers;
using Fiona.Services;
using Fiona.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace Fiona.ViewModels
{
    public class ShellViewModel : BaseViewModel
    {
        private PlayerStatus _CurrentPlayerStatus;
        public PlayerStatus CurrentPlayerStatus
        {
            get => _CurrentPlayerStatus;
            set => SetProperty(ref _CurrentPlayerStatus, value);
        }

        private List<Track> _Queue = new List<Track>();
        public List<Track> Queue
        {
            get => _Queue;
            set => SetProperty(ref _Queue, value);
        }

        public PlayerList PlayersList
        {
            get
            {
                var ap = FionaDataService.GetAllPlayers();
                if ((ap != null) && (ap.Players.Count > 0)) // if there are players
                {
                    Player cp = null;
                    foreach (Player p in ap.Players)
                    {
                        if (p.IsPlaying) // pick the one who is playing now
                        {
                            cp = p;
                        }
                    }
                    if (cp is null) cp = ap.Players[0]; // or pick the first one if no active ones
                    //TODO maybe provide a "preferred player" selection in Settings

                    CurrentPlayer = cp;
                    DispatcherTimerSetup();
                }
                return ap;
            }
        }

        public Player CurrentPlayer
        {
            get => FionaDataService.CurrentPlayer;
            set => FionaDataService.CurrentPlayer = value;
        }

        private string _nowPlayingAlbumArt;
        public string NowPlayingAlbumArt
        {
            get => string.IsNullOrEmpty(_nowPlayingAlbumArt) ? FionaDataService.DefaultArtworkUrl : _nowPlayingAlbumArt;
            set => SetProperty(ref _nowPlayingAlbumArt, value);
        }

        private string _isPlayingGlyph;
        public string IsPlayingGlyph
        {
            get => _isPlayingGlyph;
            set => SetProperty(ref _isPlayingGlyph, value);
        }

        private bool _IsMuted;
        public bool IsMuted
        {
            get => _IsMuted;
            set => SetProperty(ref _IsMuted, value);
        }

        private Visibility _NowPlayingPageVisibility = Visibility.Collapsed;
        public Visibility NowPlayingPageVisibility
        {
            get => _NowPlayingPageVisibility;
            set => SetProperty(ref _NowPlayingPageVisibility, value);
        }

        private string _artistBio = "";
        public string ArtistBio
        {
            get => _artistBio;
            set => SetProperty(ref _artistBio, value);
        }

        private string _artistImageUrl = "";
        public string ArtistImageUrl
        {
            get => string.IsNullOrEmpty(_artistImageUrl) ? FionaDataService.DefaultArtworkUrl : _artistImageUrl;
            set => SetProperty(ref _artistImageUrl, value);
        }

        private DispatcherTimer dispatcherTimer;

        public void DispatcherTimerSetup()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        void dispatcherTimer_Tick(object sender, object e)
        {
            CurrentPlayerStatus = FionaDataService.GetPlayerStatus(this.CurrentPlayer);

            if (CurrentPlayerStatus.Mode == PlayerMode.play)
                IsPlayingGlyph = "\xE103";
            else
                IsPlayingGlyph = "\xE768";

            IsMuted = CurrentPlayerStatus.IsMuted;

            if (CurrentPlayerStatus.CurrentSong != null)
            {
                if (NowPlayingAlbumArt != CurrentPlayerStatus.CurrentSong?.ArtworkUrl)
                {
                    NowPlayingAlbumArt = string.IsNullOrEmpty(CurrentPlayerStatus.CurrentSong.ArtworkUrl) ? FionaDataService.DefaultArtworkUrl : CurrentPlayerStatus.CurrentSong.ArtworkUrl;


                    if (CurrentPlayerStatus.CurrentSong.Artist != null)
                    {
                        string ca = CurrentPlayerStatus.CurrentSong.Artist;
                        if (ca.IndexOf(',') > 0)
                            ca = ca.Substring(0, ca.IndexOf(',')); // if there is a comma, take the first artist

                        var aa = (from a in FionaDataService.AllArtists.Artists where a.Name == ca select a);
                        if (aa.Count<Artist>() > 0)
                        {
                            var artist = aa.First<Artist>();
                            if (string.IsNullOrEmpty(artist.Profile))
                            {
                                DiscogsArtist da = DiscogsDataService.GetArtistInfo(artist.Name);
                                if (da != null)
                                {
                                    artist.Profile = da.Profile;
                                    artist.Images = new List<string>();
                                    if (da.Images.Count > 0)
                                    {
                                        foreach (var i in da.Images)
                                            artist.Images.Add(i.ImageUrl);
                                    }
                                }
                            }

                            ArtistBio = artist.Profile;
                            ArtistImageUrl = artist.Images.Count > 0 ? artist.Images[0] : FionaDataService.DefaultArtworkUrl;
                            
                        }
                    }
                }
            }

            //TODO change the queue even if it has the same number of entries as the current playlist
            if (CurrentPlayerStatus.Playlist != null) {
                if (CurrentPlayerStatus.Playlist.Count > 0)
                {
                    if ((CurrentPlayerStatus.Playlist.Count - CurrentPlayerStatus.PlaylistCurrentIndex) != Queue?.Count)
                    {
                        //if (Queue.Count > 0)
                        {
                            //    if (CurrentPlayerStatus.Playlist[0].ID != Queue?[0].ID)
                            //        Queue = CurrentPlayerStatus.Playlist.Skip(CurrentPlayerStatus.PlaylistCurrentIndex).ToList<Track>();
                            //} else
                            //{
                            Queue = CurrentPlayerStatus.Playlist.Skip(CurrentPlayerStatus.PlaylistCurrentIndex).ToList<Track>();
                        }
                    }
                }
            }
        }

        public RelayCommand TransportPauseCommand { get; set; }
        public RelayCommand TransportPreviousTrackCommand { get; set; }
        public RelayCommand TransportNextTrackCommand { get; set; }
        public RelayCommand TransportToggleRepeatCommand { get; set; }
        public RelayCommand TransportToggleShuffleCommand { get; set; }
        public RelayCommand ToggleMuteCommand { get; set; }
        public RelayCommand ShowNowPlayingCommand { get; set; }
        public RelayCommand HideNowPlayingCommand { get; set; }

        public ShellViewModel()
        {
            TransportPauseCommand = new RelayCommand(() => FionaDataService.TransportPause(CurrentPlayer));
            TransportPreviousTrackCommand = new RelayCommand(() => FionaDataService.TransportPrevious(CurrentPlayer));
            TransportNextTrackCommand = new RelayCommand(() => FionaDataService.TransportNext(CurrentPlayer));
            TransportToggleRepeatCommand = new RelayCommand(() => FionaDataService.TransportRepeat(CurrentPlayer));
            TransportToggleShuffleCommand = new RelayCommand(() => FionaDataService.TransportShuffle(CurrentPlayer));
            ToggleMuteCommand = new RelayCommand(() => FionaDataService.ToggleMuteVolume(CurrentPlayer));
            ShowNowPlayingCommand = new RelayCommand(() => NowPlayingPageVisibility = Visibility.Visible);
            HideNowPlayingCommand = new RelayCommand(() => NowPlayingPageVisibility = Visibility.Collapsed);
        }

        #region Template implementation
        private readonly KeyboardAccelerator _altLeftKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu);
        private readonly KeyboardAccelerator _backKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.GoBack);

        private bool _isBackEnabled;
        private IList<KeyboardAccelerator> _keyboardAccelerators;
        private WinUI.NavigationView _navigationView;
        private WinUI.NavigationViewItem _selected;
        private ICommand _loadedCommand;
        private ICommand _itemInvokedCommand;

        public bool IsBackEnabled
        {
            get { return _isBackEnabled; }
            set { SetProperty(ref _isBackEnabled, value); }
        }

        public WinUI.NavigationViewItem Selected
        {
            get { return _selected; }
            set { SetProperty(ref _selected, value); }
        }

        public ICommand LoadedCommand => _loadedCommand ?? (_loadedCommand = new RelayCommand(OnLoaded));

        public ICommand ItemInvokedCommand => _itemInvokedCommand ?? (_itemInvokedCommand = new RelayCommand<WinUI.NavigationViewItemInvokedEventArgs>(OnItemInvoked));

        public void Initialize(Frame frame, WinUI.NavigationView navigationView, IList<KeyboardAccelerator> keyboardAccelerators)
        {
            _navigationView = navigationView;
            _keyboardAccelerators = keyboardAccelerators;
            NavigationService.Frame = frame;
            NavigationService.NavigationFailed += Frame_NavigationFailed;
            NavigationService.Navigated += Frame_Navigated;
            _navigationView.BackRequested += OnBackRequested;
        }

        private async void OnLoaded()
        {
            // Keyboard accelerators are added here to avoid showing 'Alt + left' tooltip on the page.
            // More info on tracking issue https://github.com/Microsoft/microsoft-ui-xaml/issues/8
            _keyboardAccelerators.Add(_altLeftKeyboardAccelerator);
            _keyboardAccelerators.Add(_backKeyboardAccelerator);
            await Task.CompletedTask;
        }

        private void OnItemInvoked(WinUI.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                NavigationService.Navigate(typeof(SettingsPage), null, args.RecommendedNavigationTransitionInfo);
            }
            else if (args.InvokedItemContainer is WinUI.NavigationViewItem selectedItem)
            {
                var pageType = selectedItem.GetValue(NavHelper.NavigateToProperty) as Type;
                NavigationService.Navigate(pageType, null, args.RecommendedNavigationTransitionInfo);
            }
        }

        private void OnBackRequested(WinUI.NavigationView sender, WinUI.NavigationViewBackRequestedEventArgs args)
        {
            NavigationService.GoBack();
        }

        private void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw e.Exception;
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            IsBackEnabled = NavigationService.CanGoBack;
            if (e.SourcePageType == typeof(SettingsPage))
            {
                Selected = _navigationView.SettingsItem as WinUI.NavigationViewItem;
                return;
            }

            var selectedItem = GetSelectedItem(_navigationView.MenuItems, e.SourcePageType);
            if (selectedItem != null)
            {
                Selected = selectedItem;
            }
        }

        private WinUI.NavigationViewItem GetSelectedItem(IEnumerable<object> menuItems, Type pageType)
        {
            foreach (var item in menuItems.OfType<WinUI.NavigationViewItem>())
            {
                if (IsMenuItemForPageType(item, pageType))
                {
                    return item;
                }

                var selectedChild = GetSelectedItem(item.MenuItems, pageType);
                if (selectedChild != null)
                {
                    return selectedChild;
                }
            }

            return null;
        }

        private bool IsMenuItemForPageType(WinUI.NavigationViewItem menuItem, Type sourcePageType)
        {
            var pageType = menuItem.GetValue(NavHelper.NavigateToProperty) as Type;
            return pageType == sourcePageType;
        }

        private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
        {
            var keyboardAccelerator = new KeyboardAccelerator() { Key = key };
            if (modifiers.HasValue)
            {
                keyboardAccelerator.Modifiers = modifiers.Value;
            }

            keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;
            return keyboardAccelerator;
        }

        private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var result = NavigationService.GoBack();
            args.Handled = result;
        }
        #endregion
    }
}
