using Fiona.Core.Services;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Fiona.Core.Models
{
    public class Album : FionaBase
    {
        [JsonProperty(PropertyName = "disccount")]
        public int DiscCount { get; set; }

        [JsonProperty(PropertyName = "extid")]
        public string ExtID { get; set; }

        [JsonProperty(PropertyName = "artists_id")]
        public int ArtistsIDs { get; set; }

        [JsonProperty(PropertyName = "artwork_track_id")]
        public string ArtworkID { get; set; }

        [JsonProperty(PropertyName = "compilation")]
        public int Compilation { get; set; }

        [JsonProperty(PropertyName = "artist")]
        public string ArtistName { get; set; }

        [JsonProperty(PropertyName = "album")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "artists")]
        public string ArtistsNames { get; set; }

        [JsonProperty(PropertyName = "textkey")]
        public string TextKey { get; set; }

        [JsonProperty(PropertyName = "artist_id")]
        public int ArtistID { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "disc")]
        public int Disc { get; set; }

        [JsonProperty(PropertyName = "year")]
        public string Year { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        public List<Track> Tracks { get; set; }

        public List<Genre> Genres { get; set; }

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

        private string GetArtworkUrl(int size)
        {
            return string.Format("{0}music/{1}/cover_{2}x{2}.jpg", FionaDataService.RemoteUrl, string.IsNullOrEmpty(ArtworkID) ? "0" : ArtworkID, size);
        }
    }

    public class AlbumList : FionaBase
    {
        [JsonProperty(PropertyName = "count")]
        public string Count { get; set; }

        [JsonProperty(PropertyName = "albums_loop")]
        public List<Album> Albums { get; set; }
    }
}
