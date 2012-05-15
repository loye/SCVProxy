using System;
using System.Net;
using System.Net.Sockets;

namespace SCVProxy
{
    public class Listener
    {
        private TcpListener tcpListener;

        private IMiner miner;

        public IPEndPoint Proxy { get; set; }

        public Listener(string ip, int port)
        {
            tcpListener = new TcpListener(IPAddress.Parse(ip), port);
            miner = new LocalMiner();
        }

        public Listener(string ip, int port, string proxyIp, int proxyPort)
            : this(ip, port)
        {
            this.Proxy = new IPEndPoint(IPAddress.Parse(proxyIp), proxyPort);
        }

        public void Start()
        {
            this.tcpListener.Start(100);
            this.tcpListener.BeginAcceptTcpClient(new AsyncCallback(DoAccept), tcpListener);
        }

        private void DoAccept(IAsyncResult ar)
        {
            TcpListener tcp = (ar.AsyncState as TcpListener);
            tcp.BeginAcceptTcpClient(new AsyncCallback(DoAccept), tcp);
            using (TcpClient client = tcp.EndAcceptTcpClient(ar))
            {
                using (NetworkStream stream = client.GetStream())
                {
                    HttpPackage request = HttpPackage.Read(stream);
                    Console.WriteLine(string.Format("{0}[{1}={2}+{3}]",
                        request.StartLine,
                        request.Length,
                        request.ContentOffset,
                        request.ContentLength));
                    DateTime startTime = DateTime.Now;
                    HttpPackage response = this.miner.Fech(request, this.Proxy);
                    if (response != null)
                    {
                        Console.WriteLine(string.Format("{0}[{1}]", response.StartLine, (DateTime.Now - startTime)));
                        stream.Write(response.Binary, 0, response.Length);
                    }
                    else
                    {
                        Console.WriteLine("NULL ERROR:#########################################\r\n" + request.Header);
                    }
                }
            }
        }
    }
}
