using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace IkSocks5.Core
{
    public class ClientHandler
    {
        public Dictionary<IPEndPoint, Client> Clients = new Dictionary<IPEndPoint, Client>();

        public bool HasClient(IPEndPoint ipEndPoint) => Clients.ContainsKey(ipEndPoint);

        public void AddClient(TcpClient client)
        {
            Clients.Add(client.Client.RemoteEndPoint as IPEndPoint, new Client(client));
        }        
    }
}
