using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace SCVProxy
{
    public static class DnsHelper
    {
        private static readonly Regex HOSTS_REGEX = new Regex(@"(?<ip>\d+\.\d+\.\d+\.\d+)[ \t]+(?<host>[^ \t\r\n]+)", RegexOptions.Compiled);

        private static Dictionary<string, IPAddress> hosts = null;

        static DnsHelper()
        {
            hosts = new Dictionary<string, IPAddress>();
            for (Match match = HOSTS_REGEX.Match(Config.Hosts); match.Success; match = match.NextMatch())
            {
                IPAddress ip;
                if (IPAddress.TryParse(match.Groups["ip"].Value, out ip))
                {
                    hosts[match.Groups["host"].Value] = ip;
                }
            }
        }

        public static IPAddress GetHostAddress(string host)
        {
            IPAddress address;
            return TryGetHostAddress(host, out address) ? address : null;
        }

        public static bool TryGetHostAddress(string host, out IPAddress address)
        {
            address = null;

            if (hosts != null && hosts.ContainsKey(host))
            {
                address = hosts[host];
                return true;
            }

            try
            {
                address = Dns.GetHostAddresses(host).Where(a => a.AddressFamily == AddressFamily.InterNetwork).First();
                return true;
            }
            catch { }

            return false;
        }
    }
}
