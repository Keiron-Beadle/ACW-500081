using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace locationserver
{
    public partial class App : Application
    {
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwId);

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        private void Initialisation(object sender, StartupEventArgs e)
        {
            bool windowed = false;
            foreach (string s in e.Args)
            {
                if (s == "-w")
                {
                    windowed = true;
                    break;
                }
            }
            if (windowed)
            {
                FreeConsole();
                MainWindow wnd = new MainWindow(e.Args);
                wnd.Show();
            }
            else
            {
                try
                {
                    AttachConsole(-1);
                    ConsoleWindow.Main(e.Args);
                    Environment.Exit(0);
                }
                catch { throw new Exception("No parent console to attach to."); }
            }
        }
    }
}
