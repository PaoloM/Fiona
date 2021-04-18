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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fiona.ViewModels
{
    public class ArtistDetailsViewModel : BaseViewModel
    {
        private enum BioServices
        {
            DISCOGS
        }

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
                                
                string ca = value.Name;
                if (ca.IndexOf(',') > 0)
                    ca = ca.Substring(0, ca.IndexOf(',')); // if there is a comma, take the first artist
                if (ca.IndexOf(" - ") > 0)
                    ca = ca.Substring(ca.IndexOf(" - ") + 3); // if there is a " - ", take the last part of the string. This to support Band's Camp plugin

                DiscogsArtist da = DiscogsDataService.GetArtistInfo(ca);
                if (da != null)
                {
                    value.DiscogsID = da.ID;
                    value.Profile = da.Profile;
                    value.Images = new List<string>();
                    if (da.Images.Count > 0)
                    {
                        foreach (var i in da.Images)
                            value.Images.Add(i.ImageUrl);
                    }
                }
                                
                ArtistBio = PrettifyBio(BioServices.DISCOGS, value.Profile, false);
                ArtistImageUrl = value.Images?.Count > 0 ? value.Images[0] : null;

                SetProperty(ref _currentArtist, value);
            }
        }

        private string PrettifyBio(BioServices service, string bio, bool keepHTML)
        {
            var result = "";

            if (service == BioServices.DISCOGS)
            {
                // [a12345] <- artist by Discogs ID
                // DONE [a=ABCDEF] <- artist by name
                // [m12345] <- master by Discogs ID
                // [m=12345] <- master by Discogs ID, there is no name
                // [l12345] <- label by Discogs ID
                // DONE [l=ABCDEF] <- label by name
                // [r=12345] <- release by Discogs ID (?)
                // DONE [i]...[/i] <- italic
                // DONE [url=...]...[/url] <- straight href

                if (!string.IsNullOrEmpty(bio))
                {
                    string[] tok = Regex.Split(bio, @"(\[.+?\])|(\w+)");

                    for (int i = 0; i < tok.Length; i++)
                    {
                        // simple html cases
                        tok[i] = tok[i].Replace("[i]", keepHTML ? "<i>" : "");
                        tok[i] = tok[i].Replace("[/i]", keepHTML ? "</i>" : "");
                        tok[i] = tok[i].Replace("[/url]", keepHTML ? "</a>" : "");

                        if (tok[i].StartsWith("["))
                        {
                            if (tok[i][1] == 'u') // url
                            {
                                tok[i] = keepHTML ? ("<a href=\"" + tok[i].Substring(5, tok[i].Length - 6) + "\">") : "";
                            }
                            else
                            {
                                if (tok[i][2] == '=') // by name, just ignore it
                                {
                                    switch (tok[i][1])
                                    {
                                        case 'a': // artist
                                        case 'l': // label
                                            tok[i] = tok[i].Substring(3, tok[i].Length - 4);
                                            // TODO - check for all numbers, that means it requires another query from Discogs like the block below

                                            break;
                                    }
                                }
                                else // get more info from Discogs
                                {
                                    //TODO
                                }
                            }
                        }
                    }

                    result = String.Join("", tok);
                }
            }

            return result.Trim();
        }
    }
}
