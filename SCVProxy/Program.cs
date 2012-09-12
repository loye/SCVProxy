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

            Regex HEADER_REGEX = new Regex(
@"(?:^(?<request>(?<method>GET|HEAD|POST|PUT|DELETE|TRACE|CONNECT)\s(?<url>(?:\w+://)?(?<host>[^/: ]+)?(?:\:(?<port>\d+))?\S*)\s(?<version>.*)\r\n)|^(?<response>(?<version>HTTP\S+)\s(?<status>(?<code>\d+).*)\r\n))(?:(?<key>[\w\-]+):\s?(?<value>.*)\r\n)*\r\n",
RegexOptions.Compiled);
            string src1 = @"GET / HTTP/1.1
Host: github.com
Connection: keep-alive
Cache-Control: max-age=0
User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/21.0.1180.89 Safari/537.1
Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8
Accept-Encoding: gzip,deflate,sdch
Accept-Language: en-US,zh-CN;q=0.8,en;q=0.6
Accept-Charset: GBK,utf-8;q=0.7,*;q=0.3
Cookie: tracker=direct; _gauges_unique_month=1; _gauges_unique_year=1; _gauges_unique=1; _gauges_unique_day=1; _gh_sess=BAh7BzoPc2Vzc2lvbl9pZCIlNDNmYTFmOTk0YTA1NTE0ZDJhZjIwMmE5YmIwZTc2YjI6EF9jc3JmX3Rva2VuIjFwNlk1OXZ0M211MUduNEdWY2liRmFBY1MrblJWNXpTbnIwblBSbStycG8wPQ%3D%3D--4f9d9f723dafc89adb4bd62112d997c5e3c36b2d; __utma=1.727679668.1347343532.1347429896.1347432712.5; __utmc=1; __utmz=1.1347343532.1.1.utmcsr=(direct)|utmccn=(direct)|utmcmd=(none)

";
            //Console.WriteLine(
            //HEADER_REGEX.Replace(src1, m => {
            //    m.Groups["url"].
            //    return null;
            //}));

            //var match = HEADER_REGEX.Match(src1);
            //Console.WriteLine(match.Success);
            //Console.WriteLine(match.Groups["url"].Value);
            //Console.WriteLine(match.Groups["host"].Value);
            //Console.WriteLine(match.Groups["port"].Value);

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
                    Console.WriteLine();
                    Console.WriteLine("Threads:" + System.Diagnostics.Process.GetCurrentProcess().Threads.Count);
                }
            }
        }

    }
}
