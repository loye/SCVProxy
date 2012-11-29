using System;
using System.Net;

namespace SCVProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            IListener listener;

            switch (Config.MinerType)
            {
                case "HttpMiner":
                    listener = new Listener<HttpMiner>(Config.ListenAddress, Config.ListenPort);
                    break;
                case "LocalMiner":
                default:
                    listener = new Listener<LocalMiner>(Config.ListenAddress, Config.ListenPort);
                    break;
            }
            if (!String.IsNullOrEmpty(Config.ProxyAddress))
            {
                listener.ProxyEndPoint = new IPEndPoint(IPAddress.Parse(Config.ProxyAddress), Config.ProxyPort);
            }

            listener.Start();

            while (true)
            {
                var key = Console.ReadKey().Key;
                Console.WriteLine();
                switch (key)
                {
                    case ConsoleKey.H:
                        Logger.Info(
@"H: Help
I: Show information
C: Clear screen
T: Show threads",
                            ConsoleColor.Green);
                        break;
                    case ConsoleKey.I:
                        Logger.Info(listener.ToString(), ConsoleColor.Green);
                        break;
                    case ConsoleKey.C:
                        Console.Clear();
                        break;
                    case ConsoleKey.T:
                        Logger.Info("Threads:" + System.Diagnostics.Process.GetCurrentProcess().Threads.Count, ConsoleColor.Green);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
