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
        private static readonly Regex URL_REGEX = new Regex(@"(?:(?<schema>\w+)\://)?(?<host>[^/: ]+)(?:\:(?<port>\d+))?\S*", RegexOptions.Compiled);

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
                            if (!match.Success)
                            {
                                return null;
                            }
                            string host = match.Groups["host"].Value;
                            IPAddress ipAddress;
                            if (!DnsHelper.TryGetHostAddress(host, out ipAddress))
                            {
                                return null;
                            }
                            bool isSSL = match.Groups["schema"].Success && match.Groups["schema"].Value.ToLower() == "https";
                            int port = match.Groups["port"].Success ? int.Parse(match.Groups["port"].Value) : (isSSL ? 443 : 80);
                            return new MinerEndPoint()
                            {
                                Url = u,
                                Host = host,
                                Port = port,
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

        public static string Hosts
        {
            get
            {
                return ConfigurationManager.AppSettings["Hosts"];
            }
        }
    }

    internal class MinerEndPoint
    {
        public string Url { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public bool IsSSL { get; set; }

        public IPEndPoint EndPoint { get; set; }
    }
}
