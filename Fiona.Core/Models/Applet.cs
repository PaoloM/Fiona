using Fiona.Core.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiona.Core.Models
{
    public class Applet : FionaBase
    {
        [JsonProperty(PropertyName = "icon")]
        public string Icon { get; set; }

        [JsonProperty(PropertyName = "cmd")]
        public string Cmd { get; set; }

        [JsonProperty(PropertyName = "weight")]
        public int Weight { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "image")]
        public string Image { get; set; }

        [JsonProperty(PropertyName = "isaudio")]
        public int IsAudio { get; set; }

        [JsonProperty(PropertyName = "hasitems")]
        public int HasItems { get; set; }

        public string GetIconUrl
        {
            get {
                if (string.IsNullOrEmpty(Icon))
                {
                    if ((Image != null) && (!Image.ToLower().StartsWith("http")))
                        return string.Format("{0}{1}", FionaDataService.RemoteUrl, Image);
                    else
                        return Image;
                }
                else
                {
                    return string.Format("{0}{1}", FionaDataService.RemoteUrl, Icon);
                }
            }
        }
    }

    public class AppletList : FionaBase
    {
        [JsonProperty(PropertyName = "count")]
        public string Count { get; set; }

        [JsonProperty(PropertyName = "appss_loop")]
        public List<Applet> Applets { get; set; }

        [JsonProperty(PropertyName = "loop_loop")]
        public List<Applet> Cmds { get; set; }

        public bool IsTopLevel
        {
            get => Applets != null;
        }
    }
}
