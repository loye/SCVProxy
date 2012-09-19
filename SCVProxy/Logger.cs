using System;
using System.Net.Sockets;

namespace SCVProxy
{
    internal static class Logger
    {
        private static readonly object consoleLocker = new object();
        private static readonly int logLevel = 0;

        static Logger()
        {
            Console.Title = "SCVProxy";
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BufferHeight = 5000;
            Console.BufferWidth = 200;
        }

        public static void Message(string message, int level = 0, ConsoleColor color = ConsoleColor.Gray)
        {
            if (level == -1 || level >= logLevel)
            {
                lock (consoleLocker)
                {
                    Console.ForegroundColor = color;
                    Console.WriteLine(message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }

        public static void Info(string message, ConsoleColor color = ConsoleColor.Gray)
        {
            Message(message, -1, color);
        }

        public static void Warnning(string message)
        {
            Message(message, 2, ConsoleColor.Yellow);
        }

        public static void Error(string message)
        {
            Message(message, 3, ConsoleColor.Red);
        }

        public static void PublishException(Exception ex, string message = null)
        {
            ConsoleColor color = ConsoleColor.Red;
            int level = 3;
            message = String.IsNullOrEmpty(message) ? null : message + "\n";
            string exMessage = string.Format("{0}\n{1}\n{2}", ex.Message, ex.Source, ex.StackTrace);
            if (ex is SocketException)
            {
                color = ConsoleColor.Yellow;
                level = 2;
            }
            Message(string.Format("{0}{1}\n{2}\n", message, ex.GetType(), exMessage), level, color);
            // Publish inner exception
            if (ex.InnerException != null)
            {
                PublishException(ex.InnerException, "Inner Exception:");
            }
        }
    }
}
