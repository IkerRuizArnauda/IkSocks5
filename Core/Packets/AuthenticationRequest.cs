using System.IO;

namespace IkSocks5.Core.Packets
{
    // Security Considerations: rfc1929
    // This document describes a subnegotiation that provides authentication
    // services to the SOCKS protocol. Since the request carries the
    // password in cleartext, this subnegotiation is not recommended for
    // environments where "sniffing" is possible and practical.

    ///+----+------+----------+------+----------+
    ///|VER | ULEN |  UNAME   | PLEN |  PASSWD  |
    ///+----+------+----------+------+----------+
    ///| 1  |  1   | 1 to 255 |  1   | 1 to 255 |
    ///+----+------+----------+------+----------+
    public class AuthenticationRequest : BinaryReader
    {
        public byte Subnegotiation { get; private set; }
        public byte[] Username { get; private set; }
        public byte[] Password { get; private set; }
        public bool Valid { get; private set; }
        public AuthenticationRequest(byte[] data) : base(new MemoryStream(data))
        {
            try
            {
                Subnegotiation = ReadByte(); //Subnegotiation 0x01
                Username = ReadBytes(ReadByte());
                Password = ReadBytes(ReadByte());
                Valid = true;
            }
            catch
            {
                Valid = false;
            }
        }
    }
}
