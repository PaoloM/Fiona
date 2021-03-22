using System;
using Fiona.Core.Models;
using Fiona.Core.Services;
using Fiona.Helpers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

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

        private string CurrentApp = "";

        private System.Collections.Generic.Stack<Applet> stack;

        private RelayCommand<Applet> _AppSelectedCommand;
        public RelayCommand<Applet> AppSelectedCommand => _AppSelectedCommand ?? (_AppSelectedCommand = new RelayCommand<Applet>(param => AppSelected((Applet)param)));
        private void AppSelected(Applet applet)
        {
            //TODO 1. in case of playlist, show tracks and affordances to play all, play track, queue all, queue track, shuffle all
            //TODO 2. use the navigation back button
            //TODO 3. search? text? other types?

            if (applet.Type.ToLower() == "playlist")
            {
                FionaDataService.PlayPlaylistFromApp(FionaDataService.CurrentPlayer,
                    CurrentApp, CurrentApp, applet.Params.item_id);
            }
            else
            {
                if (applet.Style?.ToLower() != "itemnoaction")
                {
                    if (string.IsNullOrEmpty(applet.Actions.Go.Params.item_id))
                    { // this is the first level of an app
                        applet.Actions.Go.Params.item_id = "0";
                        CurrentApp = applet.Actions.Go.Cmd[0];
                    }

                    Apps = FionaDataService.GetApps(FionaDataService.CurrentPlayer,
                        applet.Actions.Go.Cmd[0], applet.Actions.Go.Cmd[1],
                        applet.Actions.Go.Params.Menu, applet.Actions.Go.Params.item_id);

                    stack.Push(applet);
                }
            }

        }

        private RelayCommand _GoBackCommand;
        public RelayCommand GoBackCommand => _GoBackCommand ?? (_GoBackCommand = new RelayCommand(GoBack));
        private void GoBack()
        {
            if (stack.Count > 1)
            {
                var discard = stack.Pop();
                AppSelected(stack.Pop());
            }
            else
            {
                stack.Clear();
                Apps = FionaDataService.GetAllApps(FionaDataService.CurrentPlayer);
            }
        }


        public AppsViewModel()
        {
            Apps = FionaDataService.GetAllApps(FionaDataService.CurrentPlayer);
            stack = new System.Collections.Generic.Stack<Applet>();
        }
    }
}
