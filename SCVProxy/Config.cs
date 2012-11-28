using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace SCVProxy
{
    public static class Config
    {
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

        public static List<string> WebMinerUrlList
        {
            get
            {
                return (ConfigurationManager.AppSettings["WebMinerUrl"] ?? String.Empty)
                    .Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .Distinct()
                    .ToList();
            }
        }
    }
}
