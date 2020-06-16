using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace IkSocks5.Core
{
    class IKSocksServer
    {
        private TcpListener Server;

        public void StartServer()
        {
            try
            {
                Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
                Server = new TcpListener(new IPEndPoint(IPAddress.Any, 1080));
                Server.Start();

                Console.WriteLine("IkSocks5 is running and waiting for connections...");

                Server.BeginAcceptTcpClient(AcceptCallback, Server);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                if (ar.AsyncState is TcpListener serverSocket)
                {
                    TcpClient clientTcpClient = serverSocket.EndAcceptTcpClient(ar);
                    var client = new Client(clientTcpClient);
                    Task.Run(() =>
                    {
                        Console.WriteLine($"New incoming connection from {clientTcpClient.Client.RemoteEndPoint} running on threadID {Thread.CurrentThread.ManagedThreadId}");
                        client.Listen();  //Blocking
                        client?.Dispose();
                    });                   
                }

                //Keep listening incoming connections.
                Server.BeginAcceptTcpClient(AcceptCallback, Server);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
