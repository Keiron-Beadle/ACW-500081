using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Get_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(new RequestPage(mainFrame, false));
        }

        private void Set_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(new RequestPage(mainFrame, true));
        }
    }
}
