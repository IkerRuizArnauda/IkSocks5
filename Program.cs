using System;
using IkSocks5.Core;

namespace IkSocks5
{
    class Program
    {
        static void Main(string[] args)
        {
            IKSocksServer socksServer = new IKSocksServer();
            socksServer.StartServer();

            Console.ReadLine();
        }
    }
}
