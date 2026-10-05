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
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Xaml.Media.Imaging;

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

        private PlayerList _PlayersList;
        public PlayerList PlayersList
        {
            get => _PlayersList;
            private set => SetProperty(ref _PlayersList, value);
        }

        /// <summary>
        /// Asks the server which players it has and settles on one to control. Has to be
        /// called rather than computed on demand: the shell is built before a server has
        /// necessarily been resolved, and the player picker binds once, so a list fetched
        /// while there was nothing to ask would never be replaced.
        /// </summary>
        public void RefreshPlayers()
        {
            var ap = FionaDataService.GetAllPlayers();
            PlayersList = ap;

            if (!(ap?.Players?.Count > 0)) // no server, or a server with no players attached
            {
                return;
            }

            // Whatever is already selected wins: refreshing happens for reasons that have nothing
            // to do with the user - a player connecting, say - and moving their selection out from
            // under them sends the next command they press to a different player entirely.
            // The list is deserialized afresh each time, so match on the id and not the reference.
            Player cp = null;
            string selectedId = FionaDataService.CurrentPlayer == null ? null : FionaDataService.CurrentPlayer.ID;
            if (!string.IsNullOrEmpty(selectedId))
            {
                cp = ap.Players.FirstOrDefault(p => p.ID == selectedId);
            }

            if (cp == null)
            {
                //TODO maybe provide a "preferred player" selection in Settings
                cp = ap.Players.FirstOrDefault(p => p.IsPlaying) // the one playing now
                    ?? ap.Players[0];                            // or the first, if none is
            }

            CurrentPlayer = cp;
            OnPropertyChanged(nameof(CurrentPlayer));
            DispatcherTimerSetup();
        }

        public Player CurrentPlayer
        {
            get => FionaDataService.CurrentPlayer;
            set => FionaDataService.CurrentPlayer = value;
        }

        private string _nowPlayingAlbumArt;
        public string NowPlayingAlbumArt
        {
            get => string.IsNullOrEmpty(_nowPlayingAlbumArt) ? FionaDataService.DefaultAlbumImageUrl : _nowPlayingAlbumArt;
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

        private string _ServerMessage = "";

        /// <summary>
        /// What went wrong the last time we spoke to the server. Queries answer with nothing rather
        /// than throwing, so without showing this an unreachable server looks exactly like an empty
        /// library - which is the confusion this whole path exists to prevent.
        /// </summary>
        public string ServerMessage
        {
            get => _ServerMessage;
            set => SetProperty(ref _ServerMessage, value);
        }

        private Visibility _ServerMessageVisibility = Visibility.Collapsed;
        public Visibility ServerMessageVisibility
        {
            get => _ServerMessageVisibility;
            set => SetProperty(ref _ServerMessageVisibility, value);
        }

        /// <summary>
        /// Call on the UI thread: the data service reports failures from whichever thread made the
        /// request.
        /// </summary>
        public void ShowServerProblem(string reason)
        {
            ServerMessage = string.Format("Shell_ServerProblem".GetLocalized(), reason);
            ServerMessageVisibility = Visibility.Visible;
        }

        private Visibility _SetupPageVisibility = Visibility.Collapsed;
        public Visibility SetupPageVisibility
        {
            get => _SetupPageVisibility;
            set => SetProperty(ref _SetupPageVisibility, value);
        }

        private string _artistBio = "";
        public string ArtistBio
        {
            get => _artistBio;
            set => SetProperty(ref _artistBio, value);
        }

        private List<Image> _artistImageList = new List<Image>();
        public List<Image> ArtistImageList
        {
            get => _artistImageList;
            set => SetProperty(ref _artistImageList, value);
        }

        private Image _artistImage = new Image();
        public Image ArtistImage
        {
            get => _artistImage;// == null ? FionaDataService.DefaultAlbumImageUrl : _artistImageUrl;
            set => SetProperty(ref _artistImage, value);
        }

        private int CurrentArtistImageIndex = 0;
        private int Tick = 0;
        // Seconds an artist image stays up, on a one second tick. Longer than it used to be
        // because the image now moves while it is there: a six second cut was too short to read as
        // anything but a flicker, and too short for a pan to go anywhere.
        private int NextImageDuration = 19;

        private DispatcherTimer dispatcherTimer;

        public void DispatcherTimerSetup()
        {
            // Run again on every player refresh, so the timer already polling the server
            // once a second has to be retired rather than left behind alongside the new one.
            if (dispatcherTimer != null)
            {
                dispatcherTimer.Stop();
                dispatcherTimer.Tick -= dispatcherTimer_Tick;
            }

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            CurrentArtistImageIndex = 0;
            Tick = 0;
            dispatcherTimer.Start();
        }

        /// <summary>
        /// A failure in here used to close the app: an exception out of a DispatcherTimer tick is
        /// unhandled, and this runs every second, reaching both the server and Discogs as it goes.
        /// There is nothing useful to do about one failed tick except let the next one try again.
        /// </summary>
        void dispatcherTimer_Tick(object sender, object e)
        {
            try
            {
                UpdateNowPlaying();
            }
            catch (Exception)
            {
                // Swallowed on purpose. The next tick is a second away, and a transient failure
                // reaching the server is not worth interrupting someone over.
            }
        }

        private void UpdateNowPlaying()
        {
            // move to the next artist image in the Now Playing screen
            if (++Tick > NextImageDuration)
            {
                int c = ArtistImageList.Count();
                if (c > 0)
                {
                    if (CurrentArtistImageIndex >= c - 1)
                        CurrentArtistImageIndex = 0;
                    else
                        CurrentArtistImageIndex++;
                    ArtistImage = ArtistImageList[CurrentArtistImageIndex];
                }
                Tick = 0;
            }

            CurrentPlayerStatus = FionaDataService.GetPlayerStatus(this.CurrentPlayer);

            // The server did not answer, or answered with nothing usable. Everything below reads
            // this status, and leaving the last known state on screen beats blanking it over one
            // failed poll.
            if (CurrentPlayerStatus == null) return;

            // Answering at all means whatever was wrong no longer is.
            ServerMessageVisibility = Visibility.Collapsed;

            if (CurrentPlayerStatus.Mode == PlayerMode.play)
                IsPlayingGlyph = "\xE103";
            else
                IsPlayingGlyph = "\xE768";

            IsMuted = CurrentPlayerStatus.IsMuted;

            if (CurrentPlayerStatus.CurrentSong != null)
            {
                if (NowPlayingAlbumArt != CurrentPlayerStatus.CurrentSong?.ArtworkUrl) //TODO find a better way to see if the track changed
                {
                    NowPlayingAlbumArt = string.IsNullOrEmpty(CurrentPlayerStatus.CurrentSong.ArtworkUrl) ? FionaDataService.DefaultAlbumImageUrl : CurrentPlayerStatus.CurrentSong.ArtworkUrl;

                    if (CurrentPlayerStatus.CurrentSong.Artist != null)
                    {
                        string ca = CurrentPlayerStatus.CurrentSong.Artist;
                        if (ca.IndexOf(',') > 0)
                            ca = ca.Substring(0, ca.IndexOf(',')); // if there is a comma, take the first artist

                        if (ca.IndexOf(" - ") > 0)
                            ca = ca.Substring(ca.IndexOf(" - ") + 3); // if there is a " - ", take the last part of the string. This to support Band's Camp plugin

                        ca = ca.Trim();

                        // look in the local artists list, which may not have loaded yet
                        ArtistList cached = FionaDataService.AllArtists;
                        List<Artist> known = cached == null ? null : cached.Artists;
                        var aa = (from a in known ?? new List<Artist>() where a.Name == ca select a);
                        if (aa.Count<Artist>() > 0)
                        {
                            var artist = aa.First<Artist>();

                            DiscogsArtist da = DiscogsDataService.GetArtistInfo(artist.Name);
                            if (da != null)
                            {
                                artist.Profile = da.Profile;
                                artist.Images = new List<string>();
                                ArtistImageList.Clear();

                                if (da.Images != null && da.Images.Count > 0)
                                {
                                    foreach (var i in da.Images)
                                    {
                                        artist.Images.Add(i.ImageUrl);
                                        Image img = new Image();
                                        BitmapImage bm = new BitmapImage();
                                        Uri uri = new Uri(i.ImageUrl);
                                        bm.UriSource = uri;
                                        img.Source = bm;
                                        ArtistImageList.Add(img);
                                    }
                                }
                            }

                            ArtistBio = artist.Profile;
                            Random rnd = new Random();
                            //ArtistImageUrl = artist.Images.Count > 0 ? artist.Images[rnd.Next(0, artist.Images.Count - 1)] : FionaDataService.DefaultAlbumImageUrl;
                        }
                        else
                        { // did not find it in the internal artist list, let's see if discogs has anything about the artist
                            DiscogsArtist da = DiscogsDataService.GetArtistInfo(ca);
                            if (da != null)
                            {
                                ArtistImageList.Clear();
                                if (da.Images != null && da.Images.Count > 0)
                                {
                                    foreach (var i in da.Images)
                                    {
                                        //da.Images.Add(i.ImageUrl);
                                        Image img = new Image();
                                        BitmapImage bm = new BitmapImage();
                                        Uri uri = new Uri(i.ImageUrl);
                                        bm.UriSource = uri;
                                        img.Source = bm;
                                        ArtistImageList.Add(img);
                                    }
                                }
                                ArtistBio = da.Profile;
                                Random rnd = new Random();
                                //ArtistImageUrl = da.Images.Count > 0 ? da.Images[rnd.Next(0, da.Images.Count - 1)].ImageUrl : FionaDataService.DefaultAlbumImageUrl;
                            }
                        }
                    }

                    // Construct the live tile content
                    var content = new TileContent()
                    {
                        Visual = new TileVisual()
                        {
                            Branding = TileBranding.NameAndLogo,
                            TileMedium = new TileBinding()
                            {
                                Content = new TileBindingContentAdaptive()
                                {
                                    BackgroundImage = new TileBackgroundImage()
                                    {
                                        Source = NowPlayingAlbumArt
                                    }
                                }
                            },
                            TileLarge = new TileBinding()
                            {
                                Content = new TileBindingContentAdaptive()
                                {
                                    BackgroundImage = new TileBackgroundImage()
                                    {
                                        Source = NowPlayingAlbumArt
                                    }
                                }
                            }
                        }
                    };

                    // Then create the tile notification
                    var notification = new TileNotification(content.GetXml());
                    // Send the notification to the primary tile                    
                    TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
                }
            }

            if (CurrentPlayerStatus.Playlist != null)
            {
                if (CurrentPlayerStatus.Playlist.Count > 0)
                {
                    //TODO change the queue even if it has the same number of entries as the current playlist
                    if ((CurrentPlayerStatus.Playlist.Count - CurrentPlayerStatus.PlaylistCurrentIndex) != Queue?.Count)
                    {
                        Queue = CurrentPlayerStatus.Playlist.Skip(CurrentPlayerStatus.PlaylistCurrentIndex).ToList<Track>();
                    }
                }
            }
        }

        private RelayCommand _ShowNowPlayingCommand;
        public RelayCommand ShowNowPlayingCommand => _ShowNowPlayingCommand ?? (_ShowNowPlayingCommand = new RelayCommand(ShowNowPlaying));
        private void ShowNowPlaying()
        {
            NowPlayingPageVisibility = Visibility.Visible;
        }

        private RelayCommand _HideNowPlayingCommand;
        public RelayCommand HideNowPlayingCommand => _HideNowPlayingCommand ?? (_HideNowPlayingCommand = new RelayCommand(HideNowPlaying));
        private void HideNowPlaying()
        {
            NowPlayingPageVisibility = Visibility.Collapsed;
        }

        public ShellViewModel()
        {
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

        private Type CurrentPageType;

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

            RefreshPlayers();
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
                CurrentPageType = pageType;
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
