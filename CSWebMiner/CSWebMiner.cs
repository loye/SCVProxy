using System;
using System.IO;
using System.Net;
using System.Web;

namespace SCVProxy.CSWebMiner
{
    public class MinerHandler : IHttpHandler
    {
        #region IHttpHandler Members

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
                bool isSSL = bool.TryParse(request.Headers["SCV-SSL"], out isSSL) && isSSL;
                string host = request.Headers["SCV-Host"];
                int port = int.Parse(request.Headers["SCV-Port"]);
                IPAddress ip = String.IsNullOrEmpty(request.Headers["SCV-IP"]) ? Dns.GetHostAddresses(host)[0] : IPAddress.Parse(request.Headers["SCV-IP"]);

                byte[] buffer = new byte[request.ContentLength];
                using (Stream stream = request.InputStream)
                {
                    stream.Read(buffer, 0, buffer.Length);
                }
                HttpPackage requestPackage = HttpPackage.Read(buffer);

                IPEndPoint endPoint = new IPEndPoint(ip, port);
                requestPackage.Host = host;
                requestPackage.Port = port;
                requestPackage.IsSSL = isSSL;
                HttpPackage responsePackage = miner.Fetch(requestPackage, endPoint);

                response.Headers["SCV-Length"] = responsePackage.Length.ToString();
                using (Stream stream = response.OutputStream)
                {
                    stream.Write(responsePackage.Binary, 0, responsePackage.Length);
                }
            }
            else
            {
                response.StatusCode = 404;
            }
            response.End();
        }

        #endregion
    }
}
