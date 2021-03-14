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
    public class AlbumDetailsViewModel : BaseViewModel
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
                var artist = (from a in FionaDataService.AllArtists.Artists where a.ID == _currentAlbum.ArtistID.ToString() select a).First<Artist>();
                CurrentArtist = artist;
            }
        }

        private Artist _currentArtist;
        public Artist CurrentArtist
        {
            get => _currentArtist;
            set => SetProperty(ref _currentArtist, value);
        }

        public string AlbumShortInfo
        {
            get
            {
                return string.Format(ResourceExtensions.GetLocalized("Album_ShortInfoFormat"), CurrentAlbum.Year, AllGenres, CurrentAlbum.Tracks.Count.ToString());
            }
        }

        public AlbumDetailsViewModel()
        {
        }
    }
}
