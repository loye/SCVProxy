using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SCVProxy
{
    public class HttpPackage
    {
        private const int BUFFER_LENGTH = 4096;

        private static readonly Regex HEADER_REGEX = new Regex(
            @"(?:^(?<request>(?<method>GET|HEAD|POST|PUT|DELETE|TRACE|CONNECT)\s(?<url>(?:\w+://)?(?<host>[^/: ]+)(?:\:(?<port>\d+))?\S*)\s(?<version>.*)\r\n)|^(?<response>(?<version>HTTP\S+)\s(?<status>(?<code>\d+).*)\r\n))(?:(?<key>[\w\-]+):\s?(?<value>.*)\r\n)*\r\n",
            RegexOptions.Compiled);

        private int _chunkedNextBlockOffset;

        public string HttpMethod { get; private set; }

        public string Url { get; private set; }

        public string Host { get; private set; }

        public int Port { get; private set; }

        public string Version { get; private set; }

        public string Status { get; private set; }

        public int StatusCode { get; private set; }

        public string StartLine { get; private set; }

        public Dictionary<string, string> HeaderItems { get; private set; }

        public string Header { get; private set; }

        public byte[] Binary { get; private set; }

        public int Length { get; private set; }

        public int ContentOffset { get; private set; }

        public int ContentLength { get; private set; }

        private HttpPackage(Match match)
        {
            if (match.Groups["request"].Success)
            {
                this.HttpMethod = match.Groups["method"].Value;
                this.Url = match.Groups["url"].Value;
                this.Host = match.Groups["host"].Value;
                this.Port = match.Groups["port"].Success ? int.Parse(match.Groups["port"].Value) : 80;
                this.Version = match.Groups["version"].Value;
                this.StartLine = match.Groups["request"].Value;
            }
            if (match.Groups["response"].Success)
            {
                this.Version = match.Groups["version"].Value;
                this.Status = match.Groups["status"].Value;
                this.StatusCode = int.Parse(match.Groups["code"].Value);
                this.StartLine = match.Groups["response"].Value;
            }
            Group keyGroup = match.Groups["key"];
            Group valueGroup = match.Groups["value"];
            Dictionary<string, string> headerItems = new Dictionary<string, string>();
            for (int i = 0, headerCount = keyGroup.Captures.Count; i < headerCount; i++)
            {
                headerItems[keyGroup.Captures[i].Value] = valueGroup.Captures[i].Value;
            }
            this.HeaderItems = headerItems;
            this.Header = match.Captures[0].Value;
            this.ContentOffset = this.Header.Length;
            this.ContentLength = headerItems.ContainsKey("Content-Length")
                ? int.Parse(headerItems["Content-Length"])
                : (headerItems.ContainsKey("Transfer-Encoding") && headerItems["Transfer-Encoding"] == "chunked" ? -1 : 0);
            this._chunkedNextBlockOffset = this.ContentOffset;
        }

        public static HttpPackage Read(Stream stream)
        {
            HttpPackage package = null;
            byte[] buffer = new byte[BUFFER_LENGTH];
            using (MemoryStream mem = new MemoryStream())
            {
                for (int len = stream.Read(buffer, 0, buffer.Length);
                    len > 0;
                    len = stream.Read(buffer, 0, buffer.Length))
                {
                    mem.Write(buffer, 0, len);
                    byte[] bin = mem.GetBuffer();
                    if (ValidatePackage(bin, (int)mem.Length, ref package))
                    {
                        if (package.ContentLength == 0
                            && package.HeaderItems.ContainsKey("Connection")
                            && package.HeaderItems["Connection"] == "close") // Connection: close
                        {
                            continue;
                        }
                        break;
                    }
                }
            }
            return package;
        }

        public static HttpPackage Read(byte[] bin)
        {
            HttpPackage package = null;
            ValidatePackage(bin, bin.Length, ref package);
            return package;
        }

        private static bool ValidatePackage(byte[] bin, int length, ref HttpPackage package)
        {
            bool isValid = false;
            if (package == null)
            {
                string str = ASCIIEncoding.ASCII.GetString(bin, 0, length);
                Match match = HEADER_REGEX.Match(str);
                if (match.Success)
                {
                    package = new HttpPackage(match);
                }
                else
                {
                    Console.WriteLine("NOT MATCH:########################################\r\n" + str);
                }
            }
            if (package != null)
            {
                package.Binary = bin;
                package.Length = length;
                if (package.ContentLength == 0)
                {
                    isValid = true;
                }
                else if (package.ContentLength > 0)
                {
                    isValid = package.Length >= package.ContentOffset + package.ContentLength;
                }
                else // Transfer-Encoding: chunked
                {
                    Console.WriteLine("Transfer-Encoding: chunked");
                    isValid = ValidateChunkedBlock(package);
                    if (isValid)
                    {
                        package.ContentLength = package.Length - package.ContentOffset;
                    }
                }
            }
            return isValid;
        }

        private static bool ValidateChunkedBlock(HttpPackage package)
        {
            byte[] bin = package.Binary;
            int length = package.Length, startIndex = package._chunkedNextBlockOffset;
            if (startIndex > length - 5)
            {
                return false;
            }
            int contentLength = 0, i = startIndex;
            for (int temp = bin[i]; temp != 0x0D && i < length; temp = bin[++i])
            {
                if (i >= length - 1)
                {
                    return false;
                }
                contentLength = contentLength * 16 + (temp > 0x40 ? (temp > 0x60 ? temp - 0x60 : temp - 0x40) + 9 : temp - 0x30);
            }
            package._chunkedNextBlockOffset = i + 4 + contentLength;
            return contentLength == 0 || ValidateChunkedBlock(package);
        }
    }
}
