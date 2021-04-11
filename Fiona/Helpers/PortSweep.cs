using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fiona.Helpers
{
    public class PortSweep
    {
        private string BaseIP = "192.168.1.";
        private int StartIP = 1;
        private int StopIP = 255;
        private string ip;

        private int timeout = 50;
        private int slimserverport = 3483;

        private string slimServer = "";

        public string GetServer()
        {
            return slimServer;
        }

        public async Task RunPortSweep_Async()
        {
            var tasks = new List<Task>();

            // find the LAN 
            string localip = GetLocalIPAddress();
            BaseIP = localip.Substring(0, localip.LastIndexOf('.') + 1);

            for (int i = StartIP; i <= StopIP; i++)
            {
                ip = BaseIP + i.ToString();
                var task = CheckPort_Async(ip, slimserverport, timeout);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        private async Task CheckPort_Async(string ip, int port, int timeout)
        {
            bool b = IsPortOpen(ip, port, TimeSpan.FromMilliseconds(timeout));
            if (b) Interlocked.Exchange<string>(ref slimServer, ip);
        }

        private string GetLocalIPAddress()
        {
            string localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
            return localIP;
        }

        private bool IsPortOpen(string host, int port, TimeSpan timeout)
        {
            TcpClient client = null;
            bool result = false;

            try
            {
                client = new TcpClient();
                Task task = client.ConnectAsync(host, port);
                if (task.Wait(timeout))
                {//if fails within timeout, task.Wait still returns true.
                    if (client.Connected)
                    {
                        // port reachable
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // connection failed
                result = false;
            }
            finally
            {
                client.Close();
            }

            return result;
        }

    }

}
