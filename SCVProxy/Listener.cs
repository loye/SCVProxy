using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SCVProxy
{
    public class Listener<T> where T : IMiner, new()
    {
        private TcpListener tcpListener;

        private IMiner miner;

        public IPEndPoint ProxyEndPoint { get; set; }

        public Listener(string ip, int port)
        {
            tcpListener = new TcpListener(IPAddress.Parse(ip), port);
            miner = new T();
        }

        public Listener(string ip, int port, string proxyIp, int proxyPort)
            : this(ip, port)
        {
            this.ProxyEndPoint = new IPEndPoint(IPAddress.Parse(proxyIp), proxyPort);
        }

        public void Start()
        {
            this.tcpListener.Start(50);
            Logger.Info(this.ToString());
            this.tcpListener.BeginAcceptTcpClient(new AsyncCallback(DoAccept), tcpListener);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder()
                .AppendLine("----------------------------------------------------------------------")
                .AppendFormat("Listen Address\t:\t{0}\n", this.tcpListener.LocalEndpoint)
                .AppendFormat("Miner Type\t:\t{0}\n", this.miner);
            if (this.ProxyEndPoint != null)
            {
                sb.AppendFormat("Proxy Address\t:\t{0}\n", this.ProxyEndPoint);
            }
            sb.AppendLine("----------------------------------------------------------------------");
            return sb.ToString();
        }

        private void DoAccept(IAsyncResult ar)
        {
            TcpListener tcp = (ar.AsyncState as TcpListener);
            tcp.BeginAcceptTcpClient(new AsyncCallback(DoAccept), tcp);
            using (TcpClient client = tcp.EndAcceptTcpClient(ar))
            using (NetworkStream networkStream = client.GetStream())
            {
                HttpPackage request = null;
                HttpPackage response = null;
                bool keepAlive = false, isSsl = false;
                string host = null;
                int port = 0;
                Stream stream = networkStream;
                try
                {
                    for (request = HttpPackage.Read(stream);
                        request != null;
                        request = keepAlive ? HttpPackage.Read(stream) : null, response = null, keepAlive = false)
                    {
                        DateTime startTime = DateTime.Now;
                        if (isSsl)
                        {
                            request.Host = host;
                            request.Port = port;
                            request.IsSSL = true;
                        }
                        if (request.HttpMethod == "CONNECT")
                        {
                            stream = SwitchToSslStream(stream, request);
                            isSsl = keepAlive = true;
                            host = request.Host;
                            port = request.Port;
                        }
                        else
                        {
                            response = this.miner.Fetch(request, this.ProxyEndPoint, this.ProxyEndPoint != null);
                            if (response != null && stream.CanWrite)
                            {
                                stream.Write(response.Binary, 0, response.Length);
                                // Proxy-Connection: keep-alive
                                keepAlive = (request.HeaderItems.ContainsKey("Proxy-Connection")
                                    && request.HeaderItems["Proxy-Connection"] == "keep-alive")
                                    && response.HeaderItems.ContainsKey("Connection")
                                    && response.HeaderItems["Connection"] == "Keep-Alive";
                            }
                        }
                        // Log Message
                        DateTime endTime = DateTime.Now;
                        Logger.Message(
                            String.Format(
                                "[{0}] [{1}] [{2}:{3}]\n{4}{5}",
                                startTime.ToString("HH:mm:ss.fff"),
                                client.Client.RemoteEndPoint,
                                request.Host,
                                request.Port,
                                request.StartLine,
                                response == null
                                    ? null
                                    : String.Format("\n[{0}] {1}",
                                        (endTime - startTime).ToString(),
                                        response.StartLine)),
                            0,
                            (response != null || request.HttpMethod == "CONNECT") ? (isSsl ? ConsoleColor.DarkYellow : ConsoleColor.Gray) : ConsoleColor.Red);
                    } // end of for
                }
                catch (Exception ex)
                {
                    Logger.PublishException(ex, request != null ? String.Format("{0}:{1}\n{2}", request.Host, request.Port, request.StartLine) : null);
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                }
            } // end of using
        }

        private SslStream SwitchToSslStream(Stream stream, HttpPackage request)
        {
            SslStream sslStream = null;
            byte[] repBin = ASCIIEncoding.ASCII.GetBytes(String.Format("{0} 200 Connection Established\r\n\r\n", request.Version));
            stream.Write(repBin, 0, repBin.Length);
            X509Certificate2 cert = CAHelper.GetCertificate(request.Host);
            if (cert != null && cert.HasPrivateKey)
            {
                sslStream = new SslStream(stream, false);
                sslStream.AuthenticateAsServer(cert, false, SslProtocols.Tls, true);
            }
            return sslStream;
        }
    }
}
