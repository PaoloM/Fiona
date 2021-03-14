using Newtonsoft.Json;
using System.Collections.Generic;

namespace Fiona.Core.Models
{
    public class DiscogsSearchResult : FionaBase
    {
        [JsonProperty(PropertyName = "id")]
        public int ID { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string EntityType { get; set; }

        [JsonProperty(PropertyName = "uri")]
        public string URI { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "thumb")]
        public string ThumbnailUrl { get; set; }

        [JsonProperty(PropertyName = "cover_image")]
        public string ImagelUrl { get; set; }

        [JsonProperty(PropertyName = "profile")]
        public string Profile { get; set; }

        public override string ToString()
        {
            return string.Format("{0}", Name);
        }
    }
}
