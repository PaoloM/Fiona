using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;

namespace Fiona.Helpers
{
    /// <summary>
    /// Looks for a Logitech Media Server by probing the slimproto port on every address of
    /// every local subnet. All the probes really do run at once, so a sweep takes about as
    /// long as one timeout rather than one timeout per address.
    /// </summary>
    public class PortSweep
    {
        private const int StartIP = 1;
        private const int StopIP = 254;
        private const int SlimServerPort = 3483;

        // Generous enough for a busy wireless LAN: nothing waits on it serially any more.
        private const int ProbeTimeoutMs = 500;

        // Sockets in flight at once. High enough to sweep a /24 in two or three rounds,
        // low enough not to exhaust the connection table on a machine with several adapters.
        private const int MaxConcurrentProbes = 64;

        private string slimServer = "";

        public string GetServer()
        {
            return slimServer;
        }

        public async Task RunPortSweep_Async()
        {
            await RunPortSweep_Async(CancellationToken.None);
        }

        public async Task RunPortSweep_Async(CancellationToken cancellationToken)
        {
            slimServer = "";

            var candidates = new List<string>();
            foreach (string prefix in GetLocalSubnets())
            {
                for (int i = StartIP; i <= StopIP; i++)
                {
                    candidates.Add(prefix + i.ToString());
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            // Cancelled as soon as one address answers, so the rest of the sweep is dropped.
            using (var found = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var throttle = new SemaphoreSlim(MaxConcurrentProbes))
            {
                await Task.WhenAll(candidates.Select(ip => ProbeAsync(ip, found, throttle)));
            }
        }

        private async Task ProbeAsync(string ip, CancellationTokenSource found, SemaphoreSlim throttle)
        {
            if (found.IsCancellationRequested)
            {
                return; // we already have a server, this address never needs probing
            }

            // Waited on without the token deliberately. Handing it the token instead makes
            // ending the sweep throw once per queued probe - around 190 exceptions for a
            // single /24 - which is control flow by exception: slow, and it breaks into the
            // debugger on every successful run.
            await throttle.WaitAsync();

            try
            {
                if (found.IsCancellationRequested)
                {
                    return; // another address answered while this probe sat in the queue
                }

                if (await IsPortOpenAsync(ip, SlimServerPort, ProbeTimeoutMs))
                {
                    // First answer wins; later ones leave the field alone.
                    if (Interlocked.CompareExchange(ref slimServer, ip, "") == "")
                    {
                        found.Cancel();
                    }
                }
            }
            finally
            {
                throttle.Release();
            }
        }

        private static async Task<bool> IsPortOpenAsync(string host, int port, int timeoutMs)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    Task connect = client.ConnectAsync(host, port);

                    // Abandoning a connect below would otherwise surface as an unobserved
                    // task exception once the socket is torn down. Deliberately not awaited:
                    // it outlives the probe by design.
                    Task observed = connect.ContinueWith(t => { var ignored = t.Exception; },
                        TaskContinuationOptions.OnlyOnFaulted);

                    if (await Task.WhenAny(connect, Task.Delay(timeoutMs)) != connect)
                    {
                        return false; // nothing at this address answered in time
                    }

                    await connect; // a refused connection reaches us as an exception
                    return client.Connected;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// The /24 around each of this machine's IPv4 addresses. Every adapter is swept
        /// rather than guessing one, so a virtual adapter (WSL, Hyper-V) sitting on the
        /// default route can no longer send the whole sweep to the wrong subnet.
        /// </summary>
        private static IEnumerable<string> GetLocalSubnets()
        {
            var prefixes = new List<string>();

            foreach (HostName hostName in NetworkInformation.GetHostNames())
            {
                if (hostName.Type != HostNameType.Ipv4 || hostName.IPInformation == null)
                {
                    continue; // not an IPv4 address belonging to a local adapter
                }

                string address = hostName.CanonicalName;
                if (address.StartsWith("127.") || address.StartsWith("169.254."))
                {
                    continue; // loopback and link-local addresses never reach a server
                }

                string prefix = address.Substring(0, address.LastIndexOf('.') + 1);
                if (!prefixes.Contains(prefix))
                {
                    prefixes.Add(prefix);
                }
            }

            return prefixes;
        }
    }
}
