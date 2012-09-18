using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
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
                .AppendLine("------------------------------------------------------------")
                .AppendFormat("Listen Address\t:\t{0}\n", this.tcpListener.LocalEndpoint)
                .AppendFormat("Miner Type\t:\t{0}\n", this.miner.GetType().Name);
            if (this.ProxyEndPoint != null)
            {
                sb.AppendFormat("Proxy Address\t:\t{0}\n", this.ProxyEndPoint);
            }
            sb.AppendLine("------------------------------------------------------------");
            return sb.ToString();
        }

        private void DoAccept(IAsyncResult ar)
        {
            TcpListener tcp = (ar.AsyncState as TcpListener);
            tcp.BeginAcceptTcpClient(new AsyncCallback(DoAccept), tcp);

            using (TcpClient client = tcp.EndAcceptTcpClient(ar))
            using (NetworkStream networkStream = client.GetStream())
            {
                bool keepAlive = false;
                string host = null;
                int port = 0;
                bool isSsl = false;
                HttpPackage response = null;
                Stream stream = networkStream;
                for (HttpPackage request = HttpPackage.Read(stream); request != null; request = keepAlive ? HttpPackage.Read(stream) : null, keepAlive = false)
                {
                    Logger.Message(String.Format("[{0}] {1}", client.Client.RemoteEndPoint, request.StartLine));
                    if (isSsl)
                    {
                        request.Host = host;
                        request.Port = port;
                        request.IsSsl = isSsl;
                    }
                    if (request.HttpMethod == "CONNECT")
                    {
                        try
                        {
                            stream = SwitchToSslStream(stream, request);
                        }
                        catch (Exception ex)
                        {
                            Logger.PublishException(ex, request.StartLine);
                        }
                        isSsl = true;
                        keepAlive = true;
                    }
                    else
                    {
                        try
                        {
                            response = this.miner.Fech(request, this.ProxyEndPoint);
                        }
                        catch (Exception ex) //TODO: to be removed
                        {
                            Logger.PublishException(ex, request.StartLine);
                        }

                        if (response != null)
                        {
                            stream.Write(response.Binary, 0, response.Length);
                            Logger.Message(response.StartLine);
                            // Proxy-Connection: keep-alive
                            keepAlive = (request.HeaderItems.ContainsKey("Proxy-Connection")
                                && request.HeaderItems["Proxy-Connection"] == "keep-alive")
                                && response.HeaderItems.ContainsKey("Connection")
                                && response.HeaderItems["Connection"] == "Keep-Alive";
                        }
                        else
                        {
                            Logger.Error("RESPONSE NULL ERROR:#########################################\r\n" + request.Header);
                        }
                    }
                    if (keepAlive)
                    {
                        host = request.Host;
                        port = request.Port;
                    }
                }
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
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
