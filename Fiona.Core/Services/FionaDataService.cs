using Fiona.Core.Helpers;
using Fiona.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fiona.Core.Services
{
    public static class FionaDataService
    {
        public static string ServerIP { get; set; }
        public static int ServerPort { get; set; }

        public static string Username;
        public static string Password;

        public static DataMode Mod { get; set; }

        public static Player CurrentPlayer { get; set; }

        public static Applet CurrentApplet { get; set; }
        public static string CurrentAppletId { get; set; }

        public static AlbumList AllAlbums { get; set; }
        public static ArtistList AllArtists { get; set; }
        public static GenreList AllGenres { get; set; }
        public static AppletList AllApps { get; set; }

        #region Commands

        #region Server/Player
        public static bool ContactServer(string url, int port)
        {
            ServerIP = url;
            ServerPort = port;
            try
            {
                var msg = FionaMessage.CreateMessage(FionaCommand.ServerStatus);
                var res = QueryWebServiceWithPost<ServerStatus>(RemoteUrlJson, msg);
                return true;
            } catch (Exception ex)
            {
                return false;
            }
        }

        public static ServerStatus GetServerStatus()
        {
            var msg = FionaMessage.CreateMessage(FionaCommand.ServerStatus);
            return QueryWebServiceWithPost<ServerStatus>(RemoteUrlJson, msg);
        }

        public static PlayerList GetAllPlayers()
        {
            var msg = FionaMessage.CreateMessage(FionaCommand.Players, "0", FionaCommand.MaxItems);
            return QueryWebServiceWithPost<PlayerList>(RemoteUrlJson, msg);
        }

        public static PlayerStatus GetPlayerStatus(Player player)
        {
            var msg = FionaMessage.CreateMessage(player, FionaCommand.Status, "0", FionaCommand.MaxItems, FionaCommand.PlayerSelectTags);
            return QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }
        #endregion

        #region Album
        public static AlbumList GetAllAlbums()
        {
            var msg = FionaMessage.CreateMessage(FionaCommand.Albums, "0", FionaCommand.MaxItems, FionaCommand.AlbumTags);
            AllAlbums = QueryWebServiceWithPost<AlbumList>(RemoteUrlJson, msg);
            return AllAlbums;
        }

        public static TrackList GetAllTracksByAlbum(Album album)
        {
            var msg = FionaMessage.CreateMessage(FionaCommand.Songs, "0", FionaCommand.MaxItems, FionaCommand.TrackTags, string.Format(FionaCommand.AlbumSelect, album.ID));
            return QueryWebServiceWithPost<TrackList>(RemoteUrlJson, msg);
        }

        public static GenreList GetAllGenresByAlbum(Album album)
        {
            var msg = FionaMessage.CreateMessage(FionaCommand.Genres, "0", FionaCommand.MaxItems, FionaCommand.GenreTags, string.Format(FionaCommand.AlbumSelect, album.ID));
            return QueryWebServiceWithPost<GenreList>(RemoteUrlJson, msg);
        }
        #endregion

        #region Artists
        private static ArtistList _allArtists = null;
        public static ArtistList GetAllArtists()
        {
            if (_allArtists == null)
            {
                var msg = FionaMessage.CreateMessage(FionaCommand.Artists, "0", FionaCommand.MaxItems, FionaCommand.ArtistTags);
                AllArtists = QueryWebServiceWithPost<ArtistList>(RemoteUrlJson, msg);
            } else
            {
                AllArtists = _allArtists;
            }
            return AllArtists;
        }

        public static AlbumList GetAllAlbumsByArtist(Artist artist)
        {
            var msg = FionaMessage.CreateMessage(FionaCommand.Albums, "0", FionaCommand.MaxItems, string.Format(FionaCommand.ArtistSelect, artist.ID), FionaCommand.AlbumTags);
            return QueryWebServiceWithPost<AlbumList>(RemoteUrlJson, msg);
        }

        public static TrackList GetAllTracksByArtist(Artist artist)
        {
            var msg = FionaMessage.CreateMessage(FionaCommand.Songs, "0", FionaCommand.MaxItems, FionaCommand.TrackTags, string.Format(FionaCommand.ArtistSelect, artist.ID));
            return QueryWebServiceWithPost<TrackList>(RemoteUrlJson, msg);
        }

        public static GenreList GetAllGenresByArtist(Artist artist)
        {
            var msg = FionaMessage.CreateMessage(FionaCommand.Genres, "0", FionaCommand.MaxItems, FionaCommand.GenreTags, string.Format(FionaCommand.ArtistSelect, artist.ID));
            return QueryWebServiceWithPost<GenreList>(RemoteUrlJson, msg);
        }
        #endregion

        #region Genres
        public static GenreList GetAllGenres()
        {
            var msg = FionaMessage.CreateMessage(FionaCommand.Genres, "0", FionaCommand.MaxItems, FionaCommand.GenreTags);
            AllGenres = QueryWebServiceWithPost<GenreList>(RemoteUrlJson, msg);
            return AllGenres;
        }
        #endregion

        #region Playlist
        public static void ClearPlaylist(Player player)
        {
            var msg = FionaMessage.CreateMessage(player, "playlist", "clear");
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void PlaylistLoadAndPlayAlbum(Player player, Album album)
        {
            var msg = FionaMessage.CreateMessage(player, "playlistcontrol", "cmd:load", "album_id:" + album.ID.ToString());
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void PlaylistAppendAlbum(Player player, Album album)
        {
            var msg = FionaMessage.CreateMessage(player, "playlistcontrol", "cmd:add", "album_id:" + album.ID.ToString());
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void PlaylistPlayTrack(Player player, string track_url)
        {
            var msg = FionaMessage.CreateMessage(player, "playlist", "play", track_url);
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void PlaylistAddTrackToQueue(Player player, string track_url)
        {
            var msg = FionaMessage.CreateMessage(player, "playlist", "add", track_url);
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void ShuffleAllAlbums(Player player)
        {
            // clear the current playlist

            // load the playlist with all albums

            // shuffle the playlist
            TransportShuffle(player);
        }
        #endregion

        #region Transport
        public static void TransportPause(Player player)
        {
            var msg = FionaMessage.CreateMessage(player, "pause");
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void TransportPrevious(Player player)
        {
            var msg = FionaMessage.CreateMessage(player, "playlist", "index", "-1");
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void TransportNext(Player player)
        {
            var msg = FionaMessage.CreateMessage(player, "playlist", "index", "+1");
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void TransportShuffle(Player player)
        {
            var msg = FionaMessage.CreateMessage(player, "playlist", "shuffle");
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void TransportRepeat(Player player)
        {
            var msg = FionaMessage.CreateMessage(player, "playlist", "repeat");
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }
        #endregion

        #region Apps
        public static AppletList GetApps()
        {
            var msg = FionaMessage.CreateMessage("apps", "0", FionaCommand.MaxItems);
            AllApps = QueryWebServiceWithPost<AppletList>(RemoteUrlJson, msg);
            return AllApps;
        }

        public static AppletList GetApps(Player player, string app)
        {
            var msg = FionaMessage.CreateMessage(player, app, "items", "0", FionaCommand.MaxItems);
            return QueryWebServiceWithPost<AppletList>(RemoteUrlJson, msg);
        }

        public static AppletList GetApps(Player player, string app, string id)
        {
            var msg = FionaMessage.CreateMessage(player, app, "items", "0", FionaCommand.MaxItems, "item_id:" + id);
            var res = QueryWebServiceWithPost<AppletList>(RemoteUrlJson, msg);
            return res;
        }
        #endregion

        #region Misc
        public static void SetVolume(Player player, double volume)
        {
            var msg = FionaMessage.CreateMessage(player, "mixer", "volume", volume.ToString());
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void ToggleMuteVolume(Player player)
        {
            var msg = FionaMessage.CreateMessage(player, "mixer", "muting", "toggle");
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void MuteVolume(Player player, bool mute)
        {
            var msg = FionaMessage.CreateMessage(player, "mixer", "muting", mute ? "1" : "0");
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        #endregion

        #endregion

        #region Plumbing
        public static string RemoteUrl
        {
            get
            {
                return string.IsNullOrEmpty(ServerIP) ? "" : $"http://{ServerIP}:{ServerPort}/";
            }
        }

        public static string RemoteUrlJson
        {
            get
            {
                return string.IsNullOrEmpty(ServerIP) ? "" : $"{RemoteUrl}jsonrpc.js";
            }
        }

        public static int BigImageSize { get => 350; }
        public static int SmallImageSize { get => 75; }

        public static string DefaultArtworkUrl
        {
            get => $"{RemoteUrl}music/0/cover_{BigImageSize}x{BigImageSize}.jpg";
        }

        private static HttpClient client = new HttpClient();

        private static T QueryWebServiceWithPost<T>(string url, string msg)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(ServerIP))
            {
                return default(T);
            }
            else
            {
                //TODO authentication
                var content = new StringContent(msg, Encoding.UTF8, "application/json");
                // send the message and wait for the response
                var response = client.PostAsync(url, content);
                // read the response
                string res = "";
               
                using (HttpContent c = response.Result.Content)
                {
                    Task<string> result = c.ReadAsStringAsync();
                    res = result.Result;
                }
              
                JObject o = JObject.Parse(res);
                var jsonResult = o["result"];
                var outval = JsonConvert.DeserializeObject<T>(jsonResult.ToString());
                return outval;
            }
        }
        #endregion
    }
}
