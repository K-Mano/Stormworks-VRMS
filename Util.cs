using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormworks_VRMS
{
    public class Util
    {
        // イベントハンドラを定義
        public event EventHandler<ConsoleStreamEventArgs> ConsoleStreamEvent;
        public delegate void ConsoleStreamEventHandler(ConsoleStreamEventArgs e);
        public class ConsoleStreamEventArgs : EventArgs
        {
            public ConsoleStreamEventArgs(string log)
            {
                Log = log;
            }

            public string Log { get; set; }
        }
        private List<string> con_stream = new List<string>();
        public enum ConsoleEventLevel
        {
            INFO = 0,
            WARN = 1,
            ERROR = 2,
            CRITICAL = 3,
            DATA = 4
        }
        public void ConsoleWriteDetails(string text, ConsoleEventLevel type)
        {
            string time = DateTime.Now.ToString();
            string line = string.Format("[{0}][{1}]: {2}", time, type, text);
            con_stream.Add(line);
            Console.WriteLine(line);
            ConsoleStreamEvent(this, new ConsoleStreamEventArgs(line));
        }

        public string ConsoleReadAt(int count)
        {
            try
            {
                return con_stream[count];
            }
            catch(IndexOutOfRangeException)
            {
                return null;
            }
        }

        public List<string> GetConsoleLogList()
        {
            return con_stream;
        }
    }
}
