using Fiona.Core.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiona.Core.Models
{
    public class Applet : FionaBase
    {
        [JsonProperty(PropertyName = "params")]
        public AppletListItemLoopParams Params { get; set; }

        [JsonProperty(PropertyName = "icon")]
        public string Icon { get; set; }

        [JsonProperty(PropertyName = "icon-id")]
        public string IconID { get; set; }

        [JsonProperty(PropertyName = "actions")]
        public AppletListItemLoopActions Actions { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [JsonProperty(PropertyName = "style")]
        public string Style { get; set; }

        [JsonProperty(PropertyName = "presetParams")]
        public AppletListItemLoopParams PresetParams { get; set; }

        [JsonProperty(PropertyName = "goAction")]
        public string GoAction { get; set; }

        [JsonProperty(PropertyName = "addAction")]
        public string AddAction { get; set; }

        [JsonProperty(PropertyName = "action")]
        public string Action { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        // Loop properties

        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "image")]
        public string Image { get; set; }

        [JsonProperty(PropertyName = "isaudio")]
        public int IsAudio { get; set; }

        [JsonProperty(PropertyName = "hasitems")]
        public int HasItems { get; set; }

        public string GetText
        {
            get
            {
                if (Type != null)
                {
                    switch (Type.ToLower())
                    {
                        case "redirect": return Text;
                        case "playlist":
                            if (IsAudio == 1)
                                return Name;
                            else
                                return Text;
                        case "link":
                            if (string.IsNullOrEmpty(AddAction))
                                return Name;
                            else
                                return Text;
                        default: return Text;
                    }
                }
                else
                {
                    if (Style?.ToLower() == "itemplay" || Style == null)
                        return Text;
                    else
                    if (GoAction?.ToLower() == "playcontrol")
                        return Text;
                    else
                        return Name;
                }
            }
        }

        public string GetImageUrl
        {
            get
            {
                if (Type != null)
                {
                    switch (Type.ToLower())
                    {
                        case "redirect": return string.Format("{0}{1}", FionaDataService.RemoteUrl, IconID);
                        case "playlist":
                            if (!string.IsNullOrEmpty(Icon))
                            {
                                if (Icon.StartsWith("http"))
                                    return Icon;
                                else
                                    return string.Format("{0}{1}", FionaDataService.RemoteUrl, Icon);
                            }
                            else
                                return FionaDataService.DefaultAppImageUrl;
                        case "link":
                            if (!string.IsNullOrEmpty(Icon))
                            {
                                if (Icon.StartsWith("http"))
                                    return Icon;
                                else
                                    return string.Format("{0}{1}", FionaDataService.RemoteUrl, Icon);
                            }
                            else
                            if (!string.IsNullOrEmpty(Image))
                            {
                                if (Image.StartsWith("http"))
                                    return Image;
                                else
                                    return string.Format("{0}{1}", FionaDataService.RemoteUrl, Image);
                            }
                            else
                                return FionaDataService.DefaultAppImageUrl;
                        default: return FionaDataService.DefaultAppImageUrl;
                    }
                }
                else
                {
                    if (Style?.ToLower() == "itemplay" || Style == null)
                    {
                        if (string.IsNullOrEmpty(Icon))
                            return string.Format("{0}{1}", FionaDataService.RemoteUrl, Image);
                        else
                        if (Icon.StartsWith("http"))
                            return Icon;
                        else
                            return string.Format("{0}{1}", FionaDataService.RemoteUrl, Icon);
                    }
                    else
                        return string.Format("{0}{1}", FionaDataService.RemoteUrl, Image);
                }
            }
        }

        public string GetMenu
        {
            get
            {
                if (Type?.ToLower() == "redirect" && AddAction?.ToLower() == "go")
                    return Actions.Go.Params.Menu;
                if (Type?.ToLower() == "link" && AddAction?.ToLower() == "go")
                    return Actions.Go.Params.Menu;
                else
                    return FionaDataService.CurrentAppletMenu;
            }
        }

        public string GetID
        {
            get
            {
                if (GoAction?.ToLower() == "playcontrol")
                    return Params.item_id;
                else
                if (Type?.ToLower() == "link" && AddAction?.ToLower() == "go")
                    return Actions.Go.Params.item_id;
                else
                    if (AddAction?.ToLower() == "play" || Type?.ToLower() == "playlist")
                    return Params.item_id;
                else
                    if (Style?.ToLower() != "itemnoaction")
                    return ID;
                else
                    return "";
            }
        }
    }

    public class AppletList : FionaBase
    {
        //TODO ignoring "base" for now

        [JsonProperty(PropertyName = "count")]
        public string Count { get; set; }

        [JsonProperty(PropertyName = "window")]
        public AppletListWindow window { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "offset")]
        public string Offset { get; set; }

        [JsonProperty(PropertyName = "item_loop")]
        public List<Applet> Applets { get; set; }
    }

    public class LoopApplet : FionaBase
    {

    }

    public class AppletListWindow : FionaBase
    {
        [JsonProperty(PropertyName = "windowStyle")]
        public string windowStyle { get; set; }
    }

    public class AppletListItemLoopParams
    {
        [JsonProperty(PropertyName = "touchToPlay")]
        public string TouchToPlay { get; set; }

        [JsonProperty(PropertyName = "isContextMenu")]
        public int IsContextMenu { get; set; }

        [JsonProperty(PropertyName = "menu")]
        public string Menu { get; set; }

        [JsonProperty(PropertyName = "item_id")]
        public string item_id { get; set; }

        [JsonProperty(PropertyName = "icon")]
        public string Icon { get; set; }

        [JsonProperty(PropertyName = "favorites_type")]
        public string FavoritesType { get; set; }

        [JsonProperty(PropertyName = "favorites_title")]
        public string FavoritesTitle { get; set; }

        [JsonProperty(PropertyName = "favorites_url")]
        public string FavoritesUrl { get; set; }
    }

    public class AppletListItemLoopActions
    {
        [JsonProperty(PropertyName = "go")]
        public AppletListItemLoopAction Go { get; set; }
    }

    public class AppletListItemLoopAction
    {
        [JsonProperty(PropertyName = "params")]
        public AppletListItemLoopParams Params { get; set; }

        [JsonProperty(PropertyName = "cmd")]
        public List<string> Cmd { get; set; } = new List<string>();
    }
}
