using System;
using Fiona.Core.Models;
using Fiona.Core.Services;
using Fiona.Helpers;
using Fiona.Services;
using Fiona.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Windows.UI.Xaml;

namespace Fiona.ViewModels
{
    public class AppsViewModel : BaseViewModel
    {
        private AppletList _Apps;
        public AppletList Apps
        {
            get => _Apps;
            set => SetProperty(ref _Apps, value);
        }

        private Visibility _PlaylistCommandsVisibility;
        public Visibility PlaylistCommandsVisibility
        {
            get => _PlaylistCommandsVisibility;
            set => SetProperty(ref _PlaylistCommandsVisibility, value);
        }

        private string _AppletTitle;
        public string AppletTitle
        {
            get => _AppletTitle;
            set => SetProperty(ref _AppletTitle, value);
        }

        private string _AppletIconUrl = FionaDataService.DefaultArtworkUrl;
        public string AppletIconUrl
        {
            get => _AppletIconUrl;
            set => SetProperty(ref _AppletIconUrl, value);
        }

        private string _TileTitle;
        public string TileTitle
        {
            get => _TileTitle;
            set => SetProperty(ref _TileTitle, value);
        }

        private Applet _CurrentApplet = new Applet();
        public Applet CurrentApplet
        {
            get => _CurrentApplet;
            set
            {
                FionaDataService.CurrentApplet = value;

                if (value == null) // coming from the Shell, show all apps
                {
                    PlaylistCommandsVisibility = Visibility.Collapsed;
                    Apps = FionaDataService.GetAllApps(FionaDataService.CurrentPlayer);
                }
                else
                {
                    if (value.Type == null)
                        value.Type = "link";

                    AppletTitle = FionaDataService.CurrentAppletName;
                    AppletIconUrl = string.IsNullOrEmpty(FionaDataService.CurrentAppletIconUrl) ? FionaDataService.DefaultArtworkUrl : FionaDataService.CurrentAppletIconUrl;

                    switch (value.Type.ToLower())
                    {
                        case "playlist": // show all tracks and the transport common bar
                            PlaylistCommandsVisibility = Visibility.Visible;
                            Apps = FionaDataService.GetApps(FionaDataService.CurrentPlayer,
                                   FionaDataService.CurrentAppletName, "items",
                                   FionaDataService.CurrentAppletName,
                                   value.Params?.item_id != null ? value.Params.item_id : value.ID);
                            TileTitle = Apps.Title;
                            break;
                        case "link":
                            PlaylistCommandsVisibility = Visibility.Collapsed;
                            if (value.ID != null)
                                Apps = FionaDataService.GetApps(FionaDataService.CurrentPlayer,
                                       FionaDataService.CurrentAppletName, "items",
                                       FionaDataService.CurrentAppletName, value.ID);
                            else
                                if (value.Actions == null)
                                Apps = FionaDataService.GetApps(FionaDataService.CurrentPlayer,
                                       FionaDataService.CurrentAppletName, "items",
                                       FionaDataService.CurrentAppletName, value.Params.item_id);
                            else
                            Apps = FionaDataService.GetApps(FionaDataService.CurrentPlayer,
                                   value.Actions.Go.Cmd[0], value.Actions.Go.Cmd[1],
                                   value.Actions.Go.Params.Menu, value.Actions.Go.Params.item_id);
                            TileTitle = Apps.Title;
                            break;
                        case "redirect": // top level of an app
                            PlaylistCommandsVisibility = Visibility.Collapsed;
                            Apps = FionaDataService.GetAppTopLevel(FionaDataService.CurrentPlayer,
                                value.Name.ToLower());
                            FionaDataService.CurrentAppletName = value.Name.ToLower();
                            FionaDataService.CurrentAppletIconUrl = value.GetIconUrl;
                            AppletTitle = FionaDataService.CurrentAppletName;
                            AppletIconUrl = FionaDataService.CurrentAppletIconUrl;
                            break;
                        default:
                            break;
                    }
                }
                SetProperty(ref _CurrentApplet, value);
            }
        }

        private RelayCommand _PlayPlaylistCommand;
        public RelayCommand PlayPlaylistCommand => _PlayPlaylistCommand ?? (_PlayPlaylistCommand = new RelayCommand(PlayPlaylist));
        private void PlayPlaylist()
        {
            FionaDataService.PlayPlaylistFromApp(FionaDataService.CurrentPlayer, FionaDataService.CurrentAppletName, FionaDataService.CurrentAppletName, FionaDataService.CurrentApplet.Params.item_id);
        }

        private RelayCommand _QueuePlaylistCommand;
        public RelayCommand QueuePlaylistCommand => _QueuePlaylistCommand ?? (_QueuePlaylistCommand = new RelayCommand(QueuePlaylist));
        private void QueuePlaylist()
        {
            //TODO
        }

        private RelayCommand<Applet> _AppSelectedCommand;
        public RelayCommand<Applet> AppSelectedCommand => _AppSelectedCommand ?? (_AppSelectedCommand = new RelayCommand<Applet>(param => AppSelected((Applet)param)));
        private void AppSelected(Applet applet)
        {
            NavigationService.Navigate<AppsPage>(applet);
        }

        public AppsViewModel()
        {
        }
    }
}
