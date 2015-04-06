using System.Windows;

namespace ScannerUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static new App Current { get { return (App)Application.Current; } }

        public Global Context { get { return (Global) Resources["DataGlobal"]; } }
    }
}
