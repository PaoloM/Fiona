﻿using Fiona.Core.Models;
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
                        AllGenres += ResourceExtensions.GetLocalized("Album_GenreSeparator");
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

        private RelayCommand _PlayAllCommand;
        public RelayCommand PlayAllCommand => _PlayAllCommand ?? (_PlayAllCommand = new RelayCommand(PlayAll));
        private void PlayAll()
        {
            FionaDataService.PlaylistLoadAndPlayAlbum(FionaDataService.CurrentPlayer, CurrentAlbum);
        }

        private RelayCommand _AddToQueueCommand;
        public RelayCommand AddToQueueCommand => _AddToQueueCommand ?? (_AddToQueueCommand = new RelayCommand(AddToQueue));
        private void AddToQueue()
        {
            FionaDataService.PlaylistAppendAlbum(FionaDataService.CurrentPlayer, CurrentAlbum);
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
