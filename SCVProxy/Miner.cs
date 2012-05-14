using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SCVProxy
{
    interface IMiner
    {
        HttpPackage Fech(HttpPackage request, IPEndPoint proxy);
    }

    public class LocalMiner : IMiner
    {
        public HttpPackage Fech(HttpPackage request, IPEndPoint proxy)
        {
            IPEndPoint endPoint = proxy ?? new IPEndPoint(Dns.GetHostAddresses(request.Host).First(), request.Port);

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

    //public class CSMiner : IMiner
    //{
    //    public byte[] Fech(byte[] request)
    //    {
    //        WebRequest webRequest = WebRequest.Create("http://127.0.0.1/proxy.ashx");
    //        webRequest.Headers["ProxyType"] = "HTTP";
    //        webRequest.Method = "POST";
    //        using (Stream stream = webRequest.GetRequestStream())
    //        {
    //            stream.Write(request, 0, request.Length);
    //        }
    //        using (WebResponse response = webRequest.GetResponse())
    //        {
    //            byte[] buffer = new byte[response.ContentLength];
    //            using (Stream stream = response.GetResponseStream())
    //            {
    //                stream.Read(buffer, 0, (int)response.ContentLength);
    //            }
    //            return buffer;
    //        }
    //    }
    //}
}
