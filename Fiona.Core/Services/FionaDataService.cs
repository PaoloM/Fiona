using Fiona.Core.Helpers;
using Fiona.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
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
        public static string CurrentAppletName { get; set; }
        public static string CurrentAppletMenu { get; set; }
        public static string CurrentAppletIconUrl { get; set; }

        public static AlbumList AllAlbums { get; set; }
        public static ArtistList AllArtists { get; set; }
        public static GenreList AllGenres { get; set; }
        public static AppletList AllApps { get; set; }
        public static AppletList AllRadios { get; set; }
        public static FavoriteList AllFavorites { get; set; }

        /// <summary>
        /// Raised when a request does not come back with a usable answer, carrying something short
        /// enough to show a person. Queries return nothing rather than throwing, so without this a
        /// caller cannot tell an unreachable server from an empty library.
        ///
        /// Raised on whichever thread made the request, which is not necessarily the UI one -
        /// marshal before touching anything bound to the UI.
        /// </summary>
        public static event EventHandler<string> RequestFailed;

        /// <summary>
        /// False from the moment a request fails until one succeeds again. Starts true so a freshly
        /// started app does not claim to be offline before it has tried anything.
        /// </summary>
        public static bool IsServerReachable { get; private set; } = true;

        /// <summary>
        /// Drops the cached library, so the next read fetches it afresh. Needed whenever the
        /// server changes: the albums and artists of the old one say nothing about the new one.
        /// </summary>
        public static void InvalidateLibrary()
        {
            AllAlbums = null;
            AllArtists = null;
            AllGenres = null;
            AllFavorites = null;
        }

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
            } catch 
            {
                return false;
            }
        }

        /// <summary>
        /// Checks that a Logitech Media Server is really answering at this address, within
        /// the given timeout and without disturbing the address the app is currently using.
        /// </summary>
        public static async Task<bool> IsServerReachableAsync(string server, int port, TimeSpan timeout)
        {
            if (string.IsNullOrEmpty(server))
            {
                return false;
            }

            string url = $"http://{server}:{port}/jsonrpc.js";

            using (var cts = new CancellationTokenSource(timeout))
            {
                try
                {
                    var content = new StringContent(FionaMessage.CreateMessage(FionaCommand.ServerStatus), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content, cts.Token);
                    if (!response.IsSuccessStatusCode)
                    {
                        return false;
                    }

                    // Any web server will answer here; only ours answers with a JSON-RPC result.
                    JObject o = JObject.Parse(await response.Content.ReadAsStringAsync());
                    return o["result"] != null;
                }
                catch (Exception)
                {
                    return false;
                }
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
        /// <summary>
        /// The whole album list, fetched once and then kept. This is read from XAML bindings,
        /// which evaluate it repeatedly while a page renders, so querying per read meant a
        /// blocking round trip to the server each time. A failed query caches nothing, so the
        /// next read tries again; <see cref="InvalidateLibrary"/> drops what we have.
        /// </summary>
        public static AlbumList GetAllAlbums()
        {
            if (AllAlbums == null)
            {
                var msg = FionaMessage.CreateMessage(FionaCommand.Albums, "0", FionaCommand.MaxItems, FionaCommand.AlbumTags);
                AllAlbums = QueryWebServiceWithPost<AlbumList>(RemoteUrlJson, msg);
            }
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
        /// <summary>
        /// The whole artist list. Cached like <see cref="GetAllAlbums"/>, and for the same reason.
        /// </summary>
        public static ArtistList GetAllArtists()
        {
            if (AllArtists == null)
            {
                var msg = FionaMessage.CreateMessage(FionaCommand.Artists, "0", FionaCommand.MaxItems, FionaCommand.ArtistTags);
                AllArtists = QueryWebServiceWithPost<ArtistList>(RemoteUrlJson, msg);
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

        #region Favorites
        /// <summary>
        /// Deliberately not cached, unlike the album and artist lists: favorites are added and
        /// removed from inside the app, so a stale copy would be visibly wrong. The list is
        /// small enough for that to cost little.
        /// </summary>
        public static FavoriteList GetAllFavorites()
        {
            var msg = FionaMessage.CreateMessage(FionaCommand.Favorites, "items", "0", FionaCommand.MaxItems);
            AllFavorites = QueryWebServiceWithPost<FavoriteList>(RemoteUrlJson, msg);
            return AllFavorites;
        }

        public static void PlaylistLoadAndPlayFavorite(Player player, Favorite favorite)
        {
            TransportUnsetShuffle(player);
            var msg = FionaMessage.CreateMessage(player, "favorites", "playlist", "play", "item_id:" + favorite.ID.ToString());
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void PlaylistAppendFavorite(Player player, Favorite favorite)
        {
            var msg = FionaMessage.CreateMessage(player, "favorites", "playlist", "add", "item_id:" + favorite.ID.ToString());
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void AddFavorite(Player player, string title, string url, string icon)
        {
            var msg = FionaMessage.CreateMessage(player, "favorites", "add", "title:" + title, "url:" + url, "icon:" + icon);
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void UnFavorite(Player player, Favorite favorite)
        {
            var id = favorite.ID.Substring(favorite.ID.IndexOf('.') + 1);
            var msg = FionaMessage.CreateMessage(player, "favorites", "delete", "item_id:" + id.ToString());
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
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
            TransportUnsetShuffle(player);
            var msg = FionaMessage.CreateMessage(player, "playlistcontrol", "cmd:load", "album_id:" + album.ID.ToString());
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void PlaylistAppendAlbum(Player player, Album album)
        {
            var msg = FionaMessage.CreateMessage(player, "playlistcontrol", "cmd:add", "album_id:" + album.ID.ToString());
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void PlaylistLoadAndPlayArtist(Player player, Artist artist)
        {
            TransportUnsetShuffle(player);
            var msg = FionaMessage.CreateMessage(player, "playlistcontrol", "cmd:load", "artist_id:" + artist.ID.ToString());
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void PlaylistLoadAndShuffleArtist(Player player, Artist artist)
        {
            TransportSetShuffle(player);
            var msg = FionaMessage.CreateMessage(player, "playlistcontrol", "cmd:load", "artist_id:" + artist.ID.ToString());
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void PlaylistAppendArtist(Player player, Artist artist)
        {
            TransportUnsetShuffle(player);
            var msg = FionaMessage.CreateMessage(player, "playlistcontrol", "cmd:add", "artist_id:" + artist.ID.ToString());
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
            TransportToggleShuffle(player);
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

        public static void TransportToggleShuffle(Player player)
        {
            var msg = FionaMessage.CreateMessage(player, "playlist", "shuffle");
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void TransportSetShuffle(Player player)
        {
            var msg = FionaMessage.CreateMessage(player, "playlist", "shuffle", "1");
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void TransportUnsetShuffle(Player player)
        {
            var msg = FionaMessage.CreateMessage(player, "playlist", "shuffle", "0");
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void TransportToggleRepeat(Player player)
        {
            var msg = FionaMessage.CreateMessage(player, "playlist", "repeat");
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void TransportSetRepeat(Player player)
        {
            var msg = FionaMessage.CreateMessage(player, "playlist", "repeat", "1");
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }

        public static void TransportUnsetRepeat(Player player)
        {
            var msg = FionaMessage.CreateMessage(player, "playlist", "repeat", "2");
            var r = QueryWebServiceWithPost<PlayerStatus>(RemoteUrlJson, msg);
        }
        #endregion

        #region Apps
        public static AppletList GetAllApps(Player player)
        {
            if (AllApps != null)
                return AllApps;
            else
            {
                var msg = FionaMessage.CreateMessage(player, "myapps", "items", "0", FionaCommand.MaxItems, "menu:1", "want_url:1");
                var res = QueryWebServiceWithPost<AppletList>(RemoteUrlJson, msg);
                AllApps = res;
                return res;
            }
        }

        public static AppletList GetAppTopLevel(Player player, string cmd1)
        {
            var msg = FionaMessage.CreateMessage(player, cmd1, "items", "0", FionaCommand.MaxItems, "menu:1", "want_url:1");
            var res = QueryWebServiceWithPost<AppletList>(RemoteUrlJson, msg);
            return res;
        }

        public static AppletList GetApps(Player player, string cmd1, string cmd2, string menu, string item_id)
        {
            var msg = FionaMessage.CreateMessage(player, cmd1, cmd2, "0", FionaCommand.MaxItems, "menu:" + menu, "item_id:" + item_id, "want_url:1");
            var res = QueryWebServiceWithPost<AppletList>(RemoteUrlJson, msg);
            return res;
        }

        public static void PlayPlaylistFromApp(Player player, string appname, string menu, string item_id)
        {
            var msg = FionaMessage.CreateMessage(player, appname, "playlist", "play", "_index:0", "_quantity:" + FionaCommand.MaxItems.ToString(), "menu:" + menu, "item_id:" + item_id, "want_url:1");
            var res = QueryWebServiceWithPost<AppletList>(RemoteUrlJson, msg);
        }

        public static void QueuePlaylistFromApp(Player player, string appname, string menu, string item_id)
        {
            var msg = FionaMessage.CreateMessage(player, appname, "playlist", "add", "_index:0", "_quantity:" + FionaCommand.MaxItems.ToString(), "menu:" + menu, "item_id:" + item_id, "want_url:1");
            var res = QueryWebServiceWithPost<AppletList>(RemoteUrlJson, msg);
        }

        public static AppletList SearchInApp(Player player, string appname, string menu, string item_id, string query_term)
        {
            var msg = FionaMessage.CreateMessage(player, appname, "items", "0", FionaCommand.MaxItems, "menu:" + menu, "item_id:" + item_id, "cachesearch:1", "search:" + query_term);
            var res = QueryWebServiceWithPost<AppletList>(RemoteUrlJson, msg);
            return res;
        }
        #endregion

        #region Radios
        public static AppletList GetAllRadios(Player player)
        {
            if (AllRadios != null)
                return AllRadios;
            else
            {
                var msg = FionaMessage.CreateMessage(player, "radios", "0", FionaCommand.MaxItems, "menu:1", "want_url:1");
                var res = QueryWebServiceWithPost<AppletList>(RemoteUrlJson, msg);
                AllRadios = res;
                return res;
            }
        }

        public static AppletList GetRadioTopLevel(Player player, string cmd1)
        {
            var msg = FionaMessage.CreateMessage(player, cmd1, "items", "0", FionaCommand.MaxItems, "menu:1", "want_url:1");
            var res = QueryWebServiceWithPost<AppletList>(RemoteUrlJson, msg);
            return res;
        }

        public static AppletList GetRadios(Player player, string cmd1, string cmd2, string menu, string item_id)
        {
            var msg = FionaMessage.CreateMessage(player, cmd1, cmd2, "0", FionaCommand.MaxItems, "menu:" + menu, "item_id:" + item_id, "want_url:1");
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

        public static string DefaultAlbumImageUrl
        {
            get => $"{RemoteUrl}music/0/cover_{BigImageSize}x{BigImageSize}.jpg";
        }

        public static string DefaultArtistImageUrl
        {
            get => $"{RemoteUrl}music/0/cover_{BigImageSize}x{BigImageSize}.jpg";
        }

        public static string DefaultAppImageUrl
        {
            //            get => $"{RemoteUrl}music/0/cover_{BigImageSize}x{BigImageSize}.jpg";
            get => $"/Assets/playlist.png";
        }

        // A LAN server that has gone away should fail in seconds, not in the 100 the
        // HttpClient default would have us wait.
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

        private static HttpClient client = new HttpClient { Timeout = RequestTimeout };

        private static T QueryWebServiceWithPost<T>(string url, string msg)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(ServerIP))
            {
                return default(T);
            }
            else
            {
                //TODO authentication
                string body;

                try
                {
                    var content = new StringContent(msg, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = client.PostAsync(url, content).Result;

                    if (!response.IsSuccessStatusCode)
                    {
                        ReportFailure("the server answered " + ((int)response.StatusCode).ToString()
                            + " " + response.ReasonPhrase);
                        return default(T);
                    }

                    using (HttpContent c = response.Content)
                    {
                        body = c.ReadAsStringAsync().Result;
                    }
                }
                catch (Exception ex)
                {
                    // Unreachable, refused, timed out, name no longer resolving: all the same to a
                    // caller, which can only carry on with nothing. This must not be allowed out.
                    // Callers include XAML bindings and a timer that ticks every second, so letting
                    // an exception through here does not fail a request, it closes the app.
                    ReportFailure(Innermost(ex).Message);
                    return default(T);
                }

                T value;
                string error;
                if (!TryReadResult(body, out value, out error))
                {
                    ReportFailure(error);
                    return default(T);
                }

                IsServerReachable = true;
                return value;
            }
        }

        /// <summary>
        /// Takes the result out of a JSON-RPC answer, or says why it could not. Kept apart from the
        /// request so it can be tested: every step here was once unguarded, and a server answering
        /// with an error, with an HTML error page, or with nothing at all closed the app rather
        /// than failing one request.
        /// </summary>
        internal static bool TryReadResult<T>(string body, out T value, out string error)
        {
            value = default(T);
            error = null;

            if (string.IsNullOrWhiteSpace(body))
            {
                error = "the server sent an empty response";
                return false;
            }

            JObject o;
            try
            {
                o = JObject.Parse(body);
            }
            catch (JsonException)
            {
                // Typically a proxy or web server error page, arriving where JSON was expected.
                error = "the server sent something that is not JSON";
                return false;
            }

            JToken failure = o["error"];
            if (failure != null && failure.Type != JTokenType.Null)
            {
                error = "the server reported an error: " + failure.ToString();
                return false;
            }

            JToken result = o["result"];
            if (result == null || result.Type == JTokenType.Null)
            {
                error = "the server sent no result";
                return false;
            }

            //HACK convert loop_loop into item_loop
            string json = result.ToString().Replace("loop_loop", "item_loop");

            try
            {
                value = JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonException ex)
            {
                error = "the server result did not fit a " + typeof(T).Name + ": " + ex.Message;
                return false;
            }

            return true;
        }

        /// <summary>
        /// A blocking wait wraps whatever went wrong in an AggregateException, sometimes twice over,
        /// and "One or more errors occurred" tells a person nothing at all.
        /// </summary>
        private static Exception Innermost(Exception ex)
        {
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            return ex;
        }

        private static void ReportFailure(string reason)
        {
            IsServerReachable = false;

            EventHandler<string> handler = RequestFailed;
            if (handler != null) handler(null, reason);
        }

        #endregion
    }
}
