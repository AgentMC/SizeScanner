using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ScannerCore
{
    public class DirectoryScanner
    {
        private const int FileDirectoryInformation = 1;
        private const uint StatusSuccess = 0x00000000;
        private const uint StatusNoMoreFiles = 0x80000006;

        #region Native

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        // ReSharper disable InconsistentNaming
        internal struct LARGE_INTEGER
        {
            [FieldOffset(0)]
            internal Int64 QuadPart;
            [FieldOffset(0)]
            internal Int32 LowPart;
            [FieldOffset(4)]
            internal UInt32 HighPart;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct IO_STATUS_BLOCK_UNION
        {
            [FieldOffset(0)]
            internal UInt32 Status;
            [FieldOffset(0)]
            internal IntPtr Pointer;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal class IO_STATUS_BLOCK
        {
            internal IO_STATUS_BLOCK_UNION Union;
            internal UIntPtr Information;
        }

        const Int32 FDI_FileName_FieldSize = 2;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        internal class FILE_DIRECTORY_INFORMATION
        {
            internal UInt32 NextEntryOffset;
            internal UInt32 FileIndex;
            internal LARGE_INTEGER CreationTime;
            internal LARGE_INTEGER LastAccessTime;
            internal LARGE_INTEGER LastWriteTime;
            internal LARGE_INTEGER ChangeTime;
            internal LARGE_INTEGER EndOfFile;
            internal LARGE_INTEGER AllocationSize;
            internal CreateFileOptions FileAttributes;
            internal UInt32 FileNameLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = FDI_FileName_FieldSize)] internal Byte[] FileName;
        }

        readonly int FileNameOffset = Marshal.SizeOf(typeof(FILE_DIRECTORY_INFORMATION)) - FDI_FileName_FieldSize;

        [Flags]
        public enum FileAccessRights : uint
        {
            /// <summary>	For a directory, the right to create a file in the directory.	</summary>
            FILE_ADD_FILE = 2,

            /// <summary>	For a directory, the right to create a subdirectory.	</summary>
            FILE_ADD_SUBDIRECTORY = 4,

            /// <summary>	All possible access rights for a file.	</summary>
            FILE_ALL_ACCESS = 511,

            /// <summary>	For a file object, the right to append data to the file. (For local files, write operations will not overwrite existing data if this flag is specified without FILE_WRITE_DATA.) For a directory object, the right to create a subdirectory (FILE_ADD_SUBDIRECTORY).	</summary>
            FILE_APPEND_DATA = 4,

            /// <summary>	For a named pipe, the right to create a pipe.	</summary>
            FILE_CREATE_PIPE_INSTANCE = 4,

            /// <summary>	For a directory, the right to delete a directory and all the files it contains, including read-only files.	</summary>
            FILE_DELETE_CHILD = 64,

            /// <summary>	For a native code file, the right to execute the file. This access right given to scripts may cause the script to be executable, depending on the script interpreter.	</summary>
            FILE_EXECUTE = 32,

            /// <summary>	For a directory, the right to list the contents of the directory.	</summary>
            FILE_LIST_DIRECTORY = 1,

            /// <summary>	The right to read file attributes.	</summary>
            FILE_READ_ATTRIBUTES = 128,

            /// <summary>	For a file object, the right to read the corresponding file data. For a directory object, the right to read the corresponding directory data.	</summary>
            FILE_READ_DATA = 1,

            /// <summary>	The right to read extended file attributes.	</summary>
            FILE_READ_EA = 8,

            /// <summary>	For a directory, the right to traverse the directory. By default, users are assigned the BYPASS_TRAVERSE_CHECKING privilege, which ignores the FILE_TRAVERSE access right. See the remarks in File Security and Access Rights for more information.	</summary>
            FILE_TRAVERSE = 32,

            /// <summary>	The right to write file attributes.	</summary>
            FILE_WRITE_ATTRIBUTES = 256,

            /// <summary>	For a file object, the right to write data to the file. For a directory object, the right to create a file in the directory (FILE_ADD_FILE).	</summary>
            FILE_WRITE_DATA = 2,

            /// <summary>	The right to write extended file attributes.	</summary>
            FILE_WRITE_EA = 16,


            /// <summary>	The right to delete the object.	</summary>
            DELETE = 0x00010000,

            /// <summary>	The right to read the information in the object's security descriptor, not including the information in the system access control list (SACL).	</summary>
            READ_CONTROL = 0x00020000,

            /// <summary>	The right to modify the discretionary access control list (DACL) in the object's security descriptor.	</summary>
            WRITE_DAC = 0x00040000,

            /// <summary>	The right to change the owner in the object's security descriptor.	</summary>
            WRITE_OWNER = 0x00080000,

            /// <summary>	The right to use the object for synchronization. This enables a thread to wait until the object is in the signaled state. Some object types do not support this access right.	</summary>
            SYNCHRONIZE = 0x00100000,


            /// <summary>	Combines DELETE, READ_CONTROL, WRITE_DAC, WRITE_OWNER, and SYNCHRONIZE access.	</summary>
            STANDARD_RIGHTS_ALL = 0x001F0000,

            /// <summary>	Currently defined to equal READ_CONTROL.	</summary>
            STANDARD_RIGHTS_EXECUTE = READ_CONTROL,

            /// <summary>	Currently defined to equal READ_CONTROL.	</summary>
            STANDARD_RIGHTS_READ = READ_CONTROL,

            /// <summary>	Combines DELETE, READ_CONTROL, WRITE_DAC, and WRITE_OWNER access.	</summary>
            STANDARD_RIGHTS_REQUIRED = 0x000F0000,

            /// <summary>	Currently defined to equal READ_CONTROL.	</summary>
            STANDARD_RIGHTS_WRITE = READ_CONTROL,


            /// <summary>	All possible access rights	</summary>
            GENERIC_ALL = 0x10000000,

            /// <summary>	Execute access	</summary>
            GENERIC_EXECUTE = 0x20000000,

            /// <summary>	Write access	</summary>
            GENERIC_WRITE = 0x40000000,

            /// <summary>	Read access	</summary>
            GENERIC_READ = 0x80000000
        }

        [Flags]
        public enum CreateFileOptions : uint
        {
            /// <summary>
            /// No specific options specified for this request.
            /// </summary>
            FILE_OPTIONS_NOT_SET = 0x0,

            /// <summary>	A file or directory that is an archive file or directory. Applications typically use this
            ///  attribute to mark files for backup or removal . 	</summary>
            FILE_ATTRIBUTE_ARCHIVE = 0x20,

            /// <summary>	A file or directory that is compressed. For a file, all of the data in the file is compressed.
            ///  For a directory, compression is the default for newly created files and subdirectories. 	</summary>
            FILE_ATTRIBUTE_COMPRESSED = 0x800,

            /// <summary>	This value is reserved for system use. 	</summary>
            FILE_ATTRIBUTE_DEVICE = 0x40,

            /// <summary>	The handle that identifies a directory. 	</summary>
            FILE_ATTRIBUTE_DIRECTORY = 0x10,

            /// <summary>	A file or directory that is encrypted. For a file, all data streams in the file are encrypted. 
            /// For a directory, encryption is the default for newly created files and subdirectories. 	</summary>
            FILE_ATTRIBUTE_ENCRYPTED = 0x4000,

            /// <summary>	The file or directory is hidden. It is not included in an ordinary directory listing. 	</summary>
            FILE_ATTRIBUTE_HIDDEN = 0x2,

            /// <summary>	The directory or user data stream is configured with integrity (only supported on ReFS volumes). 
            /// It is not included in an ordinary directory listing. The integrity setting persists with the file if it's 
            /// renamed. If a file is copied the destination file will have integrity set if either the source file or 
            /// destination directory have integrity set. Windows Server 2008 R2, Windows 7, Windows Server 2008, Windows 
            /// Vista, Windows Server 2003, and Windows XP: This flag is not supported until Windows Server 2012.	</summary>
            FILE_ATTRIBUTE_INTEGRITY_STREAM = 0x8000,

            /// <summary>	A file that does not have other attributes set. This attribute is valid only when used alone.</summary>
            FILE_ATTRIBUTE_NORMAL = 0x80,

            /// <summary>	The file or directory is not to be indexed by the content indexing service.</summary>
            FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x2000,

            /// <summary>	The user data stream not to be read by the background data integrity scanner (AKA scrubber). 
            /// When set on a directory it only provides inheritance. This flag is only supported on Storage Spaces and 
            /// ReFS volumes. It is not included in an ordinary directory listing. Windows Server 2008 R2, Windows 7, 
            /// Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP: This flag is not supported until 
            /// Windows 8 and Windows Server 2012.	</summary>
            FILE_ATTRIBUTE_NO_SCRUB_DATA = 0x20000,

            /// <summary>	The data of a file is not available immediately. This attribute indicates that the file data is 
            /// physically moved to offline storage. This attribute is used by Remote Storage, which is the hierarchical 
            /// storage management software. Applications should not arbitrarily change this attribute. 	</summary>
            FILE_ATTRIBUTE_OFFLINE = 0x1000,

            /// <summary>	A file that is read-only. Applications can read the file, but cannot write to it or delete it. 
            /// This attribute is not honored on directories. For more information, see You cannot view or change the Read-only 
            /// or the System attributes of folders in Windows Server 2003, in Windows XP, in Windows Vista or in Windows 7.
            /// </summary>
            FILE_ATTRIBUTE_READONLY = 0x1,

            /// <summary>	A file or directory that has an associated reparse point, or a file that is a symbolic link.</summary>
            FILE_ATTRIBUTE_REPARSE_POINT = 0x400,

            /// <summary>	A file that is a sparse file. 	</summary>
            FILE_ATTRIBUTE_SPARSE_FILE = 0x200,

            /// <summary>	A file or directory that the operating system uses a part of, or uses exclusively. 	</summary>
            FILE_ATTRIBUTE_SYSTEM = 0x4,

            /// <summary>	A file that is being used for temporary storage. File systems avoid writing data back to mass 
            /// storage if sufficient cache memory is available, because typically, an application deletes a temporary file 
            /// after the handle is closed. In that scenario, the system can entirely avoid writing the data. Otherwise, 
            /// the data is written after the handle is closed. 	</summary>
            FILE_ATTRIBUTE_TEMPORARY = 0x100,

            /// <summary>	This value is reserved for system use. 	</summary>
            FILE_ATTRIBUTE_VIRTUAL = 0x10000,

            /// <summary>	The file is being opened or created for a backup or restore operation. The system ensures that 
            /// the calling process overrides file security checks when the process has SE_BACKUP_NAME and SE_RESTORE_NAME 
            /// privileges. For more information, see Changing Privileges in a Token. You must set this flag to obtain a handle 
            /// to a directory. A directory handle can be passed to some functions instead of a file handle. For more information,
            ///  see the Remarks section. 	</summary>
            FILE_FLAG_BACKUP_SEMANTICS = 0x02000000,

            /// <summary>	The file is to be deleted immediately after all of its handles are closed, which includes the 
            /// specified handle and any other open or duplicated handles. If there are existing open handles to a file, the 
            /// call fails unless they were all opened with the FILE_SHARE_DELETE share mode. Subsequent open requests for the 
            /// file fail, unless the FILE_SHARE_DELETE share mode is specified. 	</summary>
            FILE_FLAG_DELETE_ON_CLOSE = 0x04000000,

            /// <summary>	The file or device is being opened with no system caching for data reads and writes. This flag does
            ///  not affect hard disk caching or memory mapped files. There are strict requirements for successfully working with 
            /// files opened with CreateFile using the FILE_FLAG_NO_BUFFERING flag, for details see File Buffering. 	</summary>
            FILE_FLAG_NO_BUFFERING = 0x20000000,

            /// <summary>	The file data is requested, but it should continue to be located in remote storage. It should not be 
            /// transported back to local storage. This flag is for use by remote storage systems. 	</summary>
            FILE_FLAG_OPEN_NO_RECALL = 0x00100000,

            /// <summary>	Normal reparse point processing will not occur; CreateFile will attempt to open the reparse point. 
            /// When a file is opened, a file handle is returned, whether or not the filter that controls the reparse point is 
            /// operational. This flag cannot be used with the CREATE_ALWAYS flag. If the file is not a reparse point, then this 
            /// flag is ignored. For more information, see the Remarks section. 	</summary>
            FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000,

            /// <summary>	The file or device is being opened or created for asynchronous I/O. When subsequent I/O operations 
            /// are completed on this handle, the event specified in the OVERLAPPED structure will be set to the signaled state. 
            /// If this flag is specified, the file can be used for simultaneous read and write operations. If this flag is not 
            /// specified, then I/O operations are serialized, even if the calls to the read and write functions specify an 
            /// OVERLAPPED structure. 	</summary>
            FILE_FLAG_OVERLAPPED = 0x40000000,

            /// <summary>	Access will occur according to POSIX rules. This includes allowing multiple files with names, 
            /// differing only in case, for file systems that support that naming. Use care when using this option, because 
            /// files created with this flag may not be accessible by applications that are written for MS-DOS or 16-bit Windows. 	
            /// </summary>
            FILE_FLAG_POSIX_SEMANTICS = 0x0100000,

            /// <summary>	Access is intended to be random. The system can use this as a hint to optimize file caching. This 
            /// flag has no effect if the file system does not support cached I/O and FILE_FLAG_NO_BUFFERING. For more information, 
            /// see the Caching Behavior section of this topic. 	</summary>
            FILE_FLAG_RANDOM_ACCESS = 0x10000000,

            /// <summary>	The file or device is being opened with session awareness. If this flag is not specified, then
            ///  per-session devices (such as a redirected USB device) cannot be opened by processes running in session 0. 
            /// This flag has no effect for callers not in session 0. This flag is supported only on server editions of Windows. 
            /// Windows Server 2008 R2, Windows Server 2008, and Windows Server 2003: This flag is not supported before Windows 
            /// Server 2012. 	</summary>
            FILE_FLAG_SESSION_AWARE = 0x00800000,

            /// <summary>	Access is intended to be sequential from beginning to end. The system can use this as a hint to 
            /// optimize file caching. This flag should not be used if read-behind (that is, reverse scans) will be used. This 
            /// flag has no effect if the file system does not support cached I/O and FILE_FLAG_NO_BUFFERING. For more information, 
            /// see the Caching Behavior section of this topic. 	</summary>
            FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000,

            /// <summary>	Write operations will not go through any intermediate cache, they will go directly to disk. For 
            /// additional information, see the Caching Behavior section of this topic. 	</summary>
            FILE_FLAG_WRITE_THROUGH = 0x80000000
        }

        internal static class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern SafeFileHandle CreateFile(
                [MarshalAs(UnmanagedType.LPTStr)] string filename,
                [MarshalAs(UnmanagedType.U4)] FileAccessRights access,
                [MarshalAs(UnmanagedType.U4)] FileShare share,
                IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
                [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                [MarshalAs(UnmanagedType.U4)] CreateFileOptions flagsAndAttributes,
                IntPtr templateFile);

            [DllImport("ntdll.dll")]
            internal static extern uint NtQueryDirectoryFile(
                SafeFileHandle FileHandle,
                IntPtr Event,
                IntPtr ApcRoutine,
                IntPtr ApcContext,
                [Out] IO_STATUS_BLOCK IoStatusBlock,
                [Out] IntPtr FileInformation,
                UInt32 Length,
                UInt32 FileInformationClass,
                [MarshalAs(UnmanagedType.Bool)] Boolean ReturnSingleEntry,
                IntPtr FileName,
                [MarshalAs(UnmanagedType.Bool)] Boolean RestartScan
                );
        }

        #endregion

        private readonly IntPtr buffer = Marshal.AllocHGlobal(1024 * 1024);
        private readonly bool PreferAllocatedSize;

        public DirectoryScanner(bool preferAllocatedSize)
        {
            PreferAllocatedSize = preferAllocatedSize;
        }

        public List<FsItem> Scan(string dir, ref long processed)
        {

            var hFolder = NativeMethods.CreateFile(dir,
                                                   FileAccessRights.FILE_LIST_DIRECTORY,
                                                   FileShare.ReadWrite | FileShare.Delete,
                                                   IntPtr.Zero,
                                                   FileMode.Open,
                                                   CreateFileOptions.FILE_FLAG_BACKUP_SEMANTICS,
                                                   IntPtr.Zero);
            if (hFolder.IsInvalid)
            {
                return null;
            }

            var res = new List<FsItem>();

            bool moreFiles = true;
            var statusBlock = new IO_STATUS_BLOCK();
            while (moreFiles)
            {
                var ntstatus = NativeMethods.NtQueryDirectoryFile(
                    hFolder,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    statusBlock,
                    buffer,
                    1024*1024,
                    FileDirectoryInformation,
                    false,
                    IntPtr.Zero,
                    false);

                switch (ntstatus)
                {
                    case StatusNoMoreFiles:
                        moreFiles = false;
                        break;
                    case StatusSuccess:
                        CheckData(buffer, res, ref processed);
                        break;
                    default:
                        moreFiles = false;
                        Debug.WriteLine(new Win32Exception().Message);
                        break;
                }
            }
            hFolder.Close();

            return res;
        }

        private void CheckData(IntPtr dataPtr, List<FsItem> items, ref long processed)
        {
            var info = new FILE_DIRECTORY_INFORMATION();
            do
            {
                Marshal.PtrToStructure(dataPtr, info);
                if ((info.FileAttributes & CreateFileOptions.FILE_ATTRIBUTE_REPARSE_POINT) == 0 //not symlink
                    || (info.FileAttributes & CreateFileOptions.FILE_ATTRIBUTE_OFFLINE) != 0) //or symlink to offline file
                {
                    var name = Marshal.PtrToStringUni(dataPtr + FileNameOffset, (int) info.FileNameLength/2);
                    var isDir = (info.FileAttributes & CreateFileOptions.FILE_ATTRIBUTE_DIRECTORY) > 0;
                    if (!(isDir && ((name.Length == 1 && name[0] == '.') || (name.Length == 2 && name[0] == '.' && name[1] == '.')))) //not "." or ".." pseudo-directories
                    {
                        items.Add(new FsItem(name, PreferAllocatedSize ? info.AllocationSize.QuadPart : info.EndOfFile.QuadPart, isDir, info.LastWriteTime.QuadPart));
                        processed += info.AllocationSize.QuadPart;
                    }
                }
                dataPtr += (int) info.NextEntryOffset;
            } while (info.NextEntryOffset != 0);
        }
    }
}
