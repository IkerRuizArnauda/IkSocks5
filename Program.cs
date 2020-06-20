using System;
using IkSocks5.Core;

namespace IkSocks5
{
    class Program
    {
        static void Main(string[] args)
        {
            using (IKSocksServer socksServer = new IKSocksServer())
            {
                socksServer.StartServer();
                Console.ReadLine();
            }
        }
    }
}
