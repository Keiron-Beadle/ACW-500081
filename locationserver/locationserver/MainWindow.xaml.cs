using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace locationserver
{
    public partial class MainWindow : Window
    {
        BackgroundWorker bw = new BackgroundWorker();

        public MainWindow(string[] args)
        {
            InitializeComponent();
            Utility.ProcessArguments(args);
            FillUI();
            Utility.CheckIfPortOpen();
            Utility.OnLoad();
            bw.DoWork += Work;
        }

        private void Start_Click(object o, RoutedEventArgs e)
        {
            Utility.databaseDir = dirTxt.Text;
            Utility.logDir = logTxt.Text;
            Utility.port = int.Parse(portTxt.Text);
            Utility.bDebug = (bool)debugCheckBox.IsChecked;
            Utility.timeout = int.Parse(timeoutTxt.Text);
            if (!bw.IsBusy)
            {
                bw.RunWorkerAsync();
            }
            else
            {
                Utility.UpdateUIText(outputTxt, "Already running server from this process...\r\n");
                MessageBoxResult result = MessageBox.Show("Would you like to restart server? Warning: Data may be lost if no database directory has been specified previously.\r\n",
                            "Warning", MessageBoxButton.YesNo);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        Utility.CloseServer();
                        bw.Dispose();
                        bw = new BackgroundWorker();
                        bw.DoWork += Work;
                        bw.RunWorkerAsync();
                        break;
                    case MessageBoxResult.No:
                        break;
                }
            }
        }

        private void Work(object sender, DoWorkEventArgs e)
        {
            Utility.RunServer(outputTxt);
        }

        private void FillUI()
        {
            dirTxt.Text = Utility.databaseDir;
            logTxt.Text = Utility.logDir;
            portTxt.Text = Utility.port.ToString();
            debugCheckBox.IsChecked = Utility.bDebug;
            timeoutTxt.Text = Utility.timeout.ToString();
        }
    }
}
