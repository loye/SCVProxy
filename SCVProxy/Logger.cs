﻿using System;
using System.Net.Sockets;

namespace SCVProxy
{
    internal static class Logger
    {
        private static readonly object consoleLocker = new object();

        public static int LogLevel = 1;

        public static void Message(string message, int level = 1, ConsoleColor color = ConsoleColor.Gray)
        {
            if (level <= 0 || level >= LogLevel)
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
            Message(message, 0, color);
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
