using System;
using System.Collections.Generic;
using System.IO;

namespace ScannerCore
{
    public class DriveScanner
    {
        private long _total, _occupied;
        private readonly List<string> _problematic = new List<string>();

        public Single Progress
        {
            get { return _occupied == 0 ? 0 : _total*(Single) 100/_occupied; }
        }

        public string[] Inaccessible
        {
            get { return _problematic.ToArray(); }
        }

        public long GetDisplayThreshold(Single percent, bool includeFreeSpace)
        {
            return (long) (percent*(includeFreeSpace ? _total : _occupied));
        }

        public FsItem Scan(string driveName) //C:
        {
            _total = 0;
            _problematic.Clear();

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
            var pp = parentPath + item.Name + "\\";
            item.Items = DirectoryScanner.Scan(pp, ref _total);
            if (item.Items == null)
            {
                _problematic.Add(pp);
                return; //Access to directory denied
            }
            for (var i = item.Items.Count - 1; i >= (parentPath == null ? 0 : 2); i--)
            {
                var child = item.Items[i];
                if (child.IsDir) ScanChildren(child, pp);
                item.Size += child.Size;
            }
            if (parentPath != null) //in case not drive root
                item.Items.RemoveRange(0, 2); //removing "." & ".."
        }

    }
}
