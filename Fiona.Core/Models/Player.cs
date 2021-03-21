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

        [JsonProperty(PropertyName = "modelname")]
        public string ModelName { get; set; }

        [JsonProperty(PropertyName = "isplaying")]
        public bool IsPlaying { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public string IconGlyph
        {
            get
            {
                string iconglyph = "\u0042"; // default, just to make sure
                switch (ModelName.ToLower())
                {
                    case "hifiberry": iconglyph = "\u0055"; break;
                    case "castbridge": iconglyph = "\u004A"; break;
                    case "squeezeplay": iconglyph = "\u0042"; break;
                    default: iconglyph = "\u0042"; break;
                }
                return iconglyph;
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
