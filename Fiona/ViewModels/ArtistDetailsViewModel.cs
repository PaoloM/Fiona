using Fiona.Core.Models;
using Fiona.Core.Services;
using Fiona.Helpers;
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
    public class ArtistDetailsViewModel : ObservableObject
    {
        private string _artistBio = "";
        public string ArtistBio
        {
            get => _artistBio;
            set => SetProperty(ref _artistBio, value);
        }

        private string _artistImageUrl = "";
        public string ArtistImageUrl
        {
            get => _artistImageUrl;
            set => SetProperty(ref _artistImageUrl, value);
        }

        private string _allGenres = "";
        public string AllGenres
        {
            get => _allGenres;
            set => SetProperty(ref _allGenres, value);
        }

        private Artist _currentArtist;
        public Artist CurrentArtist
        {
            get { return _currentArtist; }
            set
            {
                var e = FionaDataService.GetAllAlbumsByArtist(value);
                var g = FionaDataService.GetAllGenresByArtist(value);
                value.Albums = (from s in e.Albums orderby s.Year select s).ToList<Album>();
                value.Genres = (from t in g.Genres orderby t.TextKey select t).ToList<Genre>();
                string st = "";
                foreach (Genre gg in value.Genres)
                {
                    if (!string.IsNullOrEmpty(st))
                        st += ResourceExtensions.GetLocalized("GenreSeparator");
                    st += gg.Name;
                }
                AllGenres = st;

                SetProperty(ref _currentArtist, value);
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

        private RelayCommand<Album> _ViewAlbumDetailsCommand;
        public RelayCommand<Album> ViewAlbumDetailsCommand => _ViewAlbumDetailsCommand ?? (_ViewAlbumDetailsCommand = new RelayCommand<Album>(param => GoToAlbum((Album)param)));
        private void GoToAlbum(Album album)
        {
            NavigationService.Navigate<AlbumDetailsPage>(album);
        }

    }
}
