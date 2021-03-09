using System;

using Fiona.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Fiona.Views
{
    public sealed partial class FavoritesPage : Page
    {
        public FavoritesViewModel ViewModel { get; } = new FavoritesViewModel();

        public FavoritesPage()
        {
            InitializeComponent();
        }
    }
}
