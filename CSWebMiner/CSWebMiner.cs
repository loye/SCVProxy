using System;
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

        public void ProcessRequest(HttpContext context)
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            if (request.HttpMethod == "GET")
            {
                response.Write("C# Miner is working!");
            }
            else if (request.HttpMethod == "POST")
            {
                try
                {
                    bool isSSL = bool.TryParse(request.Headers["SCV-SSL"], out isSSL) && isSSL;
                    bool isEncrypted = bool.TryParse(request.Headers["SCV-Encrypted"], out isEncrypted) && isEncrypted;
                    string host = request.Headers["SCV-Host"];
                    int port = int.Parse(request.Headers["SCV-Port"]);
                    IPAddress ip = String.IsNullOrEmpty(request.Headers["SCV-IP"]) ? Dns.GetHostAddresses(host)[0] : IPAddress.Parse(request.Headers["SCV-IP"]);

                    EncryptionProvider encryptionProvider = isEncrypted ? new EncryptionProvider(host) : null;

                    byte[] buffer = new byte[request.ContentLength];
                    using (Stream stream = request.InputStream)
                    {
                        stream.Read(buffer, 0, buffer.Length);
                    }
                    if (isEncrypted)
                    {
                        encryptionProvider.Decrypt(buffer);
                    }
                    HttpPackage requestPackage = HttpPackage.Read(buffer);

                    requestPackage.Host = host;
                    requestPackage.Port = port;
                    requestPackage.IsSSL = isSSL;

                    HttpPackage responsePackage = miner.Fetch(requestPackage, new IPEndPoint(ip, port));

                    response.Headers["SCV-IP"] = ip.ToString();
                    if (isEncrypted)
                    {
                        encryptionProvider.Encrypt(responsePackage.Binary, responsePackage.Length);
                    }
                    using (Stream stream = response.OutputStream)
                    {
                        stream.Write(responsePackage.Binary, 0, responsePackage.Length);
                    }
                }
                catch (Exception ex)
                {
                    response.StatusCode = 500;
                    response.Clear();
                    response.Write(ex.Message);
                    response.End();
                }
            }
            else
            {
                response.StatusCode = 404;
            }
            response.End();
        }
    }
}
