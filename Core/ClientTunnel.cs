﻿using System;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;

using IkSocks5.Core.Packets;

namespace IkSocks5.Core
{
    /// <summary>
    /// ClientTCPClient <-> ClientTunnel <-> RemoteTCPClient
    /// </summary>
    public class ClientTunnel : IDisposable
    {
        /// <summary>
        /// This is out Socks5 client.
        /// </summary>
        public TcpClient ClientTCPClient { get; private set; }

        /// <summary>
        /// This is the client remote request handler.
        /// </summary>
        public TcpClient RemoteTCPClient { get; private set; }

        /// <summary>
        /// Disconnect inactive sockets after 10 seconds.
        /// </summary>
        private Stopwatch Inactivity = new Stopwatch();

        /// <summary>
        /// Client already went through handshake.
        /// </summary>
        public bool Authenticated = false;

        public Method AuthMethod { get; private set; } = Method.Null;

        /// <summary>
        /// Exit flag.
        /// </summary>
        private volatile bool IsRunning = false;

        /// <summary>
        /// Client local ep.
        /// </summary>
        public string ClientLocalEndPont { get; private set; }

        /// <summary>
        /// Initialize a ClientTunnel
        /// </summary>
        /// <param name="client">Provided by our TcpListener EndAccept.</param>
        public ClientTunnel(TcpClient client)
        {
            if (client == null)
                throw new Exception("Null TcpClient.");

            ClientTCPClient = client;
            ClientTCPClient.ReceiveBufferSize = 512000; //512Kb
            ClientTCPClient.SendBufferSize = 512000; //512Kb

            ClientLocalEndPont = client.Client.RemoteEndPoint.ToString();

            IsRunning = true;
        }

        /// <summary>
        /// Begin listening for client requests.
        /// </summary>
        public void Listen()
        {
            using (NetworkStream clientStream = ClientTCPClient.GetStream())
            {
                //Authentication and Endpoint request process.
                while (IsRunning)
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
                                //Client requeste with NoAuth method.
                                case MessageType.MethodRequest:
                                    {
                                        //Parse the client method request.
                                        using (MethodRequest mReq = new MethodRequest(buffer))
                                        {
                                            if (mReq.Valid) //We successfully built the packet.
                                            {
                                                AuthMethod = mReq.Method;

                                                if (AuthMethod == Method.NoAuthentication)
                                                {
                                                    NonBlockingConsole.WriteLine($"Client {ClientTCPClient?.Client?.RemoteEndPoint} AUTHENTICATION complete.");
                                                    Authenticated = true; //Flag as authenticated.
                                                }

                                                //Write response onto our client stream.
                                                using (MethodResponse response = new MethodResponse(mReq))
                                                    clientStream.Write(response.Data, 0, response.Data.Length);
                                            }
                                            else
                                                throw new Exception($"Client {ClientTCPClient?.Client?.RemoteEndPoint} Invalid Method request.");

                                            break;
                                        }
                                    }
                                //Client sent auth request, in this case, UserPass which is the only "secure" method supported.
                                case MessageType.AuthRequest:
                                    {
                                        //Parse the client auth request.
                                        using (AuthenticationRequest authReq = new AuthenticationRequest(buffer))
                                        {
                                            if (authReq.Valid) //We successfully built the packet.
                                            {
                                                //Create our response which will also validate login information.
                                                using (AuthenticationResponse authRes = new AuthenticationResponse(authReq))
                                                {
                                                    if (authRes.Valid)
                                                    {
                                                        NonBlockingConsole.WriteLine($"Client {ClientTCPClient?.Client?.RemoteEndPoint} AUTHENTICATION complete.");
                                                        Authenticated = true; //Flag as authenticated.
                                                    }

                                                    //Send handshake result.
                                                    clientStream.Write(authRes.Data, 0, authRes.Data.Length);
                                                }

                                                if (!Authenticated) //Wrong authentication, throw and disconnect this client.
                                                    throw new Exception($"Client {ClientTCPClient?.Client?.RemoteEndPoint} Invalid authentication request.");
                                            }
                                            else //Wrong authentication, throw and disconnect this client.
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
                                                    //Create the remote socket according to the request AddressFamily
                                                    if (RemoteTCPClient == null)
                                                    {
                                                        RemoteTCPClient = new TcpClient(dReq.DestinationAddress.AddressFamily);
                                                        RemoteTCPClient.ReceiveBufferSize = 512000; //512Kb
                                                        RemoteTCPClient.SendBufferSize = 512000; //512Kb
                                                    }

                                                    NonBlockingConsole.WriteLine($"Client {ClientTCPClient?.Client?.RemoteEndPoint} CONNECT request to {dReq?.DestinationAddress}:{dReq?.Port}");
                                                    //Try to connect to the remote endpoint the client is requesting.
                                                    RemoteTCPClient.Connect(dReq.DestinationAddress, dReq.Port);

                                                    Result result;
                                                    if (RemoteTCPClient.Connected)
                                                    {
                                                        result = Result.Succeeded;
                                                        NonBlockingConsole.WriteLine($"Client {ClientTCPClient?.Client?.RemoteEndPoint} GRANTED connection. Entering tunnel mode.");
                                                    }
                                                    else
                                                    {
                                                        result = Result.Network_Unreachable;
                                                        NonBlockingConsole.WriteLine($"Client {ClientTCPClient?.Client?.RemoteEndPoint} host REJECTED the connection.");
                                                    }

                                                    //Write the result to our client stream.
                                                    using (DataResponse dResponse = new DataResponse(result, dReq.AddressType, dReq.DestinationBytes, dReq.PortBytes))
                                                        clientStream.Write(dResponse.Data, 0, dResponse.Data.Length);

                                                    if (result != Result.Succeeded)
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
                        if (Inactivity.Elapsed.TotalSeconds > 10)
                            break;

                        Thread.Sleep(1);
                    }
                    catch (Exception ex)
                    {
                        //Something went wrong, exit loop, this will unstuck the calling thread and will call Dispose on this object.
                        NonBlockingConsole.WriteLine($"[ERROR] {ex.Message}");
                        break;
                    }
                }

                try
                {
                    if (Authenticated)
                    {
                        //The client already went through handshake and datarequest, at this point we are just passing data between client <-> remote 
                        using (NetworkStream remoteStream = RemoteTCPClient.GetStream())
                        {
                            while (IsRunning && Authenticated)
                            {
                                if (Inactivity.Elapsed.TotalSeconds > 10)
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
                                    //Something went wrong, exit loop, this will unstuck the calling thread and will call Dispose on this object.
                                    NonBlockingConsole.WriteLine($"[ERROR] {ex.Message}");
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    NonBlockingConsole.WriteLine($"[ERROR] {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Parse the kind of data we are receiving
        /// </summary>
        /// <returns></returns>
        private MessageType ReadMessageType(ClientTunnel client, byte[] data)
        {
            if (!client.Authenticated && client.AuthMethod == Method.Null)
                return MessageType.MethodRequest;
            else if (!client.Authenticated && client.AuthMethod == Method.UserPassword)
                return MessageType.AuthRequest;
            else if (data[0] == 0x05)
                return MessageType.DataRequest;
            else
                return MessageType.Null;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        /// <summary>
        /// Weak dispose, just dereference.
        /// </summary>
        public void Dispose()
        {
            NonBlockingConsole.WriteLine($"Client {ClientLocalEndPont} Disconnected.");

            Stop();
            Inactivity?.Stop();
            Inactivity = null;
            ClientTCPClient = null;
            RemoteTCPClient = null;
        }
    }
}
