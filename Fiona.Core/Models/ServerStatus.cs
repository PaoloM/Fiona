using Newtonsoft.Json;

namespace Fiona.Core.Models
{
    public class ServerStatus : FionaBase
    {
        [JsonProperty(PropertyName = "uuid")]
        public string UUID { get; set; }

        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }

        [JsonProperty(PropertyName = "rescan")]
        public int Rescan { get; set; }

        [JsonProperty(PropertyName = "lastscan")]
        public int LastScan { get; set; }

        [JsonProperty(PropertyName = "info total artists")]
        public int TotalArtists { get; set; }

        [JsonProperty(PropertyName = "info total songs")]
        public int TotalSongs { get; set; }

        [JsonProperty(PropertyName = "info total genres")]
        public int TotalGenres { get; set; }

        [JsonProperty(PropertyName = "info total albums")]
        public int TotalAlbums { get; set; }

        [JsonProperty(PropertyName = "other player count")]
        public int OtherPlayerCount { get; set; }

        [JsonProperty(PropertyName = "player count")]
        public int PlayerCount { get; set; }

        [JsonProperty(PropertyName = "sn player count")]
        public int SnPlayerCount { get; set; }
    }
}
