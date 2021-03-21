using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Mvvm;
using Microsoft.Toolkit.Mvvm.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Fiona.Controls
{
    public sealed partial class AlbumCard : UserControl
    {
        public AlbumCard()
        {
            this.InitializeComponent();
        }

        public string AlbumName
        {
            get { return (string)GetValue(AlbumNameProperty); }
            set { SetValue(AlbumNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AlbumName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AlbumNameProperty =
            DependencyProperty.Register("AlbumName", typeof(string), typeof(AlbumCard), null);

        public string ArtistName
        {
            get { return (string)GetValue(ArtistNameProperty); }
            set { SetValue(ArtistNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ArtistName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ArtistNameProperty =
            DependencyProperty.Register("ArtistName", typeof(string), typeof(AlbumCard), null);

        public string Artwork
        {
            get { return (string)GetValue(ArtworkProperty); }
            set { SetValue(ArtworkProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Artwork.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ArtworkProperty =
            DependencyProperty.Register("Artwork", typeof(string), typeof(AlbumCard), null);

        public RelayCommand<Fiona.Core.Models.Album> PlayAlbumCommand
        {
            get { return (RelayCommand<Fiona.Core.Models.Album>)GetValue(PlayAlbumCommandProperty); }
            set { SetValue(PlayAlbumCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PlayAlbumCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlayAlbumCommandProperty =
            DependencyProperty.Register("PlayAlbumCommand", typeof(RelayCommand<Fiona.Core.Models.Album>), typeof(AlbumCard), null);

        public RelayCommand<Fiona.Core.Models.Album> QueueAlbumCommand
        {
            get { return (RelayCommand<Fiona.Core.Models.Album>)GetValue(QueueAlbumCommandProperty); }
            set { SetValue(QueueAlbumCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for QueueAlbumCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty QueueAlbumCommandProperty =
            DependencyProperty.Register("QueueAlbumCommand", typeof(RelayCommand<Fiona.Core.Models.Album>), typeof(AlbumCard), null);


    }
}
