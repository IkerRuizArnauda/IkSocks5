namespace IkSocks5.Core
{
    public static class Header
    {
        public static MessageType ReadMessageType(Client client, byte[] data)
        {
            if (!client.Authenticated)
                return MessageType.MethodRequest;
            else if (data[0] == 0x05)
                return MessageType.DataRequest;
            else
                return MessageType.Tunnel; 
        }
    }
}
