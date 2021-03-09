using Fiona.Core.Models;
using Fiona.Core.Services;
using Fiona.Helpers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiona.ViewModels
{
    public class ArtistDetailsViewModel : ObservableObject
    {
        private string AllGenres = "";

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
                AllGenres = "";
                foreach (Genre gg in value.Genres)
                {
                    if (!string.IsNullOrEmpty(AllGenres))
                        AllGenres += ResourceExtensions.GetLocalized("Album_GenreSeparator");
                    AllGenres += gg.Name;
                }
                SetProperty(ref _currentArtist, value);
            }
        }
    }
}
