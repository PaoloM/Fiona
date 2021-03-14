using Fiona.Core.Models;
using Fiona.Core.Services;
using Fiona.Services;
using Fiona.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiona.ViewModels
{
    public class BaseViewModel : ObservableObject
    {
        public List<Artist> Artists { get => FionaDataService.GetAllArtists().Artists; }
        public List<Album> Albums { get => FionaDataService.GetAllAlbums().Albums; }

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
            NavigationService.Navigate<AlbumDetailsPage>(album);
        }

        private RelayCommand<string> _ViewAlbumDetailsByIDCommand;
        public RelayCommand<string> ViewAlbumDetailsByIDCommand => _ViewAlbumDetailsByIDCommand ?? (_ViewAlbumDetailsByIDCommand = new RelayCommand<string>(param => ViewAlbumDetailsByID((string)param)));
        private void ViewAlbumDetailsByID(string id)
        {
            var album = (from a in Albums where a.ID == id select a).First<Album>();
            NavigationService.Navigate<AlbumDetailsPage>(album);
        }

        private RelayCommand _ShuffleAllAlbumsCommand;
        public RelayCommand ShuffleAllAlbumsCommand => _ShuffleAllAlbumsCommand ?? (_ShuffleAllAlbumsCommand = new RelayCommand(ShuffleAllAlbums));
        private void ShuffleAllAlbums()
        {
            //TODO
        }

        private RelayCommand<Artist> _GoToArtistCommand;
        public RelayCommand<Artist> GoToArtistCommand => _GoToArtistCommand ?? (_GoToArtistCommand = new RelayCommand<Artist>(param => GoToArtist((Artist)param)));
        private void GoToArtist(Artist artist)
        {
            NavigationService.Navigate<ArtistDetailsPage>(artist);
        }

        private RelayCommand _ShuffleAllArtistsCommand;
        public RelayCommand ShuffleAllArtistsCommand => _ShuffleAllArtistsCommand ?? (_ShuffleAllArtistsCommand = new RelayCommand(ShuffleAllArtists));
        private void ShuffleAllArtists()
        {
            //TODO
        }

        private RelayCommand<Track> _PlayTrackCommand;
        public RelayCommand<Track> PlayTrackCommand => _PlayTrackCommand ?? (_PlayTrackCommand = new RelayCommand<Track>(param => PlayTrack((Track)param)));
        private void PlayTrack(Track track)
        {
            FionaDataService.PlaylistPlayTrack(FionaDataService.CurrentPlayer, track.Location);
        }

        private RelayCommand<Track> _QueueTrackCommand;
        public RelayCommand<Track> QueueTrackCommand => _QueueTrackCommand ?? (_QueueTrackCommand = new RelayCommand<Track>(param => QueueTrack((Track)param)));
        private void QueueTrack(Track track)
        {
            FionaDataService.PlaylistAddTrackToQueue(FionaDataService.CurrentPlayer, track.Location);
        }
    }
}
