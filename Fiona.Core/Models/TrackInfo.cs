using Newtonsoft.Json;

namespace Fiona.Core.Models
{
    public class TrackInfo
    {
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "artist")]
        public string Artist { get; set; }

        [JsonProperty(PropertyName = "coverid")]
        public string CoverID { get; set; }

        [JsonProperty(PropertyName = "duration")]
        public string Duration { get; set; }

        [JsonProperty(PropertyName = "coverart")]
        public string CoverArt { get; set; }

        [JsonProperty(PropertyName = "album")]
        public string Album { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "bitrate")]
        public string Bitrate { get; set; }

        [JsonProperty(PropertyName = "remote")]
        public string Remote { get; set; }

        [JsonProperty(PropertyName = "year")]
        public string Year { get; set; }

        [JsonProperty(PropertyName = "artwork_url")]
        public string ArtworkUrl { get; set; }

        public override string ToString()
        {
            return string.Format("{0}", Title);
        }
    }
}
