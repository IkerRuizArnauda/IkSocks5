using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;

namespace IkSocks5.Core
{
    public class IKSocksServer : IDisposable
    {
        private TcpListener Socks5Server { get; set; }
        private HashSet<ClientTunnel> Clients = new HashSet<ClientTunnel>();
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
                if (disposedValue)
                    return;

                if (ar.AsyncState is TcpListener serverSocket)
                {
                    TcpClient clientTcpClient = serverSocket.EndAcceptTcpClient(ar);
                    var client = new ClientTunnel(clientTcpClient);

                    ThreadPool.QueueUserWorkItem((WaitCallback) => 
                    {
                        using (clientTcpClient)
                        {
                            using (client)
                            {
                                Clients.Add(client);
                                NonBlockingConsole.WriteLine($"Client {clientTcpClient?.Client?.RemoteEndPoint} trying to connect, handling on thread {Thread.CurrentThread.ManagedThreadId}");
                                client?.Listen();  //Blocking
                                Clients.Remove(client);
                            }
                        }
                    });                
                }

                if (disposedValue)
                    return;

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

                    var clients = Clients.ToArray();
                    //Stop any active clients.
                    foreach(ClientTunnel cTun in clients)
                    {
                        NonBlockingConsole.WriteLine($"Stopping client {cTun.ClientLocalEndPont}");
                        cTun?.Stop();
                        cTun?.Dispose();
                        Clients.Remove(cTun);
                    }
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
