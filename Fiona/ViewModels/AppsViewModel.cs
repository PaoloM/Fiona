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

        public string CurrentAppletName
        {
            get => FionaDataService.CurrentAppletName;
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
            //TODO 1. in case of playlist, show tracks and affordances to play all, play track, queue all, queue track, shuffle all
            //TODO 3. search? text? other types?

            //if (applet.Action != null) CurrentAppName = applet.Actions?.Go.Cmd[0];

            if (applet.Type.ToLower() == "playlist")
            {
                PlaylistCommandsVisibility = Visibility.Visible;
                FionaDataService.CurrentApplet = applet;
                NavigationService.Navigate<AppsPage>(applet);
            }
            else
            {
                PlaylistCommandsVisibility = Visibility.Collapsed;
                if (applet.Style?.ToLower() != "itemnoaction")
                {
                    if (string.IsNullOrEmpty(applet.Actions.Go.Params.item_id))
                    { // this is the first level of an app
                        applet.Actions.Go.Params.item_id = "0";
                        FionaDataService.CurrentAppletName = applet.Actions.Go.Cmd[0];
                    }

                    FionaDataService.CurrentApplet = applet;
                    NavigationService.Navigate<AppsPage>(applet);

                    //Apps = FionaDataService.GetApps(FionaDataService.CurrentPlayer,
                    //    applet.Actions.Go.Cmd[0], applet.Actions.Go.Cmd[1],
                    //    applet.Actions.Go.Params.Menu, applet.Actions.Go.Params.item_id);
                }
            }

        }

        public AppsViewModel()
        {
            if (FionaDataService.CurrentApplet == null) // coming from the Shell, show all apps
                Apps = FionaDataService.GetAllApps(FionaDataService.CurrentPlayer);
            else
            {
                if (FionaDataService.CurrentApplet.Actions != null)
                {
                    Apps = FionaDataService.GetApps(FionaDataService.CurrentPlayer,
                           FionaDataService.CurrentApplet.Actions.Go.Cmd[0], FionaDataService.CurrentApplet.Actions.Go.Cmd[1],
                           FionaDataService.CurrentApplet.Actions.Go.Params.Menu, FionaDataService.CurrentApplet.Actions.Go.Params.item_id);
                }
                else
                {
                    Apps = FionaDataService.GetApps(FionaDataService.CurrentPlayer,
                           FionaDataService.CurrentApplet.Actions.Go.Cmd[0], FionaDataService.CurrentApplet.Actions.Go.Cmd[1],
                           FionaDataService.CurrentApplet.Actions.Go.Params.Menu, FionaDataService.CurrentApplet.Params.item_id);
                }
            }
        }
    }
}
