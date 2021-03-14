using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace locationserver
{
    public class ConsoleWindow
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(SetConsoleMsgEventHandler handler, bool add);

        private delegate bool SetConsoleMsgEventHandler(CtrlType msg);

        private enum CtrlType
        {
            CTRL_C_EVENT,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT,
            CTRL_SHUTDOWN_EVENT
        }

        public static void Main(string[] args)
        {
            Utility.ProcessArguments(args);
            Utility.CheckIfPortOpen();
            Utility.OnLoad();
            SetConsoleCtrlHandler(ConsoleExitHandler, true);
            //Thread t = new Thread(() => { Utility.RunServer(); });
            //t.Start();
            //while (true) { }
            Utility.RunServer();
        }

        private static bool ConsoleExitHandler(CtrlType msg)
        {
            switch (msg)
            {
                case CtrlType.CTRL_BREAK_EVENT:
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                    if (Utility.bDatabase)
                    {
                        Console.WriteLine("Saving database...");
                        Utility.OnExit();
                    }
                    return false;
                default:
                    return false;
            }
        }
    }
}
