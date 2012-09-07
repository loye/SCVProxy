using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace SCVProxy
{
    public interface IMiner
    {
        HttpPackage Fech(HttpPackage request, IPEndPoint endPoint = null);
    }

    public class LocalMiner : IMiner
    {
        public HttpPackage Fech(HttpPackage request, IPEndPoint endPoint = null)
        {
            endPoint = endPoint ?? new IPEndPoint(Dns.GetHostAddresses(request.Host)[0], request.Port);
            using (Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(endPoint);
                using (NetworkStream stream = new NetworkStream(socket))
                {
                    stream.Write(request.Binary, 0, request.Length);
                    return HttpPackage.Read(stream);
                }
            }
        }
    }

    public class CSWebMiner : IMiner
    {
        public HttpPackage Fech(HttpPackage request, IPEndPoint endPoint = null)
        {
            string minerUrl = "http://127.0.0.1:88/miner";
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(minerUrl);
            //if (endPoint != null)
            //{
            //    webRequest.Proxy = new WebProxy(endPoint.Address.ToString() + endPoint.Port, false);
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
