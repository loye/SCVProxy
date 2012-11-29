using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace SCVProxy
{
    internal static class Config
    {
        private static readonly Regex URL_REGEX = new Regex(@"(?:http(?<ssl>s)://)?(?<host>[^/: ]+)(?:\:(?<port>\d+))?\S*", RegexOptions.Compiled);

        public static string MakeCertFileName
        {
            get
            {
                return String.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["MakeCertFileName"])
                    ? "makecert.exe"
                    : ConfigurationManager.AppSettings["MakeCertFileName"];
            }
        }

        public static string ListenAddress
        {
            get
            {
                return (ConfigurationManager.AppSettings["ListenAddress"] ?? "127.0.0.1:1000").Split(':')[0];
            }
        }

        public static int ListenPort
        {
            get
            {
                return int.Parse((ConfigurationManager.AppSettings["ListenAddress"] ?? "127.0.0.1:1000").Split(':')[1]);
            }
        }

        public static string ProxyAddress
        {
            get
            {
                if (String.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["ProxyAddress"]))
                {
                    return null;
                }
                return (ConfigurationManager.AppSettings["ProxyAddress"]).Split(':')[0];
            }
        }

        public static int ProxyPort
        {
            get
            {
                if (String.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["ProxyAddress"]))
                {
                    return 0;
                }
                return int.Parse(ConfigurationManager.AppSettings["ProxyAddress"].Split(':')[1]);
            }
        }

        public static string MinerType
        {
            get
            {
                return ConfigurationManager.AppSettings["MinerType"];
            }
        }

        public static List<MinerEndPoint> HttpMinerEndPointList
        {
            get
            {
                return (ConfigurationManager.AppSettings["HttpMiner.Url"] ?? String.Empty)
                    .Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .Distinct()
                    .Select(u =>
                        {
                            Match match = URL_REGEX.Match(u);
                            string host = match.Groups["host"].Value;
                            bool isSSL = match.Groups["ssl"].Success;
                            IPAddress ipAddress = Dns.GetHostAddresses(host).Where(a => a.AddressFamily == AddressFamily.InterNetwork).First();
                            int port = match.Groups["port"].Success ? int.Parse(match.Groups["port"].Value) : (isSSL ? 443 : 80);
                            return !match.Success ? null : new MinerEndPoint()
                            {
                                Url = u,
                                Host = host,
                                IsSSL = isSSL,
                                EndPoint = new IPEndPoint(ipAddress, port)
                            };
                        })
                    .Where(e => e != null)
                    .ToList();
            }
        }

        public static bool Encrypt
        {
            get
            {
                bool isEncrypted;
                return bool.TryParse(ConfigurationManager.AppSettings["HttpMiner.Encrypt"], out isEncrypted) && isEncrypted;
            }
        }
    }

    internal class MinerEndPoint
    {
        public string Url { get; set; }

        public string Host { get; set; }

        public bool IsSSL { get; set; }

        public IPEndPoint EndPoint { get; set; }
    }
}
