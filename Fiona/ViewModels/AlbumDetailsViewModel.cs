using Fiona.Core.Models;
using Fiona.Core.Services;
using Fiona.Helpers;
using Fiona.Services;
using Fiona.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;

namespace Fiona.ViewModels
{
    public class AlbumDetailsViewModel : BaseViewModel
    {
        private string AllGenres = "";

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

                string ca = _currentAlbum.ArtistName;
                if (ca.IndexOf(',') > 0)
                    ca = ca.Substring(0, ca.IndexOf(',')); // if there is a comma, take the first artist

                var artist = (from a in FionaDataService.AllArtists.Artists where a.Name == ca select a).First<Artist>();
                if (string.IsNullOrEmpty(artist.Profile))
                {
                    DiscogsArtist da = DiscogsDataService.GetArtistInfo(artist.Name);
                    if (da != null)
                    {
                        artist.Profile = da.Profile;
                        artist.Images = new List<string>();
                        if (da.Images.Count > 0)
                        {
                            foreach (var i in da.Images)
                                artist.Images.Add(i.ImageUrl);
                        }
                    }
                        ArtistBio = artist.Profile;
                        ArtistImageUrl = artist.Images.Count > 0 ? artist.Images[0] : null;
                    
                }
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
