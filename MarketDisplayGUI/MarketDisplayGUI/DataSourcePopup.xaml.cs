using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace MarketDisplayGUI {
    /// <summary>
    /// Interaction logic for DataSourcePopup.xaml
    /// </summary>
    public partial class DataSourcePopup : Window {
        public DataSourcePopup() {
            InitializeComponent();

            this.DataContext = this;
        }

        private System.Net.IPAddress listenerIpAddress = new System.Net.IPAddress(new byte[] { 127, 0, 0, 1});
        public string ListenerIpAddress {
            get { return this.listenerIpAddress.ToString(); }
            set { this.listenerIpAddress = System.Net.IPAddress.Parse(value); }
        }

        private int listenerPort = 1337;
        public string ListenerPort { 
            get { return this.listenerPort.ToString(); }
            set { this.listenerPort = int.Parse(value); }
        }

        private bool useInternalTestFeed = false;
        public bool UseInternalTestFeed {
            get { return this.useInternalTestFeed; }
            set { this.useInternalTestFeed = value; }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            var mainWindow = new MainWindow(this.listenerIpAddress, this.listenerPort, this.useInternalTestFeed);
            mainWindow.Show();
            this.Close();
        }
                



    }
}
