using System;
using System.Linq;
using System.Threading;
using System.Net.Sockets;

using IkSocks5.Core.Packets;

namespace IkSocks5.Core
{
    public class Client
    {
        public TcpClient ClientTCPClient { get; set; }
        public TcpClient RemoteTCPClient { get; set; }

        public bool Authenticated = false;

        public Client(TcpClient client)
        {
            this.ClientTCPClient = client;
            this.ClientTCPClient.ReceiveBufferSize = 56000;
            this.ClientTCPClient.SendBufferSize = 56000;

            RemoteTCPClient = new TcpClient();
            RemoteTCPClient.ReceiveBufferSize = 56000;
            RemoteTCPClient.SendBufferSize = 56000;
        }

        /// <summary>
        /// Begin listening for client requests.
        /// </summary>
        public void Listen()
        {
            while (true)
            {
                NetworkStream ns = ClientTCPClient.GetStream();
                if (ClientTCPClient.Available > 0)
                {                
                    byte[] buffer = new byte[ClientTCPClient.Available];
                    var read = ns.Read(buffer, 0, ClientTCPClient.Available);

                    if (buffer.Length != read)
                        throw new Exception();
                    
                    var message = Header.ReadMessageType(this, buffer);

                    switch (message)
                    {
                        case MessageType.MethodRequest:
                            {
                                using (MethodRequest mReq = new MethodRequest(buffer))
                                {
                                    if (mReq.Valid)
                                    {
                                        Authenticated = true;

                                        using (MethodResponse response = new MethodResponse(mReq))
                                            ns.Write(response.Data, 0, response.Data.Length);
                                    }
                                    break;
                                }
                            }
                        case MessageType.DataRequest:
                            {
                                using (var dReq = new DataRequest(buffer))
                                {
                                    switch (dReq.Command)
                                    {
                                        case Command.Connect:
                                            try
                                            {
                                                RemoteTCPClient.Connect(dReq.DestinationAddress, dReq.Port);
                                                var requestResult = RemoteTCPClient.Connected ? RequestResult.Succeeded : RequestResult.Network_Unreachable;
                                                using (DataResponse dResponse = new DataResponse(requestResult, dReq.AddressType, dReq.DestinationBytes, dReq.PortBytes))
                                                    ns.Write(dResponse.Data, 0, dResponse.Data.Length);
                                            }
                                            catch { throw new Exception(); }
                                            break;
                                        default:
                                            throw new Exception("Unhandled");
                                    }
                                }
                                break;
                            }
                        case MessageType.Tunnel:
                            NetworkStream nsr = RemoteTCPClient.GetStream();
                            nsr.Write(buffer, 0, buffer.Length);
                            break;
                    }
                }

                if (RemoteTCPClient.Connected && RemoteTCPClient.Available > 0)
                {
                    NetworkStream nsr = RemoteTCPClient.GetStream();
                    byte[] remoteBuff = new byte[RemoteTCPClient.Available];
                    nsr.Read(remoteBuff, 0, remoteBuff.Length);

                    if (RemoteTCPClient.Connected)
                    {
                        try
                        {
                            ns.Write(remoteBuff, 0, remoteBuff.Length);
                        }
                        catch { Console.WriteLine("ERROR"); break; }
                    }
                }

                Thread.Sleep(10);
            }   
        }

        /// <summary>
        /// Receive a request from the client (PC/Device)
        /// </summary>
        //private void ReceiveCallback(IAsyncResult ar)
        //{
        //    if(ar.AsyncState is byte[] buffer)
        //    {
        //        var client = ClientTCPClient.GetStream();
        //        var read = client.EndRead(ar);
        //        var data = buffer.Take(read).ToArray();
        //        var message = Header.ReadMessageType(this, data);

                
        //    }
        //    //try
        //    //{
        //    //    if(ar.AsyncState is Socket clientSocket)
        //    //    {
        //    //        if (!clientSocket.Connected)
        //    //        {
        //    //            Console.WriteLine($"Invalid request, disconnecting client {clientSocket.RemoteEndPoint}");
        //    //            Socket.Shutdown(SocketShutdown.Both);
        //    //        }
        //    //        else
        //    //        {
        //    //            int received = clientSocket.EndReceive(ar);

        //    //            if (received == 0)
        //    //                return;

        //    //            byte[] data = new byte[received];
        //    //            Array.Copy(SocketBuffer, 0, data, 0, received);

        //    //            if (!HandlePacket(clientSocket, data))
        //    //            {
        //    //                Console.WriteLine($"Invalid request, disconnecting client {clientSocket.RemoteEndPoint}");
        //    //                Socket.Shutdown(SocketShutdown.Both);
        //    //            }
        //    //        }
        //    //    }
                
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    Console.WriteLine(ex.Message);
        //    //}
        //}

        private void BeginWrite(IAsyncResult ar)
        {
            var ns = RemoteTCPClient.GetStream();
            ns.EndWrite(ar);
            byte[] buffer = new byte[1024];
            ns.BeginRead(buffer, 0, buffer.Length, BeginRead, buffer);
        }

        private void BeginRead(IAsyncResult ar)
        {
            if (ar.AsyncState is byte[] buffer)
            {
                var ns = RemoteTCPClient.GetStream();
                var read = ns.EndRead(ar);
                var data = buffer.Take(read).ToArray();
                var cNs = ClientTCPClient.GetStream();

                cNs.Write(data, 0, read);
            }
        }

        /// <summary>
        /// Sent data to the client (PC/Device)
        /// </summary>
        //private void SendCallBack(IAsyncResult ar)
        //{
        //    try
        //    {
        //        if (ar.AsyncState is Socket clientSocket)
        //        {
        //            int sent = clientSocket.EndSend(ar);

        //            if (sent == 0)
        //                return;

        //            clientSocket.BeginReceive(SocketBuffer, 0, SocketBuffer.Length, SocketFlags.None, ReceiveCallback, clientSocket);
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //}

        //private bool HandlePacket(Socket clientSocket, byte[] data)
        //{
        //    try
        //    {
        //        var msgType = Header.ReadMessageType(this, data);

        //        switch (msgType)
        //        {
        //            case MessageType.MethodRequest:
        //                return HandleMessageRequest(clientSocket, data);
        //            case MessageType.DataRequest:
        //                return HandleDataRequest(data);
        //            default:
        //                return false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        return false;
        //    }
        //}

        private bool HandleDataRequest(byte[] data)
        {
            return true;
            //DataRequest dReq = null;
            //try
            //{
            //    dReq = new DataRequest(data);
                
            //    if (dReq.Valid)
            //    {
            //        if (dReq.PassThrough)
            //        { 
            //            RemoteSocket.BeginSend(dReq.Data, 0, dReq.Data.Length, SocketFlags.None, RemoteSend, new object[2] { RemoteSocket, dReq });
            //        }
            //        else
            //        {
            //            switch (dReq.Command)
            //            {
            //                case Command.Connect:
            //                    RemoteSocket.BeginConnect(dReq.DestinationAddress, dReq.Port, RemoteConnect, new object[2] { RemoteSocket, dReq });
            //                    break;
            //                default:
            //                    throw new Exception("Unhandled");
            //            }
            //        }

            //        return true;
            //    }
            //    else //Should send the client a reply with the error type.
            //    {
            //        dReq?.Dispose();
            //        return false;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    dReq?.Dispose();
            //    Console.WriteLine(ex.Message);
            //    return false;
            //}
        }

        private void RemoteSend(IAsyncResult ar)
        {
            //if (ar.AsyncState is object[] holder)
            //{
            //    Socket remoteSocket = holder[0] as Socket;
            //    DataRequest dRequest = holder[1] as DataRequest;

            //    try
            //    {
            //        var sent = remoteSocket.EndSend(ar);

            //        if (sent == 0)
            //            return;

            //        remoteSocket.BeginReceive(RemoteBuffer, 0, RemoteBuffer.Length, SocketFlags.None, RemoteReceive, new object[2] { remoteSocket, dRequest });
            //    }
            //    catch
            //    {
            //        Socket?.Shutdown(SocketShutdown.Both);
            //        remoteSocket?.Shutdown(SocketShutdown.Both);
            //    }
            //}
        }

        private void RemoteReceive(IAsyncResult ar)
        {
            //try
            //{
            //    if (ar.AsyncState is object[] holder)
            //    {
            //        Socket remoteSocket = holder[0] as Socket;
            //        DataRequest dRequest = holder[1] as DataRequest;

            //        if (remoteSocket.Connected)
            //        {
            //            int received = remoteSocket.EndReceive(ar);

            //            if (received == 0)
            //                return;

            //            byte[] data = new byte[received];
            //            Array.Copy(RemoteBuffer, 0, data, 0, received);

            //            Socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallBack, Socket);
            //        }
            //        else
            //            Socket.BeginReceive(SocketBuffer, 0, SocketBuffer.Length, SocketFlags.None, ReceiveCallback, Socket);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}
        }

        private void RemoteConnect(IAsyncResult ar)
        {
            //if (ar.AsyncState is object[] holder)
            //{
            //    Socket remoteSocket = holder[0] as Socket;
            //    DataRequest dRequest = holder[1] as DataRequest;

            //    try
            //    {
            //        remoteSocket.EndConnect(ar);

            //        using(dRequest)
            //        using (DataResponse dResponse = new DataResponse(RequestResult.Succeeded, dRequest.AddressType, dRequest.DestinationBytes, dRequest.PortBytes))
            //            Socket.BeginSend(dResponse.Data, 0, dResponse.Data.Length, SocketFlags.None, SendCallBack, Socket);
            //    }
            //    catch
            //    {
            //        Socket?.Shutdown(SocketShutdown.Both);
            //        remoteSocket?.Shutdown(SocketShutdown.Both);
            //    }
            //}
        }

        //private bool HandleMessageRequest(Socket clientSocket, byte[] data)
        //{
        //    try
        //    {
        //        using (MethodRequest mReq = new MethodRequest(data))
        //        {
        //            if (mReq.Valid)
        //            {
        //                Authenticated = true;

        //                using (MethodResponse response = new MethodResponse(mReq))
        //                    clientSocket.BeginSend(response.Data, 0, response.Data.Length, SocketFlags.None, SendCallBack, clientSocket);

        //                return true;
        //            }
        //            else
        //                return false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        return false;               
        //    }
        //}
    }
}
