using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace SCVProxy
{
    public interface IMiner
    {
        HttpPackage Fetch(HttpPackage request, IPEndPoint endPoint = null, bool byProxy = false);
    }

    public class LocalMiner : IMiner
    {
        public HttpPackage Fetch(HttpPackage request, IPEndPoint endPoint = null, bool byProxy = false)
        {
            IPEndPoint remoteEndPoint = endPoint ?? new IPEndPoint(Dns.GetHostAddresses(request.Host).Where(a => a.AddressFamily == AddressFamily.InterNetwork).First(), request.Port);
            using (Socket socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(remoteEndPoint);
                using (NetworkStream networkStream = new NetworkStream(socket))
                {
                    Stream stream = networkStream;
                    if (request.IsSSL)
                    {
                        stream = SwitchToSslStream(stream, request, byProxy);
                    }
                    if (stream == null || !stream.CanWrite)
                    {
                        return null;
                    }
                    stream.Write(request.Binary, 0, request.Length);
                    HttpPackage response = stream.CanRead ? HttpPackage.Read(stream) : null;
                    stream.Close();
                    return response;
                }
            }
        }

        public override string ToString()
        {
            return this.GetType().Name;
        }

        private SslStream SwitchToSslStream(Stream stream, HttpPackage request, bool byProxy)
        {
            SslStream ssltream = null;
            if (byProxy)
            {
                byte[] reqBin = ASCIIEncoding.ASCII.GetBytes(String.Format("CONNECT {0}:{1} {2}\r\nHost: {0}\r\nConnection: keep-alive\r\n{3}\r\n",
                    request.Host,
                    request.Port,
                    request.Version,
                    request.HeaderItems.ContainsKey("User-Agent") ? String.Format("User-Agent: {0}\r\n", request.HeaderItems["User-Agent"]) : String.Empty));
                stream.Write(reqBin, 0, reqBin.Length);
                HttpPackage response = HttpPackage.Read(stream);
                if (response != null && response.StatusCode == 200)
                {
                    ssltream = new SslStream(stream, false);
                    ssltream.AuthenticateAsClient(request.Host);
                }
            }
            else
            {
                ssltream = new SslStream(stream, false);
                ssltream.AuthenticateAsClient(request.Host);
            }
            return ssltream;
        }
    }

    public class WebMiner : IMiner
    {
        private List<string> minerUrlList = ConfigurationManager.AppSettings["WebMinerUrl"].Split('|').ToList();

        /// <summary>
        /// SCV-SSL     optional
        /// SCV-Host    required
        /// SCV-Port    required
        /// SCV-IP      optional
        /// </summary>
        /// <param name="request"></param>
        /// <param name="endPoint"></param>
        /// <param name="byProxy"></param>
        /// <returns></returns>
        public HttpPackage Fetch(HttpPackage request, IPEndPoint endPoint = null, bool byProxy = false)
        {
            string minerUrl = minerUrlList[new Random().Next(0, minerUrlList.Count)];

            //byte[] requestBin = ASCIIEncoding.ASCII.GetBytes(Convert.ToBase64String(request.Binary, 0, request.Length));

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(minerUrl);
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers["SCV-SSL"] = request.IsSSL.ToString();
            httpWebRequest.Headers["SCV-Host"] = request.Host;
            httpWebRequest.Headers["SCV-Port"] = request.Port.ToString();
            httpWebRequest.Headers["SCV-IP"] = Dns.GetHostAddresses(request.Host).Where(a => a.AddressFamily == AddressFamily.InterNetwork).First().ToString();
            if (byProxy && endPoint != null)
            {
                httpWebRequest.Proxy = new WebProxy(endPoint.Address.ToString(), endPoint.Port);
            }
            using (Stream stream = httpWebRequest.GetRequestStream())
            {
                stream.Write(request.Binary, 0, request.Length);
            }
            byte[] buffer;
            using (WebResponse response = httpWebRequest.GetResponse())
            {
                buffer = new byte[response.ContentLength];
                using (Stream stream = response.GetResponseStream())
                {
                    for (int s = 0, l = buffer.Length - s, length = stream.Read(buffer, s, l);
                         s + length < buffer.Length;
                         s += length, l = buffer.Length - s, length = stream.Read(buffer, s, l)) ;
                }
            }

            return HttpPackage.Read(buffer);
        }

        public override string ToString()
        {
            return String.Format("{0} <{1}>", this.GetType().Name, this.minerUrlList[0]);
        }
    }
}
