﻿using System;
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
                Server = new TcpListener(new IPEndPoint(IPAddress.IPv6Any, 1080));
                Server.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                Server.Start();

                NonBlockingConsole.WriteLine($"IkSocks5 is running and waiting for connections on {Server.LocalEndpoint}");

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
                    var client = new ClientTunnel(clientTcpClient);
                    
                    Task.Run(() =>
                    {
                        NonBlockingConsole.WriteLine($"Client {clientTcpClient?.Client?.RemoteEndPoint} trying to connect, handling on thread {Thread.CurrentThread.ManagedThreadId}"); 
                        client?.Listen();  //Blocking
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
