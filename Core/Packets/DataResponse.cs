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
                Write((byte)5); //Version
                Write((byte)result); //RequestResult
                Write((byte)0x00); //Reserved byte, always 0x00
                Write((byte)addressType); //AddressType
                Write(adressBytes); //AddressBytes 4 octets or 15.
                Write(portBytes); //Port bytes in network format (reversed)

                //Copy the base stream out into our Data array.
                BaseStream.Position = 0;
                BaseStream.CopyTo(ms);
                Data = ms.ToArray();
            }
        }
    }
}
