namespace IkSocks5.Core
{
    public enum Method : byte
    {
        NoAuth        = 0x00,
        GSSAPI        = 0x01,
        UserPw        = 0x02,
        Null          = 0x03,
    }

    public enum MessageType : byte
    {
        Null           = 0x00,
        MethodRequest  = 0x01,
        AuthRequest    = 0x02,
        DataRequest    = 0x03,
    }

    public enum AddressType : byte
    {
        IPv4           = 0x01,
        DomainName     = 0x03,
        IPv6           = 0x04
    }

    public enum Command : byte
    {
        Connect        = 0x01,
        Bind           = 0x02,
        Udp            = 0x03,
    }

    public enum Result : byte
    {
        Succeeded                           = 0x00,
        General_SOCKS_Server_Failure        = 0x01,
        Connection_Not_Allowed_By_Ruleset   = 0x02,
        Network_Unreachable                 = 0x03,
        Host_Unreachable                    = 0x04,
        Connection_Refused                  = 0x05,
        TTL_Expired                         = 0x06,
        Command_Not_Supported               = 0x07,
        Address_Type_Not_Supported          = 0x08,
    }
}
