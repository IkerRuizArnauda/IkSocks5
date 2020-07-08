using System;
using System.Net;
using IkSocks5.Core;

using IniParser;
using IniParser.Model;

namespace IkSocks5.Configuration
{
    public static class ConfigurationManager
    {
        public static IPAddress ListeningAddress { get; private set; }
        public static int ListneingPort { get; private set; }
        public static Method AuthenticationMethod { get; private set; }
        private static FileIniDataParser Parser { get; set; }
        private static IniData IniData { get; set; }

        static ConfigurationManager()
        {
            try
            {
                Parser = new FileIniDataParser();
                IniData = Parser.ReadFile($"{AppDomain.CurrentDomain.BaseDirectory}{@"\Configuration.ini"}");

                ListeningAddress = IPAddress.Parse(IniData["IKSOCKS5"]["IP"]);
                ListneingPort = Int32.Parse(IniData["IKSOCKS5"]["PORT"]);
                AuthenticationMethod = (Method)Int32.Parse(IniData["AUTHENTICATION"]["METHOD"]);
            }
            catch
            {
                NonBlockingConsole.WriteLine("Invalid configuration.");
            }
        }
    }
}
