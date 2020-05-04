using System;
using System.Collections.Generic;
using System.IO;

namespace ScannerCore
{
    public class DriveScanner
    {
        private long _total, _occupied;
        private readonly List<string> _problematic = new List<string>();

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

        public FsItem Scan(string driveName) //C:
        {
            _total = 0;
            _problematic.Clear();
            CurrentTarget = driveName;

            var drive = new DriveInfo(driveName);
            _occupied = drive.TotalSize - drive.TotalFreeSpace;

            var root = new FsItem(driveName, 0, true);
            ScanChildren(root, null);
            root.Items.InsertRange(0, new[]
            {
                new FsItem("[Free space]", drive.TotalFreeSpace, false),
                new FsItem("[Inaccessible]", Math.Max(0, _occupied - _total), false)
            });
            return root;
        }

        private void ScanChildren(FsItem item, string parentPath)
        {
            var scanObject = parentPath + item.Name + "\\";
            CurrentScanned = scanObject;
            item.Items = DirectoryScanner.Scan(scanObject, ref _total);
            if (item.Items == null)
            {
                _problematic.Add(scanObject);
                return; //Access to directory denied
            }
            for (var i = item.Items.Count - 1; i >= 0; i--)
            {
                var child = item.Items[i];
                if (child.IsDir) ScanChildren(child, scanObject);
                item.Size += child.Size;
            }
        }
    }
}
