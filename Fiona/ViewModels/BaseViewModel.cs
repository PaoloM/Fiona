﻿using Fiona.Core.Models;
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
        public List<Favorite> Favorites { get => FionaDataService.GetAllFavorites().Favorites; }

        #region ALBUM ------------------------------------------------------------------------------
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
        #endregion

        #region ARTIST ------------------------------------------------------------------------------
        private RelayCommand<Artist> _GoToArtistCommand;
        public RelayCommand<Artist> GoToArtistCommand => _GoToArtistCommand ?? (_GoToArtistCommand = new RelayCommand<Artist>(param => GoToArtist((Artist)param)));
        private void GoToArtist(Artist artist)
        {
            NavigationService.Navigate<ArtistDetailsPage>(artist);
        }

        private RelayCommand<Artist> _PlayAllByArtistCommand;
        public RelayCommand<Artist> PlayAllByArtistCommand => _PlayAllByArtistCommand ?? (_PlayAllByArtistCommand = new RelayCommand<Artist>(param => PlayAllByArtist((Artist)param)));
        private void PlayAllByArtist(Artist artist)
        {
            FionaDataService.PlaylistLoadAndPlayArtist(FionaDataService.CurrentPlayer, artist);
        }

        private RelayCommand<Artist> _ShuffleAllByArtistCommand;
        public RelayCommand<Artist> ShuffleAllByArtistCommand => _ShuffleAllByArtistCommand ?? (_ShuffleAllByArtistCommand = new RelayCommand<Artist>(param => ShuffleAllByArtist((Artist)param)));
        private void ShuffleAllByArtist(Artist artist)
        {
            FionaDataService.PlaylistLoadAndShuffleArtist(FionaDataService.CurrentPlayer, artist);
        }

        private RelayCommand<Artist> _AddAllToQueueByArtistCommand;
        public RelayCommand<Artist> AddAllToQueueByArtistCommand => _AddAllToQueueByArtistCommand ?? (_AddAllToQueueByArtistCommand = new RelayCommand<Artist>(param => AddAllToQueueByArtist((Artist)param)));
        private void AddAllToQueueByArtist(Artist artist)
        {
            FionaDataService.PlaylistAppendArtist(FionaDataService.CurrentPlayer, artist);
        }

        private RelayCommand<Artist> _ViewArtistOnDiscogsCommand;
        public RelayCommand<Artist> ViewArtistOnDiscogsCommand => _ViewArtistOnDiscogsCommand ?? (_ViewArtistOnDiscogsCommand = new RelayCommand<Artist>(param => ViewArtistOnDiscogs((Artist)param)));
        private async void ViewArtistOnDiscogs(Artist artist)
        {
            var uri = new Uri("https://www.discogs.com/artist/" + artist.DiscogsID.ToString());
            var success = await Windows.System.Launcher.LaunchUriAsync(uri);
         }

        private RelayCommand _ShuffleAllArtistsCommand;
        public RelayCommand ShuffleAllArtistsCommand => _ShuffleAllArtistsCommand ?? (_ShuffleAllArtistsCommand = new RelayCommand(ShuffleAllArtists));
        private void ShuffleAllArtists()
        {
            //TODO
        }
        #endregion

        #region TRACK ------------------------------------------------------------------------------
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
        #endregion

        #region TRANSPORT ------------------------------------------------------------------------------

        private RelayCommand _PauseTransportCommand;
        public RelayCommand PauseTransportCommand => _PauseTransportCommand ?? (_PauseTransportCommand = new RelayCommand(PauseTransport));
        private void PauseTransport()
        {
            FionaDataService.TransportPause(FionaDataService.CurrentPlayer);
        }

        private RelayCommand _PreviousTrackTransportCommand;
        public RelayCommand PreviousTrackTransportCommand => _PreviousTrackTransportCommand ?? (_PreviousTrackTransportCommand = new RelayCommand(PreviousTrackTransport));
        private void PreviousTrackTransport()
        {
            FionaDataService.TransportPrevious(FionaDataService.CurrentPlayer);
        }

        private RelayCommand _NextTrackTransportCommand;
        public RelayCommand NextTrackTransportCommand => _NextTrackTransportCommand ?? (_NextTrackTransportCommand = new RelayCommand(NextTrackTransport));
        private void NextTrackTransport()
        {
            FionaDataService.TransportNext(FionaDataService.CurrentPlayer);
        }

        private RelayCommand _ToggleRepeatTransportCommand;
        public RelayCommand ToggleRepeatTransportCommand => _ToggleRepeatTransportCommand ?? (_ToggleRepeatTransportCommand = new RelayCommand(ToggleRepeatTransport));
        private void ToggleRepeatTransport()
        {
            FionaDataService.TransportToggleRepeat(FionaDataService.CurrentPlayer);
        }

        private RelayCommand _ToggleShuffleTransportCommand;
        public RelayCommand ToggleShuffleTransportCommand => _ToggleShuffleTransportCommand ?? (_ToggleShuffleTransportCommand = new RelayCommand(ToggleShuffleTransport));
        private void ToggleShuffleTransport()
        {
            FionaDataService.TransportToggleShuffle(FionaDataService.CurrentPlayer);
        }

        private RelayCommand _ToggleMuteCommand;
        public RelayCommand ToggleMuteCommand => _ToggleMuteCommand ?? (_ToggleMuteCommand = new RelayCommand(ToggleMute));
        private void ToggleMute()
        {
            FionaDataService.ToggleMuteVolume(FionaDataService.CurrentPlayer);
        }
        #endregion

        #region FAVORITES ---------------------------------------------------------------------------------
        private RelayCommand<Favorite> _PlayFavoriteCommand;
        public RelayCommand<Favorite> PlayFavoriteCommand => _PlayFavoriteCommand ?? (_PlayFavoriteCommand = new RelayCommand<Favorite>(param => PlayFavorite((Favorite)param)));
        private void PlayFavorite(Favorite favorite)
        {
            FionaDataService.PlaylistLoadAndPlayFavorite(FionaDataService.CurrentPlayer, favorite);
        }

        private RelayCommand<Favorite> _QueueFavoriteCommand;
        public RelayCommand<Favorite> QueueFavoriteCommand => _QueueFavoriteCommand ?? (_QueueFavoriteCommand = new RelayCommand<Favorite>(param => QueueFavorite((Favorite)param)));
        private void QueueFavorite(Favorite favorite)
        {
            FionaDataService.PlaylistAppendFavorite(FionaDataService.CurrentPlayer, favorite);
        }

        private RelayCommand<Album> _AddFavoriteAlbumCommand;
        public RelayCommand<Album> AddFavoriteAlbumCommand => _AddFavoriteAlbumCommand ?? (_AddFavoriteAlbumCommand = new RelayCommand<Album>(param => AddFavoriteAlbum((Album)param)));
        private void AddFavoriteAlbum(Album album)
        {
            FionaDataService.AddFavorite(FionaDataService.CurrentPlayer, album.Name, album.Url == null ? album.ExtID : album.Url, album.ArtworkUrl);
            // TODO reload favorites
        }

        private RelayCommand<Track> _AddFavoriteTrackCommand;
        public RelayCommand<Track> AddFavoriteTrackCommand => _AddFavoriteTrackCommand ?? (_AddFavoriteTrackCommand = new RelayCommand<Track>(param => AddFavoriteTrack((Track)param)));
        private void AddFavoriteTrack(Track track)
        {
            FionaDataService.AddFavorite(FionaDataService.CurrentPlayer, track.Title, track.Location, track.ArtworkUrl);
            // TODO reload favorites
        }

        private RelayCommand<Favorite> _UnFavoriteCommand;
        public RelayCommand<Favorite> UnFavoriteCommand => _UnFavoriteCommand ?? (_UnFavoriteCommand = new RelayCommand<Favorite>(param => UnFavorite((Favorite)param)));
        private void UnFavorite(Favorite favorite)
        {
            FionaDataService.UnFavorite(FionaDataService.CurrentPlayer, favorite);
            // TODO reload favorites
        }

        #endregion

        private RelayCommand<string> _SearchCommand;
        public RelayCommand<string> SearchCommand => _SearchCommand ?? (_SearchCommand = new RelayCommand<string>(param => Search((string)param)));
        private void Search(string q)
        {
            NavigationService.Navigate<SearchResultsView>(q);
        }
    }
}
