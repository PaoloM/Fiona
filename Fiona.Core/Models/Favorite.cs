using Fiona.Core.Services;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Fiona.Core.Models
{
    public class Favorite : FionaBase
    {
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "image")]
        public string Image { get; set; }

        [JsonProperty(PropertyName = "isaudio")]
        public int IsAudio { get; set; }

        [JsonProperty(PropertyName = "hasitems")]
        public int HasItems { get; set; }

        public string GetImageUrl
        {
            get
            {
                if (Image == null)
                {
                    return FionaDataService.DefaultAppImageUrl;
                }
                else
                {
                    return Image.StartsWith("http") ? Image : string.Format("{0}{1}", FionaDataService.RemoteUrl, Image);
                }
            }
        }
    }

    public class FavoriteList : FionaBase
    {
        [JsonProperty(PropertyName = "count")]
        public string Count { get; set; }

        [JsonProperty(PropertyName = "item_loop")]
        public List<Favorite> Favorites { get; set; }
    }
}
