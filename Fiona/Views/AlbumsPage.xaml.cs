using System;

using Fiona.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Fiona.Views
{
    public sealed partial class AlbumsPage : Page
    {
        public AlbumsViewModel ViewModel { get; } = new AlbumsViewModel();

        public AlbumsPage()
        {
            InitializeComponent();
        }
    }
}
