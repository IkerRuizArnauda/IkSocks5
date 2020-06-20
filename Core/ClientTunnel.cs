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
        /// Client local ep.
        /// </summary>
        public string ClientLocalEndPont { get; set; }

        /// <summary>
        /// Initialize a ClientTunnel
        /// </summary>
        /// <param name="client">Provided by our TcpListener EndAccept.</param>
        public ClientTunnel(TcpClient client)
        {
            if (client == null)
                throw new Exception("Null TcpClient.");
         
            ClientTCPClient = client;
            ClientTCPClient.ReceiveBufferSize = 500000;
            ClientTCPClient.SendBufferSize = 500000;

            ClientLocalEndPont = client.Client.RemoteEndPoint.ToString();
        }

        /// <summary>
        /// Begin listening for client requests.
        /// </summary>
        public void Listen()
        {
            NetworkStream clientStream = ClientTCPClient.GetStream();

            //Authentication and Endpoint request process.
            while (true)
            {
                try
                {
                    //How much bytes is our client sending.
                    byte[] buffer = new byte[ClientTCPClient.Available];
                    if (buffer.Length > 0)
                    {
                        Inactivity.Restart();
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
                                            NonBlockingConsole.WriteLine($"Client {ClientTCPClient?.Client?.RemoteEndPoint} AUTHENTICATION complete.");
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
                                        //Create the remote socket according to the request AddressFamily
                                        if (RemoteTCPClient == null)
                                        {
                                            RemoteTCPClient = new TcpClient(dReq.DestinationAddress.AddressFamily);
                                            RemoteTCPClient.ReceiveBufferSize = 500000;
                                            RemoteTCPClient.SendBufferSize = 500000;
                                        }

                                        //Handle the command, so far we only support 0x01 CONNECT.
                                        switch (dReq.Command)
                                        {
                                            case Command.Connect:
                                                NonBlockingConsole.WriteLine($"Client {ClientTCPClient?.Client?.RemoteEndPoint} CONNECT request to {dReq?.DestinationAddress}:{dReq?.Port}");
                                                //Try to connect to the remote endpoint the client is requesting.
                                                RemoteTCPClient.Connect(dReq.DestinationAddress, dReq.Port);

                                                RequestResult result = RequestResult.Succeeded;
                                                if (RemoteTCPClient.Connected)
                                                {
                                                    NonBlockingConsole.WriteLine($"Client {ClientTCPClient?.Client?.RemoteEndPoint} GRANTED connection. Entering tunnel mode.");
                                                }
                                                else
                                                {
                                                    result = RequestResult.Network_Unreachable;
                                                    NonBlockingConsole.WriteLine($"Client {ClientTCPClient?.Client?.RemoteEndPoint} host REJECTED the connection.");
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
                            case MessageType.Null:
                                throw new Exception("Invalid routing.");
                        }
                    }
                    buffer = null;

                    //At this point, if we have stablished a remote connection we go on and break in order to enter tunnel mode.
                    if (RemoteTCPClient != null && RemoteTCPClient.Connected)
                        break;

                    //If this client has reported no activity in 10 seconds, kill it. We have no better way of knowing with TcpClients.
                    if (Inactivity.Elapsed.TotalSeconds > 18)
                        break;

                    Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    //Something went wrong, exit loop, this will unstuck the calling thread and will call Dispose)= on this object.
                    NonBlockingConsole.WriteLine($"[ERROR] {ex.Message}");
                    break;
                }
            }

            if (Authenticated && RemoteTCPClient != null && RemoteTCPClient.Connected)
            {
                NetworkStream remoteStream = RemoteTCPClient.GetStream();

                using (clientStream)
                {
                    using (remoteStream)
                    {
                        //The client already went through handshake and datarequest, at this point we are just passing data between client <-> remote 
                        while (Authenticated && RemoteTCPClient != null && RemoteTCPClient.Connected)
                        {
                            if (Inactivity.Elapsed.TotalSeconds > 18)
                                break;

                            try
                            {
                                byte[] buffer = new byte[ClientTCPClient.Available];
                                if (buffer.Length > 0)
                                {
                                    Inactivity.Restart();
                                    var read = clientStream.Read(buffer, 0, buffer.Length);
                                    remoteStream.Write(buffer, 0, buffer.Length);
                                }

                                byte[] remoteBuff = new byte[RemoteTCPClient.Available];
                                if (remoteBuff.Length > 0)
                                {
                                    Inactivity.Restart();
                                    remoteStream.Read(remoteBuff, 0, remoteBuff.Length);
                                    clientStream.Write(remoteBuff, 0, remoteBuff.Length);
                                }

                                Thread.Sleep(1);
                            }
                            catch (Exception ex)
                            {
                                //Something went wrong, exit loop, this will unstuck the calling thread and will call Dispose)= on this object.
                                NonBlockingConsole.WriteLine($"[ERROR] {ex.Message}");
                                break;
                            }
                        }
                    }
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
                return MessageType.Null;
        }

        /// <summary>
        /// Weak dispose, just dereference.
        /// </summary>
        public void Dispose()
        {
            NonBlockingConsole.WriteLine($"Client {ClientLocalEndPont} Disconnected.");

            ClientTCPClient = null;
            RemoteTCPClient = null;
        }
    }
}
