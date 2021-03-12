using System;
using System.Collections.Generic;
using Fiona.Core.Models;
using Fiona.Core.Services;
using Fiona.Helpers;
using Fiona.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace Fiona.ViewModels
{
    public class ArtistsViewModel : ObservableObject
    {
        public List<Artist> Artists { get => FionaDataService.GetAllArtists().Artists; }

        private RelayCommand _ShuffleAllArtistsCommand;
        public RelayCommand ShuffleAllArtistsCommand => _ShuffleAllArtistsCommand ?? (_ShuffleAllArtistsCommand = new RelayCommand(ShuffleAllArtists));
        private void ShuffleAllArtists()
        {
            //TODO
        }

        private RelayCommand<Artist> _ViewArtistDetailsCommand;
        public RelayCommand<Artist> ViewArtistDetailsCommand => _ViewArtistDetailsCommand ?? (_ViewArtistDetailsCommand = new RelayCommand<Artist>(param => ViewArtistDetails((Artist)param)));
        private void ViewArtistDetails(Artist _Artist)
        {
            NavigationService.Navigate(typeof(Views.ArtistDetailsPage), _Artist, null);
        }

        public ArtistsViewModel()
        {
        }
    }
}
