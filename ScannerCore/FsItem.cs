using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ScannerCore
{
    [DebuggerDisplay("Dir:{IsDir}, {Name}, {Size} bytes")]
    public class FsItem
    {
        public FsItem(string name, long size, bool isDir, long lastModified) : this(name, size, isDir, DateTime.FromFileTime(lastModified)) { }

        public FsItem(string name, long size, bool isDir, DateTime lastModified = default)
        {
            Name = name;
            Size = size;
            IsDir = isDir;
            if (lastModified == default) lastModified = DateTime.Now;
            LastModified = lastModified;
        }

        public string Name { get; private set; }
        public long Size { get; set; }
        public bool IsDir { get; private set; }
        public DateTime LastModified { get; private set; }

        public List<FsItem> Items { get; set; }
    }
}