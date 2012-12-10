using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SCVProxy
{
    public class HttpPackage
    {
        private const int BUFFER_LENGTH = 1024;

        private const int BUFFER_LENGTH_LONG = 4096;

        private static readonly Regex HEADER_REGEX = new Regex(
            @"(?:^(?<request>(?<method>GET|HEAD|POST|PUT|DELETE|TRACE|CONNECT)\s(?<url>(?:(?<schema>\w+)\://)?(?<host>[^/: ]+)?(?:\:(?<port>\d+))?\S*)\s(?<version>.*)\r\n)|^(?<response>(?<version>HTTP\S+)\s(?<status>(?<code>\d+).*)\r\n))(?:(?<key>[\w\-]+):\s?(?<value>.*)\r\n)*\r\n",
            RegexOptions.Compiled);

        private int _chunkedNextBlockOffset;

        #region Properties

        public string Host { get; set; }

        public int Port { get; set; }

        public string HttpMethod { get; private set; }

        public string Url { get; private set; }

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

        public bool IsChunked { get; private set; }

        public bool IsSSL { get; set; }

        #endregion

        private HttpPackage(Match match)
        {
            if (match.Groups["request"].Success)
            {
                this.HttpMethod = match.Groups["method"].Value;
                this.Url = match.Groups["url"].Value;
                this.Host = match.Groups["host"].Value;
                this.Port = match.Groups["port"].Success
                    ? int.Parse(match.Groups["port"].Value)
                    : (match.Groups["schema"].Success && match.Groups["schema"].Value.ToLower() == "https" ? 443 : 80);
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
            if (stream == null || !stream.CanRead)
            {
                return null;
            }
            HttpPackage package = null;
            byte[] buffer = new byte[BUFFER_LENGTH];
            using (MemoryStream mem = new MemoryStream())
            {
                for (int len = stream.Read(buffer, 0, buffer.Length), loopCount = 1;
                    len > 0;
                    buffer = ++loopCount == 3 ? new byte[BUFFER_LENGTH_LONG] : buffer, len = stream.CanRead ? stream.Read(buffer, 0, buffer.Length) : 0)
                {
                    mem.Write(buffer, 0, len);
                    byte[] bin = mem.GetBuffer();
                    if (ValidatePackage(bin, (int)mem.Length, ref package))
                    {
                        if (package.ContentLength == 0
                            && package.HeaderItems.ContainsKey("Connection")
                            && package.HeaderItems["Connection"] == "close"
                            && !package.StartLine.Contains("Connection Established")) // Connection: close
                        {
                            Logger.Info("Connection Closed", ConsoleColor.Blue); // Debug only
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
            return Read(bin, bin.Length, bin.Length);
        }

        public static HttpPackage Read(byte[] bin, int length)
        {
            return Read(bin, length, length);
        }

        public static HttpPackage Read(byte[] bin, int length, int headerLength)
        {
            HttpPackage package = null;
            return ValidatePackage(bin, length, headerLength, ref package) ? package : null;
        }


        public byte[] GetContentBinary()
        {
            byte[] responseBin, tempBin = this.Binary;
            int length = 0, startIndex = this.ContentOffset;

            if (this.ContentLength > 0)
            {
                if (this.IsChunked)
                {
                    using (MemoryStream mem = new MemoryStream(this.ContentLength))
                    {
                        for (int i = this.ContentOffset, contentLength = 0;
                            i < this.Length;
                            i += (contentLength + 2), contentLength = 0)
                        {
                            for (int temp = this.Binary[i]; temp != 0x0D && i < this.Length; temp = this.Binary[++i])
                            {
                                contentLength = contentLength * 16 + (temp > 0x40 ? (temp > 0x60 ? temp - 0x60 : temp - 0x40) + 9 : temp - 0x30);
                            }
                            if (contentLength > 0)
                            {
                                i += 2;
                                mem.Write(this.Binary, i, contentLength);
                                length += contentLength;
                            }
                        }
                        tempBin = mem.GetBuffer();
                    }
                    startIndex = 0;
                }
                else
                {
                    length = this.ContentLength;
                }
            }

            responseBin = new byte[length];
            for (int i = 0, j = startIndex; i < responseBin.Length; i++, j++)
            {
                responseBin[i] = tempBin[j];
            }

            return responseBin;
        }


        private static bool ValidatePackage(byte[] bin, int length, ref HttpPackage package)
        {
            return ValidatePackage(bin, length, length, ref package);
        }

        private static bool ValidatePackage(byte[] bin, int length, int headerLength, ref HttpPackage package)
        {
            bool isValid = false;
            if (package == null)
            {
                string str = ASCIIEncoding.ASCII.GetString(bin, 0, headerLength);
                Match match = HEADER_REGEX.Match(str);
                if (match.Success)
                {
                    package = new HttpPackage(match);
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
                    package.IsChunked = true;
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
