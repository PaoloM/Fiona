using System;
using System.Collections.Generic;
using System.Linq;
using Fiona.Core.Models;
using Fiona.Core.Services;
using Fiona.Helpers;
using Fiona.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace Fiona.ViewModels
{
    public class AlbumsViewModel : ObservableObject
    {
        public List<Album> Albums { get => FionaDataService.GetAllAlbums().Albums; }

        public RelayCommand ShuffleAllCommand { get; set; }

        public AlbumsViewModel()
        {
            PlayAlbumCommand = new RelayCommand<Album>(param => PlayAlbum((Album)param));
            QueueAlbumCommand = new RelayCommand<Album>(param => QueueAlbum((Album)param));
            ViewAlbumDetailsCommand = new RelayCommand<Album>(param => ViewAlbumDetails((Album)param));
        }

        public RelayCommand<Album> PlayAlbumCommand { get; set; }
        private void PlayAlbum(Album album)
        {
            FionaDataService.PlaylistLoadAndPlayAlbum(FionaDataService.CurrentPlayer, album);
        }

        public RelayCommand<Album> QueueAlbumCommand { get; set; }
        private void QueueAlbum(Album album)
        {
            FionaDataService.PlaylistAppendAlbum(FionaDataService.CurrentPlayer, album);
        }

        public RelayCommand<Album> ViewAlbumDetailsCommand { get; set; }
        private void ViewAlbumDetails(Album album)
        {
            NavigationService.Navigate(typeof(Views.AlbumDetailsPage), album, null);
        }

    }
}
