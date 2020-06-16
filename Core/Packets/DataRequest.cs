using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;

namespace IkSocks5.Core.Packets
{
    /// <summary>
    /// +----+-----+-------+------+----------+----------+
    /// |VER | CMD | RSV   | ATYP | DST.ADDR | DST.PORT |
    /// +----+-----+-------+------+----------+----------+
    /// | 1  |  1  | X’00’ |   1  | Variable |    2     |
    /// +----+-----+-------+------+----------+----------+
    /// </summary>
    public class DataRequest : BinaryReader
    {
        public ushort Version { get; private set; }
        public Command Command { get; private set; }
        private byte Reserved { get; set; }
        public AddressType AddressType { get; private set; }
        public IPAddress DestinationAddress { get; private set; }
        public byte[] DestinationBytes { get; private set; }
        public UInt16 Port { get; private set; }
        public byte[] PortBytes { get; private set; }
        public bool Valid { get; private set; } = true;
        public bool PassThrough { get; private set; } = false;
        public byte[] Data { get; private set; }

        public DataRequest(byte[] data) : base(new MemoryStream(data))
        {
            try
            {
                if (data[0] == 0x05)
                {
                    Version = ReadByte();
                    Command = (Command)Enum.ToObject(typeof(Command), ReadByte());
                    Reserved = ReadByte();
                    AddressType = (AddressType)Enum.ToObject(typeof(AddressType), ReadByte());

                    var data2 = Encoding.ASCII.GetString(data);
                    switch (AddressType)
                    {
                        case AddressType.IPv4:                  
                            DestinationAddress = new IPAddress(ReadBytes(4));
                            DestinationBytes = DestinationAddress.GetAddressBytes();
                            break;
                        case AddressType.IPv6:
                            DestinationAddress = new IPAddress(ReadBytes(16));
                            DestinationBytes = DestinationAddress.GetAddressBytes();
                            break;
                        case AddressType.DomainName:
                            var octets = ReadByte();
                            var domainBytes = ReadBytes(octets);
                            var domainName = Encoding.ASCII.GetString(domainBytes);

                            byte[] count = new byte[1] { octets };

                            DestinationBytes = count.Concat(domainBytes).ToArray();
                            DestinationAddress = Dns.GetHostAddresses(domainName)[0];
                            break;
                        default:
                            Valid = false;
                            break;
                    }

                    PortBytes = ReadBytes(2);
                    Port = BitConverter.ToUInt16(PortBytes.Reverse().ToArray(), 0);
                }
                else
                {
                    PassThrough = true;
                    Data = data;
                }
            }
            catch
            {
                Valid = false;
            }
        }
    }
}
