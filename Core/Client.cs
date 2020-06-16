using System;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;

using IkSocks5.Core.Packets;

namespace IkSocks5.Core
{
    public class Client : IDisposable
    {
        public TcpClient ClientTCPClient { get; set; }
        public TcpClient RemoteTCPClient { get; set; }

        private Stopwatch Inactivity = new Stopwatch();
        public bool Authenticated = false;
        public bool BridgeMode = false;

        public Client(TcpClient client)
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
                    NetworkStream ns = ClientTCPClient.GetStream();
                    NetworkStream nsr = null;
                    if (RemoteTCPClient.Connected)
                        nsr = RemoteTCPClient.GetStream();

                    if (ClientTCPClient.Available > 0)
                    {
                        Inactivity.Restart();
                        byte[] buffer = new byte[ClientTCPClient.Available];
                        var read = ns.Read(buffer, 0, buffer.Length);

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
                                        else
                                            throw new Exception("Invalid authentication request, disconnecting client.");

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
                                                    RemoteTCPClient.Connect(dReq.DestinationAddress, dReq.Port);
                                                    var requestResult = RemoteTCPClient.Connected ? RequestResult.Succeeded : RequestResult.Network_Unreachable;
                                                    using (DataResponse dResponse = new DataResponse(requestResult, dReq.AddressType, dReq.DestinationBytes, dReq.PortBytes))
                                                        ns.Write(dResponse.Data, 0, dResponse.Data.Length);
                                                break;
                                            default:
                                                throw new Exception("Unsuported message, disconnecting client.");
                                        }
                                    }
                                    break;
                                }
                            case MessageType.Tunnel:
                                nsr.Write(buffer, 0, buffer.Length);
                                break;
                        }
                    }

                    if (RemoteTCPClient.Connected && RemoteTCPClient.Available > 0)
                    {
                        Inactivity.Restart();
                        byte[] remoteBuff = new byte[RemoteTCPClient.Available];
                        nsr.Read(remoteBuff, 0, remoteBuff.Length);
                        ns.Write(remoteBuff, 0, remoteBuff.Length);
                    }

                    if (Inactivity.Elapsed.TotalSeconds > 18)
                        break;

                    Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message); 
                    break;
                }
            }        
        }

        public void Dispose()
        {
            Console.WriteLine("Client disconnected.");

            ClientTCPClient = null;
            RemoteTCPClient = null;
        }
    }
}
