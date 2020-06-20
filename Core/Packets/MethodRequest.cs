using System;
using System.IO;

namespace IkSocks5.Core.Packets
{
    /// +----+----------+----------+
    /// |VER | NMETHODS | METHODS  |
    /// +----+----------+----------+
    /// | 1  | 1        | 1 to 255 |
    /// +----+----------+----------+
    public class MethodRequest : BinaryReader
    {
        /// <summary>
        /// Socks version.
        /// </summary>
        public ushort Version { get; private set; }

        /// <summary>
        /// Number of auth methods.
        /// </summary>
        public ushort Methods { get; private set; }

        /// <summary>
        /// Auth Method selection.
        /// </summary>
        public Method Method { get; private set; }

        /// <summary>
        /// Packet formed properly.
        /// </summary>
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
