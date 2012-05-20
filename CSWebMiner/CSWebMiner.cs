﻿using System.IO;
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
                string host = request.Headers["SCV-Host"];
                int port = int.Parse(request.Headers["SCV-Port"]);

                byte[] buffer = new byte[request.ContentLength];
                using (Stream stream = request.InputStream)
                {
                    stream.Read(buffer, 0, buffer.Length);
                }

                IPEndPoint endPoint = new IPEndPoint(Dns.GetHostAddresses(host)[0], port);
                HttpPackage requestPackage = HttpPackage.Read(buffer);
                HttpPackage responsePackage = miner.Fech(requestPackage, endPoint);

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