using Fiona.Core.Models;
using Fiona.Core.Services;
using Fiona.Helpers;
using Fiona.Services;
using Fiona.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Linq;

namespace Fiona.ViewModels
{
    public class AlbumDetailsViewModel : ObservableObject
    {
        private string AllGenres = "";

        private Album _currentAlbum;

        public Album CurrentAlbum
        {
            get { return _currentAlbum; }
            set
            {
                var e = FionaDataService.GetAllTracksByAlbum(value);
                var g = FionaDataService.GetAllGenresByAlbum(value);
                value.Tracks = (from s in e.Tracks orderby s.TracknumSort select s).ToList<Track>();
                value.Genres = (from t in g.Genres orderby t.TextKey select t).ToList<Genre>();
                AllGenres = "";
                foreach (Genre gg in value.Genres)
                {
                    if (!string.IsNullOrEmpty(AllGenres))
                        AllGenres += ResourceExtensions.GetLocalized("GenreSeparator");
                    AllGenres += gg.Name;
                }
                SetProperty(ref _currentAlbum, value);
            }
        }

        public string AlbumShortInfo
        {
            get
            {
                return string.Format(ResourceExtensions.GetLocalized("Album_ShortInfoFormat"), CurrentAlbum.Year, AllGenres, CurrentAlbum.Tracks.Count.ToString());
            }
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

        private RelayCommand _GoToArtistCommand;
        public RelayCommand GoToArtistCommand => _GoToArtistCommand ?? (_GoToArtistCommand = new RelayCommand(GoToArtist));
        private void GoToArtist()
        {
            var artist = (from a in FionaDataService.AllArtists.Artists where a.ID == CurrentAlbum.ArtistID.ToString() select a).First<Artist>();
            NavigationService.Navigate<ArtistDetailsPage>(artist);
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

        public AlbumDetailsViewModel()
        {
        }
    }
}
