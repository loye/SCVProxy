using System;
using System.Net;
using System.Net.Sockets;
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
                .AppendFormat("Listen Address\t:\t{0}\n", this.tcpListener.LocalEndpoint.ToString())
                .AppendFormat("Miner Type\t:\t{0}\n", this.miner.GetType().Name);
            if (this.ProxyEndPoint != null)
            {
                sb.AppendFormat("Proxy Address\t:\t{0}\n", this.ProxyEndPoint.ToString());
            }
            sb.AppendLine("------------------------------------------------------------");
            return sb.ToString();
        }

        private void DoAccept(IAsyncResult ar)
        {
            TcpListener tcp = (ar.AsyncState as TcpListener);
            tcp.BeginAcceptTcpClient(new AsyncCallback(DoAccept), tcp);

            using (TcpClient client = tcp.EndAcceptTcpClient(ar))
            using (NetworkStream stream = client.GetStream())
            {
                HttpPackage request = HttpPackage.Read(stream);
                if (request != null)
                {
                    Logger.Message(request.StartLine);
                    HttpPackage response;
                    try
                    {
                        response = this.miner.Fech(request, this.ProxyEndPoint);
                    }
                    catch (Exception) //TODO: to be removed
                    {
                        Logger.Error(request.StartLine);
                        throw;
                    }

                    if (response != null)
                    {
                        stream.Write(response.Binary, 0, response.Length);
                        Logger.Message(response.Header);
                        //Connection: keep-alive
                    }
                    else
                    {
                        Logger.Error("NULL ERROR:#########################################\r\n" + request.Header);
                    }
                }
            }
        }
    }
}
