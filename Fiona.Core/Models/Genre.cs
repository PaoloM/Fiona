using Fiona.Core.Services;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Fiona.Core.Models
{
    public class Genre : FionaBase
    {
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "genre")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "textkey")]
        public string TextKey { get; set; }
    }

    public class GenreList : FionaBase
    {
        [JsonProperty(PropertyName = "count")]
        public string Count { get; set; }

        [JsonProperty(PropertyName = "genres_loop")]
        public List<Genre> Genres { get; set; }
    }
}
