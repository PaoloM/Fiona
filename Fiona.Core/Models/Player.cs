using Fiona.Core.Services;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Fiona.Core.Models
{
    public class Player : FionaBase
    {
        [JsonProperty(PropertyName = "playerid")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "uuid")]
        public string UUID { get; set; }

        [JsonProperty(PropertyName = "ip")]
        public string IP { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "displaytype")]
        public string DisplayType { get; set; }

        [JsonProperty(PropertyName = "connected")]
        public bool Connected { get; set; }

        [JsonProperty(PropertyName = "isplayer")]
        public bool IsPlayer { get; set; }

        [JsonProperty(PropertyName = "canpoweroff")]
        public bool CanPowerOff { get; set; }

        [JsonProperty(PropertyName = "model")]
        public string Model { get; set; }

        [JsonProperty(PropertyName = "isplaying")]
        public bool IsPlaying { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public string ArtworkUrl
        {
            get
            {
                string imageLocation = string.Format(@"html/images/Players/{0}_75x75_ffffff.png", Model);
                return string.Format("{0}{1}", FionaDataService.RemoteUrl, imageLocation);
            }
        }
    }

    public class PlayerList : FionaBase
    {
        [JsonProperty(PropertyName = "count")]
        public string Count { get; set; }

        [JsonProperty(PropertyName = "players_loop")]
        public List<Player> Players { get; set; }
    }
}
