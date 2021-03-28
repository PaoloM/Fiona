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
    public class ArtistDetailsViewModel : BaseViewModel
    {
        private string _artistBio = "";
        public string ArtistBio
        {
            get => _artistBio;
            set => SetProperty(ref _artistBio, value);
        }

        private string _artistImageUrl = FionaDataService.DefaultAlbumImageUrl;
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
                if (value.Albums.Count == 0)
                {
                    var e = FionaDataService.GetAllAlbumsByArtist(value);
                    value.Albums = (from s in e.Albums orderby s.Year select s).ToList<Album>();
                }

                if (value.Genres.Count == 0)
                {
                    var g = FionaDataService.GetAllGenresByArtist(value);
                    value.Genres = (from t in g.Genres orderby t.TextKey select t).ToList<Genre>();
                }

                string st = "";
                foreach (Genre gg in value.Genres)
                {
                    if (!string.IsNullOrEmpty(st))
                        st += ResourceExtensions.GetLocalized("GenreSeparator");
                    st += gg.Name;
                }
                AllGenres = st;

                if (string.IsNullOrEmpty(value.Profile))
                {
                    string ca = value.Name;
                    if (ca.IndexOf(',') > 0)
                        ca = ca.Substring(0, ca.IndexOf(',')); // if there is a comma, take the first artist

                    DiscogsArtist da = DiscogsDataService.GetArtistInfo(ca);
                    if (da != null)
                    {
                        value.Profile = da.Profile;
                        value.Images = new List<string>();
                        if (da.Images.Count > 0)
                        {
                            foreach (var i in da.Images)
                                value.Images.Add(i.ImageUrl);
                        }
                    }
                }
                ArtistBio = value.Profile;
                ArtistImageUrl = value.Images.Count > 0 ? value.Images[0] : null;
                SetProperty(ref _currentArtist, value);
            }
        }
    }
}
