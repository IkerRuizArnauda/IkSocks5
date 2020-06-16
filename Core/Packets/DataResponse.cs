using System.IO;

namespace IkSocks5.Core.Packets
{
    /// <summary>
    /// +----+-----+-------+------+----------+----------+
    /// |VER | REP | RSV   | ATYP | BND.ADDR | BND.PORT |
    /// +----+-----+-------+------+----------+----------+
    /// | 1  |  1  | X’00’ |  1   | Variable |    2     |
    /// +----+-----+-------+------+----------+----------+
    /// </summary>
    public class DataResponse : BinaryWriter
    {
        public byte[] Data;
        public DataResponse(RequestResult result, AddressType addressType, byte[] adressBytes, byte[] portBytes) : base(new MemoryStream())
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Write((byte)5);
                Write((byte)result);
                Write((byte)0x00);
                Write((byte)addressType);
                Write(adressBytes);
                Write(portBytes);

                BaseStream.Position = 0;
                BaseStream.CopyTo(ms);
                Data = ms.ToArray();
            }
        }
    }
}
