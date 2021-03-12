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

        private RelayCommand _ShuffleAllAlbumsCommand;
        public RelayCommand ShuffleAllAlbumsCommand => _ShuffleAllAlbumsCommand ?? (_ShuffleAllAlbumsCommand = new RelayCommand(ShuffleAllAlbums));
        private void ShuffleAllAlbums()
        {
            //TODO
        }

        private RelayCommand<Album> _PlayAlbumCommand;
        public RelayCommand<Album> PlayAlbumCommand => _PlayAlbumCommand ?? (_PlayAlbumCommand = new RelayCommand<Album>(param => PlayAlbum((Album)param)));
        private void PlayAlbum(Album album)
        {
            FionaDataService.PlaylistLoadAndPlayAlbum(FionaDataService.CurrentPlayer, album);
        }

        private RelayCommand<Album> _QueueAlbumCommand;
        public RelayCommand<Album> QueueAlbumCommand => _QueueAlbumCommand ?? (_QueueAlbumCommand = new RelayCommand<Album>(param => QueueAlbum((Album)param)));
        private void QueueAlbum(Album album)
        {
            FionaDataService.PlaylistAppendAlbum(FionaDataService.CurrentPlayer, album);
        }

        private RelayCommand<Album> _ViewAlbumDetailsCommand;
        public RelayCommand<Album> ViewAlbumDetailsCommand => _ViewAlbumDetailsCommand ?? (_ViewAlbumDetailsCommand = new RelayCommand<Album>(param => ViewAlbumDetails((Album)param)));
        private void ViewAlbumDetails(Album album)
        {
            NavigationService.Navigate(typeof(Views.AlbumDetailsPage), album, null);
        }

        public AlbumsViewModel()
        {
        }
    }
}
