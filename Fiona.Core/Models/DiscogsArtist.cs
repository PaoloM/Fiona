using Newtonsoft.Json;
using System.Collections.Generic;

namespace Fiona.Core.Models
{
    public class DiscogsArtist : FionaBase
    {
        [JsonProperty(PropertyName = "id")]
        public int ID { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string EntityType { get; set; }

        [JsonProperty(PropertyName = "uri")]
        public string URI { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "images")]
        public List<DiscogsArtistImage> Images { get; set; } = new List<DiscogsArtistImage>();

        [JsonProperty(PropertyName = "profile")]
        public string Profile { get; set; }

        public override string ToString()
        {
            return string.Format("{0}", Name);
        }
    }

    public class DiscogsArtistImage
    {
        [JsonProperty(PropertyName = "type")]
        public string ImageType { get; set; }

        [JsonProperty(PropertyName = "uri")]
        public string ImageUrl { get; set; }

        [JsonProperty(PropertyName = "uri150")]
        public string ThumbnailUrl { get; set; }

        [JsonProperty(PropertyName = "width")]
        public int Width { get; set; }

        [JsonProperty(PropertyName = "height")]
        public int Height { get; set; }
    }
}
