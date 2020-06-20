using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IkSocks5.Core
{
    public class IKSocksServer : IDisposable
    {
        private TcpListener Socks5Server { get; set; }
        private Queue<ClientTunnel> Clients = new Queue<ClientTunnel>();
        private bool disposedValue;

        public void StartServer()
        {
            try
            {
                Socks5Server = new TcpListener(new IPEndPoint(IPAddress.IPv6Any, 1080));
                Socks5Server.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                Socks5Server.Start();

                NonBlockingConsole.WriteLine($"IkSocks5 is running and waiting for connections on {Socks5Server.LocalEndpoint}");

                Socks5Server.BeginAcceptTcpClient(AcceptCallback, Socks5Server);
            }
            catch (Exception ex)
            {
                NonBlockingConsole.WriteLine(ex.Message);
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
                        Clients.Enqueue(client);
                        NonBlockingConsole.WriteLine($"Client {clientTcpClient?.Client?.RemoteEndPoint} trying to connect, handling on thread {Thread.CurrentThread.ManagedThreadId}"); 
                        client?.Listen();  //Blocking
                        Clients.Dequeue();
                        client?.Dispose();
                    });                   
                }

                //Keep listening incoming connections.
                Socks5Server.BeginAcceptTcpClient(AcceptCallback, Socks5Server);
            }
            catch (Exception ex)
            {
                NonBlockingConsole.WriteLine(ex.Message);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try { Socks5Server?.Stop(); } catch { } //Terminating, we dont care.
                    try { Socks5Server?.Server?.Dispose(); } catch { } //Terminating, we dont care.

                    //Stop any active clients.
                    while (Clients.Count > 0)
                        Clients.Dequeue().Stop();                   
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
