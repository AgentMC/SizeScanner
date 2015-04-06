using System.Collections.Generic;
using System.Diagnostics;

namespace ScannerCore
{
    [DebuggerDisplay("Dir:{IsDir}, {Name}, {Size} bytes")]
    public class FsItem
    {
        public FsItem(string name, long size, bool isDir)
        {
            Name = name;
            Size = size;
            IsDir = isDir;
        }

        public string Name { get; private set; }
        public long Size { get; set; }
        public bool IsDir { get; private set; }

        public List<FsItem> Items { get; set; }
    }
}