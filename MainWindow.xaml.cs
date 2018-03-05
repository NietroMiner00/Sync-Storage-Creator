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

            if (!Settings.Default.remDir.Equals(""))
            {
                remDir.Items.Add(Settings.Default.remDir);
                remDir.SelectedIndex = 0;
            }
        }

        private void start_Click(object sender, RoutedEventArgs e)
        {
            if (remDir.SelectedItem != null)
            {
                ContentProvider.init(remDir.Text, dir.Text);
                Settings.Default.remDir = remDir.SelectedItem.ToString();
                Settings.Default.Save();
            }
            else Console.WriteLine("Error: Select a folder!");
        }

        private void LoadFolders(object sender, RoutedEventArgs e)
        {
            string sel = (string)remDir.SelectedItem;
            remDir.Items.Clear();
            string[] paths = ContentProvider.load();
            if (paths != null)
            {
                remDir.SelectedItem = sel;
                foreach (string i in paths)
                {
                    remDir.Items.Add(i);
                }
            }
        }

        private void reset_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Reset();
            remDir.Items.Clear();
        }
    }
}
