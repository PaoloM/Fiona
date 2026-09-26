using System;
using System.ComponentModel;
using Fiona.Services;
using Fiona.ViewModels;
using Windows.ApplicationModel.Core;
using Windows.Media;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

namespace Fiona.Views
{
    public sealed partial class ShellPage : Page
    {
        public ShellViewModel ViewModel { get; } = new ShellViewModel();

        public ShellPage()
        {
            // Custom titlebar
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
            formattableTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(TitleBar); // Set XAML element as a draggable region

            InitializeComponent();

            DataContext = ViewModel;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.Initialize(shellFrame, navigationView, KeyboardAccelerators);

            // Registering as a player takes a round trip or two, so the local player is usually
            // absent from the first list the server gives us and has to be picked up afterwards.
            LocalPlayerService.ConnectionChanged += OnLocalPlayerConnectionChanged;

        }

        /// <summary>
        /// The local player appearing in or leaving the server's list changes what the picker should
        /// show. Arrives from the protocol client's own task, so it has to be marshalled before it
        /// touches the view model - refreshing builds a DispatcherTimer, which needs the UI thread.
        /// </summary>
        private async void OnLocalPlayerConnectionChanged(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ViewModel.RefreshPlayers());
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            TitleBar.Height = sender.Height;
        }

        #region Search
        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Only get results when it was a user typing,
            // otherwise assume the value got filled in by TextMemberPath
            // or the handler for SuggestionChosen.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                //Set the ItemsSource to be your filtered dataset
                //sender.ItemsSource = dataset;
            }
        }

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Set sender.Text. You can use args.SelectedItem to build your text string.
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                // User selected an item from the suggestion list, take an action on it here.
            }
            else
            {
                // Use args.QueryText to determine what to do.
                NavigationService.Navigate<SearchResultsView>(args.QueryText);
            }
        }
        #endregion

        #region Now Playing background crossfade

        private const double BackgroundOpacity = 0.2;
        private static readonly TimeSpan CrossFadeDuration = TimeSpan.FromMilliseconds(1200);

        // True when NowPlayingBackgroundA is the image currently visible.
        private bool _isBackgroundAVisible;

        // The image that has just been given a new source and is waiting to be faded in.
        private Image _pendingBackground;

        private Storyboard _crossFade;

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ShellViewModel.ArtistImage))
            {
                CrossFadeArtistBackground();
            }
        }

        private void CrossFadeArtistBackground()
        {
            ImageSource source = ViewModel.ArtistImage?.Source;
            if (source == null) return;

            Image incoming = _isBackgroundAVisible ? NowPlayingBackgroundB : NowPlayingBackgroundA;
            if (ReferenceEquals(incoming.Source, source)) return; // already showing this one

            // The fade starts once the bitmap is decoded, so we never fade in a blank image
            _pendingBackground = incoming;
            incoming.Source = source;

            // A bitmap that is already decoded may not raise ImageOpened again, so fade right away
            if (source is BitmapImage bitmap && bitmap.PixelWidth > 0)
            {
                FadeIn(incoming);
            }
        }

        private void NowPlayingBackground_ImageOpened(object sender, RoutedEventArgs e)
        {
            FadeIn(sender as Image);
        }

        private void NowPlayingBackground_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            // Keep the current background on screen if the new image could not be loaded
            if (ReferenceEquals(sender, _pendingBackground))
            {
                _pendingBackground = null;
            }
        }

        private void FadeIn(Image incoming)
        {
            if (incoming == null || !ReferenceEquals(incoming, _pendingBackground)) return;

            _pendingBackground = null;
            _isBackgroundAVisible = ReferenceEquals(incoming, NowPlayingBackgroundA);

            Image outgoing = _isBackgroundAVisible ? NowPlayingBackgroundB : NowPlayingBackgroundA;

            // A new storyboard hands off from the values the running one reached, so it never jumps
            var crossFade = new Storyboard();
            crossFade.Children.Add(CreateOpacityAnimation(incoming, BackgroundOpacity));
            crossFade.Children.Add(CreateOpacityAnimation(outgoing, 0));
            crossFade.Completed += (s, e) =>
            {
                if (!ReferenceEquals(_crossFade, crossFade)) return; // a later fade already took over

                // Make the animated values local ones so the storyboard can be released
                crossFade.Stop();
                incoming.Opacity = BackgroundOpacity;
                outgoing.Opacity = 0;
                _crossFade = null;
            };

            _crossFade = crossFade;
            crossFade.Begin();
        }

        private static DoubleAnimation CreateOpacityAnimation(Image target, double to)
        {
            var animation = new DoubleAnimation
            {
                To = to,
                Duration = new Duration(CrossFadeDuration),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, "Opacity");

            return animation;
        }

        #endregion
    }
}
