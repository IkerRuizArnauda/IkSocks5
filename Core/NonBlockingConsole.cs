using System;
using System.Threading;
using System.Collections.Concurrent;

namespace IkSocks5
{
    public static class NonBlockingConsole
    {
        private static BlockingCollection<string> m_Queue = new BlockingCollection<string>();

        /// <summary>
        /// Static constructor
        /// </summary>
        static NonBlockingConsole() { var t = new Thread(() => { while (true) Console.WriteLine(m_Queue.Take()); }); t.IsBackground = true; t.Start(); }
        public static void WriteLine(string value) { m_Queue.Add($"{DateTime.Now} {value}"); }
    }
}
