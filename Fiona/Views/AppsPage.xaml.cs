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
        public AppsViewModel ViewModel { get; }// = new AppsViewModel();

        public AppsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            FionaDataService.CurrentApplet = (Applet)(e.Parameter);
        }
    }
}
