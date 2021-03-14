using Fiona.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fiona.Core.Services
{
    public static class DiscogsDataService
    {
        public static DiscogsArtist GetArtistInfo(string name)
        {
            DiscogsSearchResult res = SearchDiscogs<DiscogsSearchResult>(name);
            DiscogsArtist artist = QueryDiscogsEntity<DiscogsArtist>("artists", res.ID.ToString());
            return artist;
        }

        #region Plumbing
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

        private static T SearchDiscogs<T>(string param)
        {
            string url = $"{QueryUrl(param)}&key={Fiona.Core.Helpers.APIKeys.DiscogsConsumerKey}&secret={Fiona.Core.Helpers.APIKeys.DiscogsConsumerSecret}";
            client.DefaultRequestHeaders.Add("User-Agent", "Fiona/0.1.0 (paolo@paolomarcucci.com)");

            var response = client.GetAsync(url);
            string res = "";

            using (HttpContent c = response.Result.Content)
            {
                Task<string> result = c.ReadAsStringAsync();
                res = result.Result;
            }

            JObject o = JObject.Parse(res);
            var jsonResult = o["results"];
            var outval = JsonConvert.DeserializeObject<List<T>>(jsonResult.ToString());
            return outval[0];
        }

        private static T QueryDiscogsEntity<T>(string entitytype, string id)
        {
            string url = $"{EntityUrl(entitytype, id)}?key={Fiona.Core.Helpers.APIKeys.DiscogsConsumerKey}&secret={Fiona.Core.Helpers.APIKeys.DiscogsConsumerSecret}";
            client.DefaultRequestHeaders.Add("User-Agent", "Fiona/0.1.0 (paolo@paolomarcucci.com)");

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
