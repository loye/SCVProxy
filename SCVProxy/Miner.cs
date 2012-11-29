using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

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

    public class HttpMiner : IMiner
    {
        private List<MinerEndPoint> endPointList = Config.HttpMinerEndPointList;

        private bool isEncrypted = Config.Encrypt;

        private LocalMiner localMiner = new LocalMiner();

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
            HttpPackage response;
            MinerEndPoint minerEndPoint = this.endPointList[new Random().Next(0, this.endPointList.Count)];
            string header = String.Format(
@"POST {0} HTTP/1.1
Host: {1}
Content-Length: {2}
Connection: Close
SCV-SSL: {3}
SCV-Host: {4}
SCV-Port: {5}
SCV-IP: {6}
SCV-Encrypted: {7}

",
            minerEndPoint.Url,
            minerEndPoint.Host,
            request.Length,
            request.IsSSL,
            request.Host,
            request.Port,
            Dns.GetHostAddresses(request.Host).Where(a => a.AddressFamily == AddressFamily.InterNetwork).First(),
            isEncrypted);
            byte[] headerBin = ASCIIEncoding.ASCII.GetBytes(header);
            byte[] bin;
            int lenth = headerBin.Length + request.Length;
            using (MemoryStream mem = new MemoryStream(lenth))
            {
                mem.Write(headerBin, 0, headerBin.Length);
                mem.Write(request.Binary, 0, request.Length);
                bin = mem.GetBuffer();
            }
            HttpPackage httpRequest = HttpPackage.Read(bin, lenth, headerBin.Length);
            httpRequest.IsSSL = minerEndPoint.IsSSL;

            HttpPackage httpResponse = localMiner.Fetch(httpRequest, endPoint ?? minerEndPoint.EndPoint, byProxy);
            using (MemoryStream mem = new MemoryStream(httpResponse.ContentLength))
            {
                mem.Write(httpResponse.Binary, httpResponse.ContentOffset, httpResponse.ContentLength);
                response = HttpPackage.Read(mem.GetBuffer(), httpResponse.ContentLength);
            }
            return response;
        }

        public override string ToString()
        {
            return String.Format("{0} <{1}>", this.GetType().Name, String.Join("|", this.endPointList.Select(e => e.Url)));
        }
    }
}
