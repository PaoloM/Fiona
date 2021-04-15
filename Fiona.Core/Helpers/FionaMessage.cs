using Fiona.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiona.Core.Helpers
{
    public sealed class FionaMessage
    {
        #region Private Members

        /// <summary>
        /// Make sure the class cannot be instantiated by others
        /// </summary>
        private FionaMessage()
        {
            // For some reason the Fiona server only seems to accept "slim.request"
            this.Method = "slim.request";
            this.ID = 1;
        }

        /// <summary>
        /// Again, make sure the class cannot be instantiated by others
        /// </summary>
        /// <param name="Params">Command list. First object should be the mac address of the Player (if used) second object</param>
        private FionaMessage(object[] Params)
            : this()
        {
            this.Params = Params;
        }

        [JsonProperty(PropertyName = "id")]
        private int ID { get; set; }

        [JsonProperty(PropertyName = "method")]
        private string Method { get; set; }

        [JsonProperty(PropertyName = "params")]
        private object[] Params { get; set; }

        #endregion

        #region Public Members

        public static string CreateMessage(string command)
        {
            return JsonConvert.SerializeObject(new FionaMessage(new object[2] { null, new List<string>(1) { command } }));
        }

        public static string CreateMessage(string command, string arg0)
        {
            return JsonConvert.SerializeObject(new FionaMessage(new object[2] { null, new List<string>(2) { command, arg0 } }));
        }

        public static string CreateMessage(string command, string arg0, string arg1)
        {
            return JsonConvert.SerializeObject(new FionaMessage(new object[2] { null, new List<string>(3) { command, arg0, arg1 } }));
        }

        public static string CreateMessage(string command, string arg0, string arg1, string arg2)
        {
            return JsonConvert.SerializeObject(new FionaMessage(new object[2] { null, new List<string>(4) { command, arg0, arg1, arg2 } }));
        }

        public static string CreateMessage(string command, string arg0, string arg1, string arg2, string arg3)
        {
            return JsonConvert.SerializeObject(new FionaMessage(new object[2] { null, new List<string>(4) { command, arg0, arg1, arg2, arg3 } }));
        }

        public static string CreateMessage(Player player, string command)
        {
            return JsonConvert.SerializeObject(new FionaMessage(new object[2] { player.ID, new List<string>(1) { command } }));
        }

        public static string CreateMessage(Player player, string command, string arg0)
        {
            return JsonConvert.SerializeObject(new FionaMessage(new object[2] { player.ID, new List<string>(2) { command, arg0 } }));
        }

        public static string CreateMessage(Player player, string command, string arg0, string arg1)
        {
            return JsonConvert.SerializeObject(new FionaMessage(new object[2] { player.ID, new List<string>(3) { command, arg0, arg1 } }));
        }

        public static string CreateMessage(Player player, string command, string arg0, string arg1, string arg2)
        {
            return JsonConvert.SerializeObject(new FionaMessage(new object[2] { player.ID, new List<string>(4) { command, arg0, arg1, arg2 } }));
        }

        public static string CreateMessage(Player player, string command, string arg0, string arg1, string arg2, string arg3)
        {
            return JsonConvert.SerializeObject(new FionaMessage(new object[2] { player.ID, new List<string>(5) { command, arg0, arg1, arg2, arg3 } }));
        }

        public static string CreateMessage(Player player, string command, string arg0, string arg1, string arg2, string arg3, string arg4)
        {
            return JsonConvert.SerializeObject(new FionaMessage(new object[2] { player.ID, new List<string>(6) { command, arg0, arg1, arg2, arg3, arg4 } }));
        }

        public static string CreateMessage(Player player, string command, string arg0, string arg1, string arg2, string arg3, string arg4, string arg5)
        {
            return JsonConvert.SerializeObject(new FionaMessage(new object[2] { player.ID, new List<string>(7) { command, arg0, arg1, arg2, arg3, arg4, arg5 } }));
        }

        public static string CreateMessage(Player player, string command, string arg0, string arg1, string arg2, string arg3, string arg4, string arg5, string arg6)
        {
            return JsonConvert.SerializeObject(new FionaMessage(new object[2] { player.ID, new List<string>(8) { command, arg0, arg1, arg2, arg3, arg4, arg5, arg6 } }));
        }

        public static string CreateMessage(Player player, string command, string arg0, string arg1, string arg2, string arg3, string arg4, string arg5, string arg6, string arg7)
        {
            return JsonConvert.SerializeObject(new FionaMessage(new object[2] { player.ID, new List<string>(9) { command, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 } }));
        }

        #endregion
    }
}
