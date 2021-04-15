using System;
using Fiona.Core.Models;
using Fiona.Core.Services;
using Fiona.ViewModels;

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Fiona.Views
{
    public sealed partial class AppsPage : Page
    {
        public AppsViewModel ViewModel { get; } = new AppsViewModel();

        public AppsPage()
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
    }
}
