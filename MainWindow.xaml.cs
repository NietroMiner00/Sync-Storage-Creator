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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Dropbox.Api;
using Sync_Storage_Creator_Windows.Properties;

namespace Sync_Storage_Creator_Windows
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            string path = System.IO.Directory.GetCurrentDirectory();
            dir.Text = path.Remove(path.LastIndexOf('\\'));

            
        }

        private void start_Click(object sender, RoutedEventArgs e)
        {
            ContentProvider.init(remDir.Text, dir.Text);
            Settings.Default.remDir = remDir.Text;
        }

        private void Sync_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
