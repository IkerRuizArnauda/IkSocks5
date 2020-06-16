using System;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;

using IkSocks5.Core.Packets;

namespace IkSocks5.Core
{
    public class ClientTunnel : IDisposable
    {
        ///
        ///  ClientTCPClient <-> ThisServer <-> RemoteTCPClient
        /// 

        /// <summary>
        /// This is out Socks5 client.
        /// </summary>
        public TcpClient ClientTCPClient { get; set; }

        /// <summary>
        /// This is the client remote request handler.
        /// </summary>
        public TcpClient RemoteTCPClient { get; set; }

        /// <summary>
        /// Disconnect inactive sockets after 10 seconds.
        /// </summary>
        private Stopwatch Inactivity = new Stopwatch();

        /// <summary>
        /// Client already went through handshake.
        /// </summary>
        public bool Authenticated = false;

        /// <summary>
        /// Client already told us his request, IPv4, IPv6 or Domain request.
        /// </summary>
        public bool BridgeMode = false;

        /// <summary>
        /// Initialize a ClientTunnel
        /// </summary>
        /// <param name="client">Provided by our TcpListener EndAccept.</param>
        public ClientTunnel(TcpClient client)
        {
            ClientTCPClient = client;
            ClientTCPClient.ReceiveBufferSize = 1000000;
            ClientTCPClient.SendBufferSize = 1000000;

            RemoteTCPClient = new TcpClient();
            RemoteTCPClient.ReceiveBufferSize = 1000000;
            RemoteTCPClient.SendBufferSize = 1000000;
        }

        /// <summary>
        /// Begin listening for client requests.
        /// </summary>
        public void Listen()
        {
            while (true)
            {
                try
                {
                    NetworkStream clientStream = ClientTCPClient.GetStream();
                    NetworkStream remoteStream = null;

                    //If this client already went through the CONNECT cmd, initialize its networkstream.
                    if (RemoteTCPClient.Connected)
                        remoteStream = RemoteTCPClient.GetStream();

                    if (ClientTCPClient.Available > 0)
                    {
                        Inactivity.Restart();

                        //How much bytes is our client sending.
                        byte[] buffer = new byte[ClientTCPClient.Available];
                        //Read those bytes.
                        var read = clientStream.Read(buffer, 0, buffer.Length);
                        //Parse the header.
                        var message = ReadMessageType(this, buffer);

                        //Process MessageType
                        switch (message)
                        {
                            //Client requested us with an auth method (Handshake), we support 0x01 (NoAuth) for now.
                            case MessageType.MethodRequest:
                                {
                                    //Parse the client method request.
                                    using (MethodRequest mReq = new MethodRequest(buffer))
                                    {
                                        if (mReq.Valid) //We successfully built the packet.
                                        {
                                            Console.WriteLine($"Client {ClientTCPClient?.Client?.RemoteEndPoint} AUTHENTICATION complete.");
                                            Authenticated = true; //Flag as authenticated.

                                            //Write response onto our client stream.
                                            using (MethodResponse response = new MethodResponse(mReq))
                                                clientStream.Write(response.Data, 0, response.Data.Length);
                                        }
                                        else
                                            throw new Exception($"Client {ClientTCPClient?.Client?.RemoteEndPoint} Invalid authentication request.");

                                        break;
                                    }
                                }
                            //After a successful handshake, the client tell us his request.
                            case MessageType.DataRequest:
                                {
                                    //Parse the datarequest from the client.
                                    using (var dReq = new DataRequest(buffer))
                                    {
                                        //Handle the command, so far we only support 0x01 CONNECT.
                                        switch (dReq.Command)
                                        {
                                            case Command.Connect:
                                                Console.WriteLine($"Client {ClientTCPClient?.Client?.RemoteEndPoint} CONNECT request to {dReq?.DestinationAddress}:{dReq?.Port}");
                                                //Try to connect to the remote endpoint the client is requesting.
                                                RemoteTCPClient.Connect(dReq.DestinationAddress, dReq.Port);

                                                RequestResult result = RequestResult.Succeeded;
                                                if (RemoteTCPClient.Connected)
                                                {
                                                    Console.WriteLine($"Client {ClientTCPClient?.Client?.RemoteEndPoint} GRANTED connection. Entering tunnel mode.");
                                                }
                                                else
                                                {
                                                    result = RequestResult.Network_Unreachable;
                                                    Console.WriteLine($"Client {ClientTCPClient?.Client?.RemoteEndPoint} host REJECTED the connection.");
                                                }

                                                //Write the result to our client stream.
                                                using (DataResponse dResponse = new DataResponse(result, dReq.AddressType, dReq.DestinationBytes, dReq.PortBytes))
                                                    clientStream.Write(dResponse.Data, 0, dResponse.Data.Length);

                                                if (result != RequestResult.Succeeded)
                                                    throw new Exception($"Client {ClientTCPClient?.Client?.RemoteEndPoint} Remote host reject the connection.");

                                                break;
                                            default:
                                                throw new Exception($"Client {ClientTCPClient?.Client?.RemoteEndPoint} Unsuported message, disconnecting client.");
                                        }
                                    }
                                    break;
                                }
                            //The client already wenth through handshake and datarequest, at this point we are just passing data between client <-> remote 
                            case MessageType.Tunnel:
                                //We write the client data onto our remote stream.
                                remoteStream.Write(buffer, 0, buffer.Length);
                                break;
                        }
                    }

                    //If the remote stream has sent us anything back.
                    if (RemoteTCPClient.Connected && RemoteTCPClient.Available > 0)
                    {
                        Inactivity.Restart();
                        byte[] remoteBuff = new byte[RemoteTCPClient.Available];

                        //Read what the remote endpoint sent us.
                        remoteStream.Read(remoteBuff, 0, remoteBuff.Length);
                        //Route it back to our client stream.
                        clientStream.Write(remoteBuff, 0, remoteBuff.Length);
                    }

                    //If this client has reported no activity in 10 seconds, kill it. We have no better way of knowing with TcpClients.
                    if (Inactivity.Elapsed.TotalSeconds > 18)
                        break;

                    Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    //Something went wrong, exit loop, this will unstuck the calling thread and will call Dispose)= on this object.
                    Console.WriteLine(ex.Message);
                    break;
                }
            }
        }

        /// <summary>
        /// Parse the kind of data we are receiving
        /// </summary>
        /// <returns></returns>
        private MessageType ReadMessageType(ClientTunnel client, byte[] data)
        {
            if (!client.Authenticated)
                return MessageType.MethodRequest;
            else if (data[0] == 0x05)
                return MessageType.DataRequest;
            else
                return MessageType.Tunnel;
        }

        /// <summary>
        /// Weak dispose, just dereference.
        /// </summary>
        public void Dispose()
        {
            Console.WriteLine($"Client {ClientTCPClient?.Client?.RemoteEndPoint} Disconnected.");

            ClientTCPClient = null;
            RemoteTCPClient = null;
        }
    }
}
