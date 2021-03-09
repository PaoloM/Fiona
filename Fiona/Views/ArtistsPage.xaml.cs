using System;

using Fiona.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Fiona.Views
{
    public sealed partial class ArtistsPage : Page
    {
        public ArtistsViewModel ViewModel { get; } = new ArtistsViewModel();

        public ArtistsPage()
        {
            InitializeComponent();
        }
    }
}
