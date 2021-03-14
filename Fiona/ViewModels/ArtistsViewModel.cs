using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Fiona.Core.Models;
using Fiona.Core.Services;
using Fiona.Helpers;
using Fiona.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace Fiona.ViewModels
{
    public class ArtistsViewModel : BaseViewModel
    {
        private ObservableCollection<GroupInfosList> _artists = new ObservableCollection<GroupInfosList>();
        public ObservableCollection<GroupInfosList> GroupedArtists
        {
            get => _artists;
            set => _artists = value;
        }

        public void GroupArtistsByInitial(List<Artist> artists)
        {
            var query = from item in artists
                        group item by item.TextKey into g
                        orderby g.Key
                        select new { GroupName = g.Key, Items = g };

            foreach (var g in query)
            {
                GroupInfosList info = new GroupInfosList
                {
                    Key = g.GroupName + " (" + g.Items.Count() + ")"
                };

                foreach (var item in g.Items)
                {
                    info.Add(item);
                }

                GroupedArtists.Add(info);
            }
        }

        public ArtistsViewModel()
        {
            GroupArtistsByInitial(Artists);
        }
    }

    public class GroupInfosList : List<object>
    {
        public object Key { get; set; }
    }

}
