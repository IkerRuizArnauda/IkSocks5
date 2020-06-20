using System;
using IkSocks5.Core;

namespace IkSocks5
{
    class Program
    {
        static void Main(string[] args)
        {
            using (IKSocksServer socks5Server = new IKSocksServer())
            {
                socks5Server.StartServer();
                Console.ReadLine();
            }
        }
    }
}
