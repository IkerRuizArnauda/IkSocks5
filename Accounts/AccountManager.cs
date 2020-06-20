using System;
using System.Text;
using IkSocks5.Core.Packets;

using IniParser;
using IniParser.Model;

namespace IkSocks5.Accounts
{
    /// <summary>
    /// Handle handshake validation for Auth method 0x01 (User:Pass)
    /// </summary>
    public static class AccountManager
    {
        private static FileIniDataParser Parser { get; set; }
        private static IniData IniData { get; set; }
        static AccountManager()
        {
            Parser = new FileIniDataParser();
            IniData = Parser.ReadFile($"{AppDomain.CurrentDomain.BaseDirectory}{@"\Accounts\Acocunts.INI"}");
        }

        public static bool Authenticate(AuthenticationRequest request)
        {
            try
            {
                return IniData["Accounts"][Encoding.ASCII.GetString(request.Username)].Equals(Encoding.ASCII.GetString(request.Password));
            }
            catch (Exception ex)
            {
                NonBlockingConsole.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
