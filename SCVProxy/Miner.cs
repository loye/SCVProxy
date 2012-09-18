using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace SCVProxy
{
    public interface IMiner
    {
        HttpPackage Fech(HttpPackage request, IPEndPoint proxyEndPoint = null);
    }

    public class LocalMiner : IMiner
    {
        public HttpPackage Fech(HttpPackage request, IPEndPoint proxyEndPoint = null)
        {
            IPEndPoint remoteEndPoint = proxyEndPoint ?? new IPEndPoint(Dns.GetHostAddresses(request.Host)[0], request.Port);
            using (Socket socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(remoteEndPoint);
                using (NetworkStream networkStream = new NetworkStream(socket))
                {
                    Stream stream = networkStream;
                    if (request.IsSsl)
                    {
                        stream = SwitchToSslStream(stream, request, proxyEndPoint != null);
                        if (stream == null)
                        {
                            return null;
                        }
                    }
                    stream.Write(request.Binary, 0, request.Length);
                    HttpPackage response = HttpPackage.Read(stream);
                    stream.Close();
                    return response;
                }
            }
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

    public class CSWebMiner : IMiner
    {
        public HttpPackage Fech(HttpPackage request, IPEndPoint proxyEndPoint = null)
        {
            string minerUrl = "http://127.0.0.1:88/miner";
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(minerUrl);
            //if (proxyEndPoint != null)
            //{
            //    webRequest.Proxy = new WebProxy(proxyEndPoint.Address.ToString() + proxyEndPoint.Port, false);
            //}
            webRequest.Headers["SCV-Host"] = request.Host;
            webRequest.Headers["SCV-Port"] = request.Port.ToString();
            webRequest.Method = "POST";
            using (Stream stream = webRequest.GetRequestStream())
            {
                stream.Write(request.Binary, 0, request.Length);
            }
            byte[] buffer;
            using (WebResponse response = webRequest.GetResponse())
            {
                buffer = new byte[response.ContentLength];
                using (Stream stream = response.GetResponseStream())
                {
                    for (int s = 0, l = buffer.Length - s, length = stream.Read(buffer, s, l);
                         s + length < buffer.Length;
                         s += length, l = buffer.Length - s, length = stream.Read(buffer, s, l)) ;
                }
            }
            Console.WriteLine(buffer.Length);
            return HttpPackage.Read(buffer);
        }
    }
}
