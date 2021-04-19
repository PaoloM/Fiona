using System;
using Fiona.Core.Models;
using Fiona.Core.Services;
using Fiona.ViewModels;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Fiona.Views
{
    public sealed partial class RadioPage : Page
    {
        public RadioViewModel ViewModel { get; } = new RadioViewModel();

        public RadioPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.CurrentApplet = (Applet)(e.Parameter);
        }

        private void PlayThis_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var item = e.OriginalSource as Button;
            var ap = item.DataContext as Applet;
            FionaDataService.PlayPlaylistFromApp(FionaDataService.CurrentPlayer, FionaDataService.CurrentAppletMenu, FionaDataService.CurrentAppletMenu, ap.GetID);
        }

        private void QueueThis_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var item = e.OriginalSource as Button;
            var ap = item.DataContext as Applet;
            FionaDataService.QueuePlaylistFromApp(FionaDataService.CurrentPlayer, FionaDataService.CurrentAppletMenu, FionaDataService.CurrentAppletMenu, ap.GetID);
        }

        private void SearchTextBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                var o = e.OriginalSource as TextBox;
                var a = o.DataContext as Applet;
                var al = FionaDataService.SearchInApp(FionaDataService.CurrentPlayer, FionaDataService.CurrentAppletMenu, FionaDataService.CurrentAppletMenu, a.GetID, o.Text);
                ViewModel.TextareaVisibility = Windows.UI.Xaml.Visibility.Collapsed;
                ViewModel.AppsGridViewVisibility = Windows.UI.Xaml.Visibility.Visible;
                ViewModel.AppsListViewVisibility = Windows.UI.Xaml.Visibility.Collapsed;
                ViewModel.Apps = al;
            }
        }
    }
}
