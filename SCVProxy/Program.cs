using System;
using System.Linq;
using System.Net;

namespace SCVProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "SCVProxy";
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BufferHeight = 5000;
            Console.BufferWidth = 200;

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
T: Show threads
,: Decrease log level
.: Increase log level
",
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
                    case ConsoleKey.P:
                        Logger.Info("Pending Requests: " + listener.PendingRequestsCount);
                        break;
                    case ConsoleKey.OemComma: //,
                    case ConsoleKey.OemPeriod: //.
                        int level = Logger.LogLevel + (key == ConsoleKey.OemComma ? -1 : 1);
                        Logger.LogLevel = level < 1 ? 1 : level > 3 ? 3 : level;
                        Logger.Info("Log level is changed to: " + Logger.LogLevel);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
