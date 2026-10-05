using Fiona.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Fiona.Core.Services
{
    public static class DiscogsDataService
    {
        public static DiscogsArtist GetArtistInfo(string name)
        {
            if (HaveKeys())
            {
                IEnumerable<DiscogsSearchResult> res = SearchDiscogs<DiscogsSearchResult>(name);

                IEnumerable<DiscogsSearchResult> a = (from aa in res where aa.EntityType == "artist" select aa);

                if (a.Count<DiscogsSearchResult>() == 0)
                    return null;

                DiscogsArtist artist = QueryDiscogsEntity<DiscogsArtist>("artists", 
                    a.First<DiscogsSearchResult>().ID.ToString());

                return artist;
            }
            else
                return null;
        }

        #region Plumbing
        private static bool HaveKeys()
        {
            return !string.IsNullOrEmpty(Fiona.Core.Helpers.APIKeys.DiscogsConsumerKey);
        }

        public static string RemoteUrl = "https://api.discogs.com/";

        public static string QueryUrl(string param)
        {
                return $"{RemoteUrl}database/search?q={param}";
        }

        public static string EntityUrl(string entity, string id)
        {
                return $"{RemoteUrl}{entity}/{id}";
        }

        // Reached synchronously from view models, so an unresponsive Discogs would otherwise hold
        // the UI for the HttpClient default of 100 seconds.
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

        private static HttpClient client = CreateClient();

        /// <summary>
        /// The user agent is set once here rather than before each request. Adding it per call
        /// appended another copy every time, so the header grew for the life of the process:
        /// DefaultRequestHeaders.Add on a list-valued header adds to the list.
        /// </summary>
        private static HttpClient CreateClient()
        {
            var created = new HttpClient { Timeout = RequestTimeout };
            created.DefaultRequestHeaders.Add("User-Agent", Fiona.Core.Helpers.APIKeys.UserAgent);
            return created;
        }

        /// <summary>
        /// Artist artwork and biographies are a nicety. Discogs being slow, rate-limiting us or
        /// answering with something unexpected should cost the extra information and nothing more.
        /// </summary>
        private static string Fetch(string url)
        {
            try
            {
                HttpResponseMessage response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode) return null;

                using (HttpContent c = response.Content)
                {
                    return c.ReadAsStringAsync().Result;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static IEnumerable<T> SearchDiscogs<T>(string param)
        {
            string url = $"{QueryUrl(param)}&key={Fiona.Core.Helpers.APIKeys.DiscogsConsumerKey}&secret={Fiona.Core.Helpers.APIKeys.DiscogsConsumerSecret}";
            string res = Fetch(url);
            if (res == null) return new List<T>();

            try
            {
                JObject o = JObject.Parse(res);
                JToken results = o["results"];
                if (results == null || results.Type == JTokenType.Null) return new List<T>();

                return JsonConvert.DeserializeObject<IEnumerable<T>>(results.ToString());
            }
            catch (JsonException)
            {
                return new List<T>();
            }
        }

        private static T QueryDiscogsEntity<T>(string entitytype, string id)
        {
            string url = $"{EntityUrl(entitytype, id)}?key={Fiona.Core.Helpers.APIKeys.DiscogsConsumerKey}&secret={Fiona.Core.Helpers.APIKeys.DiscogsConsumerSecret}";
            string res = Fetch(url);
            if (res == null) return default(T);

            try
            {
                return JsonConvert.DeserializeObject<T>(JObject.Parse(res).ToString());
            }
            catch (JsonException)
            {
                return default(T);
            }
        }
        #endregion

    }
}
