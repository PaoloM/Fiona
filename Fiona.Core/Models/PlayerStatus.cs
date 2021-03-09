using Fiona.Core.Helpers;
using Fiona.Core.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Fiona.Core.Models
{
    public class PlayerStatus : FionaBase
    {
        [JsonProperty(PropertyName = "seq_no")]
        public int SequenceNumber { get; set; }

        private int _MixerVolume;
        [JsonProperty(PropertyName = "mixer volume")]
        public int MixerVolume {
            get => _MixerVolume;
            set
            {
                _MixerVolume = value;
                FionaDataService.SetVolume(FionaDataService.CurrentPlayer, (double)value);
            }
        }

        private int StoredVolume = 0;

        private bool _IsMuted;
        public bool IsMuted
        {
            get => _IsMuted;
            set
            {
                _IsMuted = value;
                if (_IsMuted)
                {
                    StoredVolume = _MixerVolume;
                    FionaDataService.MuteVolume(FionaDataService.CurrentPlayer, true);
                }
                else
                {
                    MixerVolume = StoredVolume;
                    FionaDataService.MuteVolume(FionaDataService.CurrentPlayer, false);
                }
            }
        }

        [JsonProperty(PropertyName = "playlist_tracks")]
        public int PlaylistTracksCount { get; set; }

        [JsonProperty(PropertyName = "player_connected")]
        public bool PlayerConnected { get; set; }

        [JsonProperty(PropertyName = "time")]
        public float Time { get; set; } // PM:20201227 - Changed int to float to allow for new json responses

        [JsonProperty(PropertyName = "mode")]
        public PlayerMode Mode { get; set; }

        [JsonProperty(PropertyName = "playlist_timestamp")]
        public string PlaylistTimestamp { get; set; }

        [JsonProperty(PropertyName = "rate")]
        public int Rate { get; set; }

        [JsonProperty(PropertyName = "can_seek")]
        public bool CanSeek { get; set; }

        [JsonProperty(PropertyName = "power")]
        public bool Power { get; set; }

        [JsonProperty(PropertyName = "playlist mode")]
        public string PlaylistMode { get; set; }

        [JsonProperty(PropertyName = "playlist repeat")]
        public bool PlaylistRepeat { get; set; }

        [JsonProperty(PropertyName = "duration")]
        public double Duration { get; set; }

        [JsonProperty(PropertyName = "playlist_cur_index")]
        public int PlaylistCurrentIndex { get; set; }

        [JsonProperty(PropertyName = "signalstrength")]
        public int SignalStrength { get; set; }

        [JsonProperty(PropertyName = "playlist shuffle")]
        public bool PlaylistShuffle { get; set; }

        [JsonProperty(PropertyName = "player_name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "player_ip")]
        public string IP { get; set; }

        [JsonProperty(PropertyName = "remoteMeta")]
        public Track CurrentRemoteSong { get; set; }

        [JsonProperty(PropertyName = "playlist_loop")]
        public List<Track> Playlist { get; set; }

        public Track CurrentSong
        {
            get
            {
                if (CurrentRemoteSong != null)
                    return CurrentRemoteSong;
                else
                {
                    if (Playlist != null)
                    {
                        if (Playlist.Count > 1)
                            return Playlist[PlaylistCurrentIndex];
                        else
                            return Playlist[0];
                    }
                    else
                        return null;
                }
            }
        }

        public String TimeText
        {
            get
            {
                TimeSpan duration = TimeSpan.FromSeconds(Time);
                if (duration.TotalHours > 1.0)
                    return string.Format("{0:00}:{1:00}:{2:00}", duration.TotalHours, duration.Minutes, duration.Seconds);
                else
                    return string.Format("{0:00}:{1:00}", duration.Minutes, duration.Seconds);
            }
        }
    }
}
