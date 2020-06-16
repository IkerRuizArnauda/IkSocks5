using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace IkSocks5.Core
{
    class IKSocksServer
    {
        private TcpListener Server;
        private ClientHandler ClientManager = new ClientHandler();

        public void StartServer()
        {
            try
            {
                Server = new TcpListener(new IPEndPoint(IPAddress.Any, 1080));
                Server.Start();

                //If our server is correctly bound.
                if (Server.Server.Connected)
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
                Server.BeginAcceptTcpClient(AcceptCallback, Server);

                if (ar.AsyncState is TcpListener serverSocket)
                {
                    Task.Run(() =>
                    {
                        TcpClient clientTcpClient = serverSocket.EndAcceptTcpClient(ar);
                        new Client(clientTcpClient).Listen();
                    });       
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
