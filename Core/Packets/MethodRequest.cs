using System;
using System.IO;

namespace IkSocks5.Core.Packets
{
    /// <summary>
    /// +----+----------+----------+
    /// |VER | NMETHODS | METHODS  |
    /// +----+----------+----------+
    /// | 1  | 1        | 1 to 255 |
    /// +----+----------+----------+
    /// </summary>
    public class MethodRequest : BinaryReader
    {
        public ushort Version { get; private set; }
        public ushort Methods { get; private set; }
        public Method Method { get; private set; }
        public bool Valid { get; private set; } = true;

        public MethodRequest(byte[] data) : base(new MemoryStream(data))
        {
            try
            {
                if (data.Length == 3)
                {
                    Version = ReadByte();

                    //Socks v5
                    if (Version != 5)
                        Valid = false;

                    Methods = ReadByte();

                    Method = (Method)Enum.ToObject(typeof(Method), ReadByte());
                }
                else
                    Valid = false;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Valid = false;
            }
        }
    }
}
