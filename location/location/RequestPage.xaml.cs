using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace location
{
    public partial class RequestPage : Page
    {
        Frame frame;
        Protocol protocol;
        string address, person, location;

        public RequestPage(Frame mainFrame, bool getOrSet)
        {
            InitializeComponent();
            if (getOrSet)
            {
                locationLbl.Visibility = Visibility.Visible;
                locationTxt.Visibility = Visibility.Visible;
            }
            frame = mainFrame;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            outputTxt.Text = "";
            person = usernameTxt.Text;
            location = locationTxt.Text;
            address = addressTxt.Text;
            int port = 0;
            try { port = int.Parse(portTxt.Text); }
            catch { MessageBox.Show("Please enter an integer value for the port number! Default: 43", "Error", MessageBoxButton.OK); return; }

            int timeout = 0;
            try { timeout = int.Parse(timeoutTxt.Text); }
            catch { MessageBox.Show("Please enter an integer value for the timeout! Default: 1000", "Error", MessageBoxButton.OK); return; }

            bool debug = (bool)debugCheckBox.IsChecked;

            switch (protocolCombo.SelectionBoxItem)
            {
                case "Whois":
                    protocol = Protocol.Whois;
                    break;
                case "HTTP 0.9":
                    protocol = Protocol.HTTP9;
                    break;
                case "HTTP 1.0":
                    protocol = Protocol.HTTP0;
                    break;
                case "HTTP 1.1":
                    protocol = Protocol.HTTP1;
                    break;
            }

            TcpClient client = new TcpClient(address, port);
            if (timeout > 0) { client.ReceiveTimeout = timeout; }
            NetworkStream ns = client.GetStream();

            string request = SendRequest(ns);
            if (debug)
            {
                outputTxt.Text += "Connected to: " + client.Client.RemoteEndPoint + "\r\n";
                outputTxt.Text += "Timeout set to: " + client.ReceiveTimeout + "ms" + "\r\n";
                outputTxt.Text += "Sent Request: " + request + "\r\n";
            }

            try
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                string response = HandleResponse(ns);
                sw.Stop();
                if (debug) { outputTxt.Text += "Time for response: " + sw.ElapsedMilliseconds + "ms\r\n"; }
                outputTxt.Text += response;
            }
            catch (IOException) { outputTxt.Text += "Request timed out. Disconnecting..."; client.Close(); }
            if (outputTxt.Height > outputTxt.MaxHeight)
            {
                while (outputTxt.Height > outputTxt.MaxHeight)
                {
                    outputTxt.FontSize--;
                }
            }
        }

        private string SendRequest(NetworkStream ns)
        {
            string request = "";
            switch (protocol)
            {
                case Protocol.Whois:
                    request = WhoisManager.CreateRequest(person, location);
                    break;
                case Protocol.HTTP9:
                case Protocol.HTTP0:
                case Protocol.HTTP1:
                    request = HttpManager.CreateRequest(protocol, person, location, address);
                    break;
            }
            ns.Write(Encoding.ASCII.GetBytes(request), 0, request.Length);
            ns.Flush();
            return request;
        }

        private string HandleResponse(NetworkStream ns)
        {
            string response = "";
            switch (protocol)
            {
                case Protocol.Whois:
                    response = WhoisManager.HandleResponse(ns);
                    break;
                case Protocol.HTTP9:
                case Protocol.HTTP0:
                case Protocol.HTTP1:
                    response = HttpManager.HandleResponse(ns);
                    break;
            }
            return response;
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            frame.Content = null;
        }
    }
}
