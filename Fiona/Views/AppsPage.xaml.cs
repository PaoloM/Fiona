using System;

using Fiona.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Fiona.Views
{
    public sealed partial class AppsPage : Page
    {
        public AppsViewModel ViewModel { get; } = new AppsViewModel();

        public AppsPage()
        {
            InitializeComponent();
        }
    }
}
