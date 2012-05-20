using System;
using System.Net;
using System.Net.Sockets;

namespace SCVProxy
{
    public class Listener<T> where T : IMiner, new()
    {
        //private static int requestCount = 0;

        //private static object locker = new object();

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
                    if (request != null)
                    {
                        //lock (locker) requestCount++;
                        Console.WriteLine(request.StartLine); HttpPackage response;
                        try
                        {
                            response = this.miner.Fech(request, this.ProxyEndPoint);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine(request.StartLine);
                            throw;
                        }
                        //lock (locker) Console.WriteLine(--requestCount);
                        if (response != null)
                        {
                            stream.Write(response.Binary, 0, response.Length);
                            Console.WriteLine(response.Header);
                            //Connection: keep-alive
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
}
