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
/*
            Regex HEADER_REGEX = new Regex(
                @"(?:^(?<request>(?<method>GET|HEAD|POST|PUT|DELETE|TRACE|CONNECT)\s(?<url>(?:\w+://)?(?<host>[^/: ]+)(?:\:(?<port>\d+))?\S*)\s(?<version>.*)\r\n)|^(?<response>(?<version>HTTP\S+)\s(?<status>(?<code>\d+).*)\r\n))(?:(?<key>[\w\-]+):\s?(?<value>.*)\r\n)*\r\n",
                RegexOptions.Compiled);
            string src1 = @"GET http://clients2.google.com/service HTTP/1.1
Host: clients2.google.com
Connection: keep-alive
User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.19 (KHTML, like Gecko) Chrome/18.0.1025.168 Safari/535.19
Accept-Encoding: gzip,deflate,sdch
Accept-Language: en-US,zh-CN;q=0.8,en;q=0.6
Accept-Charset: GBK,utf-8;q=0.7,*;q=0.3

";
            string src2 = @"HTTP/1.1 200 OK
Cache-Control: private, max-age=0
Content-Type: text/html; charset=utf-8
Content-Encoding: gzip
Expires: Sun, 13 May 2012 09:05:50 GMT
P3P: CP=""NON UNI COM NAV STA LOC CURa DEVa PSAa PSDa OUR IND""
Date: Sun, 13 May 2012 09:06:50 GMT
Transfer-Encoding: chunked
Connection: keep-alive
Vary: Accept-Encoding
Connection: Transfer-Encoding

00000CE2";
*/
            //            Console.WriteLine(HEADER_REGEX.IsMatch(src1));
            //            Console.WriteLine(HEADER_REGEX.IsMatch(src2));
            //            var result1 = HEADER_REGEX.Match(src1);
            //            var result2 = HEADER_REGEX.Match(src2);
            //            Console.WriteLine(result1.Groups["host"].Value);
            //            Console.WriteLine(result2.Groups["host"].Value);


            //new Listener<LocalMiner>("127.0.0.1", 1000).Start();
            new Listener<LocalMiner>("127.0.0.1", 1000, "127.0.0.1", 8888).Start();
            //new Listener<CSWebMiner>("127.0.0.1", 1000, "127.0.0.1", 8888).Start();

            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.T)
                {
                    Console.WriteLine(System.Diagnostics.Process.GetCurrentProcess().Threads.Count);
                }
            }
        }

    }
}
