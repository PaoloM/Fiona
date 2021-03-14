using Newtonsoft.Json;
using System.Collections.Generic;

namespace Fiona.Core.Models
{
    public class Artist : FionaBase
    {
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "artist")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "textkey")]
        public string TextKey { get; set; }

        [JsonProperty(PropertyName = "extid")]
        public string ExtID { get; set; }

        public List<Album> Albums { get; set; } = new List<Album>();

        public List<Genre> Genres { get; set; } = new List<Genre>();

        public List<Track> Tracks { get; set; } = new List<Track>();

        public string Profile { get; set; }

        public List<string>Images { get; set; }

        public override string ToString()
        {
            return string.Format("{0}", Name);
        }
    }

    public class ArtistList : FionaBase
    {
        [JsonProperty(PropertyName = "count")]
        public string Count { get; set; }

        [JsonProperty(PropertyName = "artists_loop")]
        public List<Artist> Artists { get; set; }
    }
}
