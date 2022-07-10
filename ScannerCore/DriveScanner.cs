using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ScannerCore
{
    public class DriveScanner
    {
        private long _total, _occupied;
        private readonly List<string> _problematic = new List<string>();
        private DirectoryScanner _scanner;

        public float Progress
        {
            get { return _occupied == 0 ? 0 : _total*(float) 100/_occupied; }
        }

        public string[] Inaccessible
        {
            get { return _problematic.ToArray(); }
        }

        public string CurrentTarget { get; private set; }

        public string CurrentScanned { get; private set; }

        public long GetDisplayThreshold(float percent, bool includeFreeSpace)
        {
            return (long) (percent*(includeFreeSpace ? _total : _occupied));
        }

        public FsItem ScanDrive(string driveName) //C:
        {
            var drive = new DriveInfo(driveName);
            _occupied = drive.TotalSize - drive.TotalFreeSpace;

            var root = ScanUnitInternal(driveName, true, CancellationToken.None);
            root.Items.InsertRange(0, new[]
            {
                new FsItem("[Free space]", drive.TotalFreeSpace, false),
                new FsItem("[Inaccessible]", Math.Max(0, _occupied - _total), false)
            });
            return root;
        }

        public FsItem ScanDirectory(string path, CancellationToken run) => ScanUnitInternal(path, false, run);

        private FsItem ScanUnitInternal(string location, bool useAllocationSize, CancellationToken run)
        {
            _total = 0;
            _problematic.Clear();
            CurrentTarget = location;
            var root = new FsItem(location, 0, true);
            _scanner = new DirectoryScanner(useAllocationSize);
            ScanChildren(root, run);
            return root;
        }

        private void ScanChildren(FsItem item, CancellationToken run) => ScanChildren(item, null, run);

        private void ScanChildren(FsItem item, string parentPath, CancellationToken run)
        {
            if (run.IsCancellationRequested) return;

            var scanObject = parentPath + item.Name;
            if (scanObject[scanObject.Length-1] != Path.DirectorySeparatorChar) scanObject += Path.DirectorySeparatorChar;

            CurrentScanned = scanObject;
            item.Items = _scanner.Scan(scanObject, ref _total);
            if (item.Items == null)
            {
                _problematic.Add(scanObject);
                return; //Access to directory denied
            }
            for (var i = item.Items.Count - 1; i >= 0; i--)
            {
                var child = item.Items[i];
                if (child.IsDir) ScanChildren(child, scanObject, run);
                item.Size += child.Size;
            }
        }
    }
}
