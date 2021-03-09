using System;

using Fiona.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Fiona.Views
{
    public sealed partial class RadioPage : Page
    {
        public RadioViewModel ViewModel { get; } = new RadioViewModel();

        public RadioPage()
        {
            InitializeComponent();
        }
    }
}
