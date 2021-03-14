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
