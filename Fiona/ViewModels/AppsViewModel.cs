﻿using System;
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

        private Visibility _PlaylistCommandsVisibility = Visibility.Collapsed;
        public Visibility PlaylistCommandsVisibility
        {
            get => _PlaylistCommandsVisibility;
            set => SetProperty(ref _PlaylistCommandsVisibility, value);
        }

        private Visibility _AppsGridViewVisibility = Visibility.Visible;
        public Visibility AppsGridViewVisibility
        {
            get => _AppsGridViewVisibility;
            set => SetProperty(ref _AppsGridViewVisibility, value);
        }

        private Visibility _AppsListViewVisibility = Visibility.Collapsed;
        public Visibility AppsListViewVisibility
        {
            get => _AppsListViewVisibility;
            set => SetProperty(ref _AppsListViewVisibility, value);
        }

        private Visibility _TransportControlsVisibility = Visibility.Collapsed;
        public Visibility TransportControlsVisibility
        {
            get => _TransportControlsVisibility;
            set => SetProperty(ref _TransportControlsVisibility, value);
        }

        private string _AppletTitle;
        public string AppletTitle
        {
            get => _AppletTitle;
            set => SetProperty(ref _AppletTitle, value);
        }

        private string _AppletIconUrl = FionaDataService.DefaultAlbumImageUrl;
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
                    AppletTitle = FionaDataService.CurrentAppletName;
                    AppletIconUrl = string.IsNullOrEmpty(FionaDataService.CurrentAppletIconUrl) ? FionaDataService.DefaultAlbumImageUrl : FionaDataService.CurrentAppletIconUrl;

                    PlaylistCommandsVisibility = value.Type?.ToLower() == "playlist" ? Visibility.Visible : Visibility.Collapsed;

                    if (value.Type?.ToLower() == "redirect")
                    { // app top level
                        FionaDataService.CurrentAppletName = value.Name;
                        FionaDataService.CurrentAppletMenu = value.GetMenu;
                        FionaDataService.CurrentAppletIconUrl = value.GetImageUrl;
                        AppletTitle = FionaDataService.CurrentAppletName;
                        AppletIconUrl = FionaDataService.CurrentAppletIconUrl;
                        Apps = FionaDataService.GetAppTopLevel(FionaDataService.CurrentPlayer, value.GetMenu);
                    }
                    else
                    { // app tree traversal
                        Apps = FionaDataService.GetApps(FionaDataService.CurrentPlayer,
                            FionaDataService.CurrentAppletMenu, "items", value.GetMenu, value.GetID);
                        TileTitle = Apps.Title;
                        if (Apps.window.WindowStyle == "text_list")
                        {
                            AppsGridViewVisibility = Visibility.Collapsed;
                            AppsListViewVisibility = Visibility.Visible;
                        }
                        else // WindowStyle == "icon_list" || "home_menu"
                        {
                            AppsGridViewVisibility = Visibility.Visible;
                            AppsListViewVisibility = Visibility.Collapsed;
                        }
                    }
                }
                SetProperty(ref _CurrentApplet, value);
            }
        }

        private RelayCommand _PlayPlaylistCommand;
        public RelayCommand PlayPlaylistCommand => _PlayPlaylistCommand ?? (_PlayPlaylistCommand = new RelayCommand(PlayPlaylist));
        private void PlayPlaylist()
        {
            FionaDataService.PlayPlaylistFromApp(FionaDataService.CurrentPlayer, FionaDataService.CurrentAppletMenu, FionaDataService.CurrentAppletMenu, FionaDataService.CurrentApplet.Params.item_id);
        }

        private RelayCommand _QueuePlaylistCommand;
        public RelayCommand QueuePlaylistCommand => _QueuePlaylistCommand ?? (_QueuePlaylistCommand = new RelayCommand(QueuePlaylist));
        private void QueuePlaylist()
        {
            FionaDataService.QueuePlaylistFromApp(FionaDataService.CurrentPlayer, FionaDataService.CurrentAppletMenu, FionaDataService.CurrentAppletMenu, FionaDataService.CurrentApplet.Params.item_id);
        }

        private RelayCommand _FavoritePlaylistCommand;
        public RelayCommand FavoritePlaylistCommand => _FavoritePlaylistCommand ?? (_FavoritePlaylistCommand = new RelayCommand(FavoritePlaylist));
        private void FavoritePlaylist()
        {
            // TODO
        }

        private RelayCommand<Applet> _AppSelectedCommand;
        public RelayCommand<Applet> AppSelectedCommand => _AppSelectedCommand ?? (_AppSelectedCommand = new RelayCommand<Applet>(param => AppSelected((Applet)param)));
        private void AppSelected(Applet applet)
        {
            // navigate only if it makes sense
            if (applet.GoAction is null)
            {
                NavigationService.Navigate<AppsPage>(applet);
            }
        }

        public AppsViewModel()
        {
        }
    }
}
