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

        private static HttpClient client = new HttpClient();

        private static IEnumerable<T> SearchDiscogs<T>(string param)
        {
            string url = $"{QueryUrl(param)}&key={Fiona.Core.Helpers.APIKeys.DiscogsConsumerKey}&secret={Fiona.Core.Helpers.APIKeys.DiscogsConsumerSecret}";
            client.DefaultRequestHeaders.Add("User-Agent", Fiona.Core.Helpers.APIKeys.UserAgent);

            var response = client.GetAsync(url);
            string res = "";

            using (HttpContent c = response.Result.Content)
            {
                Task<string> result = c.ReadAsStringAsync();
                res = result.Result;
            }

            JObject o = JObject.Parse(res);
            var jsonResult = o["results"];
            var outval = JsonConvert.DeserializeObject<IEnumerable<T>>(jsonResult.ToString());
            return outval;
        }

        private static T QueryDiscogsEntity<T>(string entitytype, string id)
        {
            string url = $"{EntityUrl(entitytype, id)}?key={Fiona.Core.Helpers.APIKeys.DiscogsConsumerKey}&secret={Fiona.Core.Helpers.APIKeys.DiscogsConsumerSecret}";
            client.DefaultRequestHeaders.Add("User-Agent", Fiona.Core.Helpers.APIKeys.UserAgent);

            var response = client.GetAsync(url);
            string res = "";

            using (HttpContent c = response.Result.Content)
            {
                Task<string> result = c.ReadAsStringAsync();
                res = result.Result;
            }

            JObject o = JObject.Parse(res);
            var jsonResult = o; //["results"];
            var outval = JsonConvert.DeserializeObject<T>(jsonResult.ToString());
            return outval;
        }
        #endregion

    }
}
