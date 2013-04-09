using System;
using System.Collections.Generic;
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
            if (request == null)
            {
                return null;
            }
            IPEndPoint remoteEndPoint = endPoint ?? new IPEndPoint(DnsHelper.GetHostAddress(request.Host), request.Port);
            using (Socket socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(remoteEndPoint);
                using (NetworkStream networkStream = new NetworkStream(socket, true))
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
                    stream.Dispose();
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
        /// Request:
        ///     SCV-Host        required
        ///     SCV-Port        required
        ///     SCV-IP          optional
        ///     SCV-SSL         optional
        ///     SCV-Encrypted   optional
        /// Response:
        ///     SCV-Exception   optional
        /// </summary>
        /// <param name="request"></param>
        /// <param name="endPoint"></param>
        /// <param name="byProxy"></param>
        /// <returns></returns>
        public HttpPackage Fetch(HttpPackage request, IPEndPoint endPoint = null, bool byProxy = false)
        {
            MinerEndPoint minerEndPoint = this.endPointList.Count == 1 ? this.endPointList[0] : this.endPointList[new Random().Next(0, this.endPointList.Count)];
            EncryptionProvider encryptionProvider = isEncrypted ? new EncryptionProvider(request.Host) : null;
            IPAddress ip;
            string header = String.Format(
@"POST {0} HTTP/1.1
Host: {1}:{2}
Content-Length: {3}
Connection: close
SCV-Host: {4}
SCV-Port: {5}
SCV-IP: {6}
SCV-SSL: {7}
SCV-Encrypted: {8}

",
            minerEndPoint.Url,
            minerEndPoint.Host,
            minerEndPoint.Port,
            request.Length,
            request.Host,
            request.Port,
            null, //DnsHelper.TryGetHostAddress(request.Host, out ip) ? ip : null,
            request.IsSSL,
            isEncrypted);

            byte[] headerBin = ASCIIEncoding.ASCII.GetBytes(header);
            byte[] bin;
            int length = headerBin.Length + request.Length;
            using (MemoryStream mem = new MemoryStream(length))
            {
                mem.Write(headerBin, 0, headerBin.Length);
                mem.Write(request.Binary, 0, request.Length);
                bin = mem.GetBuffer();
            }
            if (isEncrypted)
            {
                encryptionProvider.Encrypt(bin, headerBin.Length, request.Length);
            }
            HttpPackage httpRequest = HttpPackage.Read(bin, length, headerBin.Length);
            httpRequest.IsSSL = minerEndPoint.IsSSL;

            HttpPackage httpResponse = localMiner.Fetch(httpRequest, endPoint ?? minerEndPoint.EndPoint, byProxy);

            if (httpResponse != null && httpResponse.StatusCode == 200)
            {
                if (!httpResponse.HeaderItems.ContainsKey("SCV-Exception"))
                {
                    byte[] responseBin = httpResponse.GetContentBinary();
                    if (isEncrypted)
                    {
                        encryptionProvider.Decrypt(responseBin);
                    }
                    return HttpPackage.Read(responseBin);
                }
                else
                {
                    Logger.Error(String.Format("Exception From Remote Miner <{0}>:\r\nException: {1}\r\n{2}",
                        minerEndPoint.Url,
                        httpResponse.HeaderItems["SCV-Exception"],
                        ASCIIEncoding.ASCII.GetString(httpResponse.GetContentBinary())));
                    return null;
                }
            }
            return null;
        }

        public override string ToString()
        {
            return String.Format("{0} <{1}>", this.GetType().Name, String.Join("|", this.endPointList.Select(e => e.Url)));
        }
    }
}
