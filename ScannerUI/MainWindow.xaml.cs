using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ScannerCore;

namespace ScannerUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent(); 
            _refresher = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(333)};
            _refresher.Tick += (sender, args) => Progress.Text = _scanner.Progress.ToString("F");
        }

        private DriveScanner _scanner;
        private readonly DispatcherTimer _refresher;
        private async void SetDrive(object sender, RoutedEventArgs e)
        {
            var info = (DriveInfo) ((Button) (sender)).DataContext;
            _scanner = new DriveScanner();

            Progress.Text = "0";

            Ready.Visibility = Visibility.Hidden;
            Processing.Visibility = Visibility.Visible;
            ItemsControl.IsEnabled = false;
           
            _refresher.Start();

            await Task.Run(() =>
            {
                App.Current.Context.StatisticsRoot = _scanner.ScanDrive(info.Name.Substring(0, 2));
                App.Current.Context.Problematic.Clear();
                App.Current.Context.Problematic.AddRange(_scanner.Inaccessible);
            });

            _refresher.Stop();

            ItemsControl.IsEnabled = true;
            Processing.Visibility = Visibility.Hidden;
            Ready.Visibility = Visibility.Visible;
        }
    }
}
