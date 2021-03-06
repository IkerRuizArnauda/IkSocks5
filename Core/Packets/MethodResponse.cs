﻿using IkSocks5.Configuration;
using System.IO;

namespace IkSocks5.Core.Packets
{
    /// +----+--------+
    /// |VER | METHOD |
    /// +----+--------+
    /// | 1  |    1   |
    /// +----+--------+
    public class MethodResponse : BinaryWriter
    {
        public byte[] Data;
        public MethodResponse(MethodRequest request) : base(new MemoryStream())
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Write((byte)request.Version);
                Write((byte)ConfigurationManager.AuthenticationMethod);

                BaseStream.Position = 0;
                BaseStream.CopyTo(ms);
                Data = ms.ToArray();
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
