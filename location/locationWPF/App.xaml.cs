using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace locationWPF
{
    public partial class App : Application
    {
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwId);

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        private void Initialisation(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 0)
            {
                FreeConsole();
                MainWindow wnd = new MainWindow();
                wnd.Show();
            }
            else
            {
                try {
                    AttachConsole(-1);
                    ConsoleWindow.Main(e.Args);
                    Environment.Exit(0);
                }
                catch { throw new Exception("No parent console to attach to."); }
            }
        }
    }
}
