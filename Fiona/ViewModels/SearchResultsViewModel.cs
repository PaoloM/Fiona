using System;
using Fiona.Core.Models;
using Fiona.Core.Services;
using Fiona.Helpers;
using Fiona.Services;
using Fiona.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Windows.UI.Xaml;

namespace Fiona.ViewModels
{
    public class SearchResultsViewModel : BaseViewModel
    {
        private ArtistList _artists = new ArtistList();
        public ArtistList ArtistsResults
        {
            get => _artists;
            set => SetProperty(ref _artists, value);
        }

        private AlbumList _albums = new AlbumList();
        public AlbumList AlbumsResults
        {
            get => _albums;
            set => SetProperty(ref _albums, value);
        }

        private TrackList _tracks = new TrackList();
        public TrackList TracksResults
        {
            get => _tracks;
            set => SetProperty(ref _tracks, value);
        }

        public string QueryTerm
        {
            set
            {
                ArtistsResults.Artists = FionaDataService.AllArtists.Artists.FindAll(p => p.Name.ToLower().StartsWith(value.ToLower()));
                AlbumsResults.Albums = FionaDataService.AllAlbums.Albums.FindAll(p => p.Name.ToLower().StartsWith(value.ToLower()));
            }
        }

        public SearchResultsViewModel()
            {
            }

    }
}
