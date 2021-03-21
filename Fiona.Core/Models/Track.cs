using Fiona.Core.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Fiona.Core.Models
{
    public class Track: FionaBase
    {
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "artist")]
        public string Artist { get; set; }

        [JsonProperty(PropertyName = "artist_id")]
        public string ArtistID { get; set; }

        [JsonProperty(PropertyName = "genre")]
        public string Genre { get; set; }

        [JsonProperty(PropertyName = "genre_id")]
        public string GenreID { get; set; }

        [JsonProperty(PropertyName = "album_id")]
        public string AlbumID { get; set; }

        [JsonProperty(PropertyName = "album")]
        public string Album { get; set; }

        [JsonProperty(PropertyName = "duration")]
        public double Duration { get; set; }

        [JsonProperty(PropertyName = "tracknum")]
        public string Tracknum { get; set; }

        [JsonProperty(PropertyName = "disc")]
        public string Disc { get; set; }

        [JsonProperty(PropertyName = "playlist index")]
        public string PlaylistIndex { get; set; }

        [JsonProperty(PropertyName = "coverart")]
        public string CoverArt { get; set; }

        [JsonProperty(PropertyName = "lyrics")]
        public string Lyrics { get; set; }

        [JsonProperty(PropertyName = "artwork_track_id")]
        public string ArtworkID { get; set; }

        [JsonProperty(PropertyName = "remote")]
        public string IsRemote { get; set; }

        [JsonProperty(PropertyName = "remote_title")]
        public string RemoteTitle { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string Location { get; set; }

        [JsonProperty(PropertyName = "comment")]
        public string Comment { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string ContentType { get; set; }

        [JsonProperty(PropertyName = "artwork_url")]
        public string RemoteArtworkUrl { get; set; }

        public string TracknumSort
        {
            get
            {
                if (string.IsNullOrEmpty(Tracknum))
                {
                    return string.Empty;
                }
                else
                {
                    if (string.IsNullOrEmpty(Disc))
                    {
                        return Tracknum.PadLeft(2, '0');
                    }
                    else
                    {
                        return Disc + "." + Tracknum.PadLeft(2, '0');
                    }
                }
            }
        }

        public string PlaylistIndexSort
        {
            get
            {
                if (string.IsNullOrEmpty(PlaylistIndex))
                {
                    return string.Empty;
                }
                else
                {
                    return PlaylistIndex.PadLeft(2, '0');
                }
            }
        }

        public string DurationText
        {
            get
            {
                TimeSpan duration = TimeSpan.FromSeconds(Duration);
                if (duration.TotalHours > 1.0)
                    return string.Format("{0:00}:{1:00}:{2:00}", duration.TotalHours, duration.Minutes, duration.Seconds);
                else
                    return string.Format("{0:00}:{1:00}", duration.Minutes, duration.Seconds);
            }
        }

        private string GetArtworkUrl(int size)
        {
            string artworkurl = "";

            if (string.IsNullOrEmpty(ArtworkID))
            {
                artworkurl = RemoteArtworkUrl != null ? RemoteArtworkUrl : FionaDataService.DefaultArtworkUrl;
            }
            else
            {
                artworkurl = string.Format("{0}music/{1}/cover_{2}x{2}.jpg", FionaDataService.RemoteUrl, ArtworkID, size);
            }
            return artworkurl;
        }

        public string ArtworkUrl
        {
            get
            {
                return GetArtworkUrl(FionaDataService.BigImageSize);
            }
        }

        public string ArtworkUrl_Small
        {
            get
            {
                return GetArtworkUrl(FionaDataService.SmallImageSize);
            }
        }

        public override string ToString()
        {
            return string.Format("{0}", Title);
        }

    }

    public class TrackList : FionaBase
    {
        [JsonProperty(PropertyName = "count")]
        public string Count { get; set; }

        [JsonProperty(PropertyName = "titles_loop")]
        public List<Track> Tracks { get; set; }
    }
}
