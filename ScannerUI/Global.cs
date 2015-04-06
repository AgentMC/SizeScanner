using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ScannerCore;

namespace ScannerUI
{
    public class Global : INotifyPropertyChanged
    {
        public Global()
        {
            Drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToArray();
            Problematic = new List<string>();
        }

        private FsItem _root;
        public FsItem StatisticsRoot {
            get
            {
                return _root;
            }
            set
            {
                _root = value;
                OnPropertyChanged();
            }
        }

        public List<string> Problematic { get; private set; }

        public DriveInfo[] Drives { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
