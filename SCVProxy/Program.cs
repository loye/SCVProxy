using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace SCVProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            //new Listener<LocalMiner>("127.0.0.1", 1000).Start();
            //new Listener<LocalMiner>("127.0.0.1", 1000, "127.0.0.1", 1001).Start();
            new Listener<WebMiner>("127.0.0.1", 1000).Start();
            //new Listener<WebMiner>("127.0.0.1", 1000, "127.0.0.1", 8888).Start();

            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.T)
                {
                    Console.WriteLine();
                    Console.WriteLine("Threads:" + System.Diagnostics.Process.GetCurrentProcess().Threads.Count);
                }
            }
        }

    }
}
