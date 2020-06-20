using System.IO;
using IkSocks5.Accounts;

namespace IkSocks5.Core.Packets
{
    /// +----+--------+
    /// |VER | STATUS |
    /// +----+--------+
    /// | 1  |    1   |
    /// +----+--------+
    public class AuthenticationResponse : BinaryWriter
    {
        public bool Valid { get; private set; }
        public byte[] Data { get; private set; }
        public AuthenticationResponse(AuthenticationRequest request) : base(new MemoryStream())
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Write((byte)request.Subnegotiation);
                Write((byte)(AccountManager.Authenticate(request) ? (byte)Result.Succeeded : (byte)Result.General_SOCKS_Server_Failure)); //Anything greater than 0 will be taken as Authentication fail by the client.

                BaseStream.Position = 0;
                BaseStream.CopyTo(ms);
                Data = ms.ToArray();

                Valid = (byte)Result.Succeeded == Data[1];
            }
        }

        /// <summary>
        /// Use BinaryWriter Flush() to dereference our Data array.
        /// </summary>
        public override void Flush()
        {
            Data = null;
            base.Flush();
        }
    }
}
