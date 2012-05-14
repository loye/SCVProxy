﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace SCVProxy
{
    public class HttpPackage
    {
        private const int BUFFER_LENGTH = 1024;

        private static readonly Regex HEADER_REGEX = new Regex(
            @"(?:(?<request>(?<method>GET|HEAD|POST|PUT|DELETE|TRACE|CONNECT)\s(?<url>(?:\w+://)?(?<host>[^/: ]+)(?:\:(?<port>\d+))?\S*)\s(?<version>.*)\r\n)|(?<response>(?<version>HTTP\S+)\s(?<status>(?<code>\d+).*)\r\n))(?:(?<key>[\w\-]+):\s?(?<value>.*)\r\n)*\r\n",
            RegexOptions.Compiled);

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

        public int ContentOffset { get { return Header == null ? Length : Header.Length; } }

        public int ContentLength { get; private set; }


        private HttpPackage(byte[] bin, Match match)
        {
            this.Binary = bin;
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
            this.ContentLength = headerItems.ContainsKey("Content-Length")
                ? int.Parse(headerItems["Content-Length"])
                : (headerItems.ContainsKey("Transfer-Encoding") && headerItems["Transfer-Encoding"] == "chunked" ? -1 : 0);
        }

        public static HttpPackage Read(NetworkStream stream)
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
                        return package;
                    }
                }
            }
            return null;
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
                    package = new HttpPackage(bin, match);
                }
                else
                {
                    Console.WriteLine("NOT MATCH:\r\n" + str);
                }
            }
            if (package != null)
            {
                package.Length = length;
                if (package.ContentLength == 0)
                {
                    isValid = true;
                }
                else if (package.ContentLength > 0)
                {
                    isValid = bin.Length >= package.ContentOffset + package.ContentLength;
                }
                else // Transfer-Encoding: chunked
                {
                    Console.WriteLine("Transfer-Encoding: chunked");

                    isValid = ValidateChunkedBlock(package, package.ContentOffset);
                }
            }
            return isValid;
        }

        private static bool ValidateChunkedBlock(HttpPackage package, int startIndex)
        {
            byte[] bin = package.Binary;
            int length = package.Length;
            if (startIndex > length - 5)
            {
                return false;
            }
            else if (bin[startIndex] == 0x30
                    && bin[startIndex + 1] == 0x0D
                    && bin[startIndex + 2] == 0x0A
                    && bin[startIndex + 3] == 0x0D
                    && bin[startIndex + 4] == 0x0A)
            {
                return true;
            }
            else
            {
                // TODO:
                int contentLength = 0;
                int i = startIndex;
                for (int temp = bin[i]; temp != 0x0D && i < length; temp = bin[++i])
                {
                    if (i >= length - 1)
                    {
                        return false;
                    }
                    contentLength += contentLength * 16 + temp > 0x40
                        ? (temp > 0x60 ? temp - 0x60 : temp - 0x40) + 9
                        : temp - 30;
                }
                int nextStartIndex = i + contentLength + 3;
                return ValidateChunkedBlock(package, nextStartIndex);
            }
        }
    }
}