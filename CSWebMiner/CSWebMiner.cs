﻿using System;
using System.IO;
using System.Net;
using System.Web;

namespace SCVProxy.CSWebMiner
{
    public class MinerHandler : IHttpHandler
    {
        private IMiner miner = new LocalMiner();

        public bool IsReusable
        {
            get { return true; }
        }

        /// <summary>
        /// Request:
        ///     SCV-Host        required
        ///     SCV-Port        required
        ///     SCV-IP          optional
        ///     SCV-SSL         optional
        ///     SCV-Encrypted   optional
        /// Response:
        ///     SCV-Exception   optional
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            if (request.HttpMethod == "GET")
            {
                response.Headers["SCV-Miner"] = "C#";
                response.Write("C# Miner is working!");
            }
            else if (request.HttpMethod == "POST")
            {
                try
                {
                    SCVRequestHeader scvHeader = ParseRequest(request);
                    EncryptionProvider encryptionProvider = scvHeader.IsEncrypted ? new EncryptionProvider(scvHeader.Host) : null;

                    byte[] buffer = new byte[request.ContentLength];
                    using (Stream stream = request.InputStream)
                    {
                        stream.Read(buffer, 0, buffer.Length);
                    }
                    if (scvHeader.IsEncrypted)
                    {
                        // Decrypt
                        encryptionProvider.Decrypt(buffer);
                    }
                    HttpPackage requestPackage = HttpPackage.Read(buffer);
                    if (requestPackage == null)
                    {
                        throw new Exception("request package is null!");
                    }
                    requestPackage.Host = scvHeader.Host;
                    requestPackage.Port = scvHeader.Port;
                    requestPackage.IsSSL = scvHeader.IsSsl;

                    HttpPackage responsePackage = miner.Fetch(requestPackage, new IPEndPoint(scvHeader.IP, scvHeader.Port));
                    if (responsePackage == null)
                    {
                        throw new Exception("response package is null!");
                    }

                    // Response
                    if (scvHeader.IsEncrypted)
                    {
                        // Encrypt
                        encryptionProvider.Encrypt(responsePackage.Binary, 0, responsePackage.Length);
                    }
                    using (Stream stream = response.OutputStream)
                    {
                        stream.Write(responsePackage.Binary, 0, responsePackage.Length);
                    }
                    response.ContentType = "image/gif";
                }
                catch (Exception ex)
                {
                    response.Clear();
                    response.Headers["SCV-Exception"] = ex.GetType().FullName;
                    response.Write(ex.Message);
                }
            }
            else
            {
                response.StatusCode = 404;
            }
            response.End();
        }

        /// <returns>
        /// SCVRequestHeader:
        ///     SCV-Host        required
        ///     SCV-Port        required
        ///     SCV-IP          optional
        ///     SCV-SSL         optional
        ///     SCV-Encrypted   optional
        /// </returns>
        private SCVRequestHeader ParseRequest(HttpRequest request)
        {
            string host = request.Headers["SCV-Host"];
            int port = int.Parse(request.Headers["SCV-Port"]);

            IPAddress ip = String.IsNullOrEmpty(request.Headers["SCV-IP"]) ? DnsHelper.GetHostAddress(host) : IPAddress.Parse(request.Headers["SCV-IP"]);
            bool isSSL = bool.TryParse(request.Headers["SCV-SSL"], out isSSL) && isSSL;
            bool isEncrypted = bool.TryParse(request.Headers["SCV-Encrypted"], out isEncrypted) && isEncrypted;

            return new SCVRequestHeader()
            {
                Host = host,
                Port = port,
                IP = ip,
                IsSsl = isSSL,
                IsEncrypted = isEncrypted
            };
        }

        private class SCVRequestHeader
        {
            public string Host;

            public int Port;

            public IPAddress IP;

            public bool IsSsl;

            public bool IsEncrypted;
        }
    }
}
