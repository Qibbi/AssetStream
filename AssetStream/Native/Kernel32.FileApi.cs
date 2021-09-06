using System;
using System.Runtime.InteropServices;

namespace Native
{
    internal static partial class Kernel32
    {
        public const int MAX_PATH = 260;

        public enum FileCreationDispositionType : uint
        {
            /// <summary>
            /// Creates a new file, only if it does not already exist.
            /// </summary>
            CreateNew = 1,
            /// <summary>
            /// Creates a new file, always.
            /// </summary>
            CreateAlways,
            /// <summary>
            /// Opens a file or device, only if it exists.
            /// </summary>
            OpenExisting,
            /// <summary>
            /// Opens a file, always.
            /// </summary>
            OpenAlways,
            /// <summary>
            /// Opens a file and truncates it so that its size is zero bytes, only if it exists.
            /// </summary>
            TruncateExisting
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FileTime
        {
            public uint DwLowDateTime;
            public uint DwHighDateTime;

            public long DateTime => ((long)DwHighDateTime << 32) | DwLowDateTime;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Win32FindDataA
        {
            public int DwFileAttributes;
            public FileTime FtCreationTime;
            public FileTime FtLastAccessTime;
            public FileTime FtLastWriteTime;
            public uint NFileSizeHigh;
            public uint NFileSizeLow;
            public int DwReserved0;
            public int DwReserved1;
            public unsafe fixed byte CFileName[MAX_PATH];
            public unsafe fixed byte CAlternateFileName[14];
            public int DwFileType;
            public int DwCreatorType;
            public short WFinderFlags;

            public DateTime CreationTime => DateTime.FromFileTime(FtCreationTime.DateTime);
            public DateTime LastAccessTime => DateTime.FromFileTime(FtLastAccessTime.DateTime);
            public DateTime LastWriteTime => DateTime.FromFileTime(FtLastWriteTime.DateTime);
            public long FileSize => ((long)NFileSizeHigh << 32) | NFileSizeLow;
            public string FileName => GetFileName();
            public string AlternateFileName => GetAlternateFileName();

            private unsafe string GetFileName()
            {
                fixed (byte* pFileName = CFileName)
                {
                    return Marshal.PtrToStringAnsi((IntPtr)pFileName);
                }
            }

            private unsafe string GetAlternateFileName()
            {
                fixed (byte* pAlternateFileName = CAlternateFileName)
                {
                    return Marshal.PtrToStringAnsi((IntPtr)pAlternateFileName);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Win32FindDataW
        {
            public int DwFileAttributes;
            public FileTime FtCreationTime;
            public FileTime FtLastAccessTime;
            public FileTime FtLastWriteTime;
            public uint NFileSizeHigh;
            public uint NFileSizeLow;
            public int DwReserved0;
            public int DwReserved1;
            public unsafe fixed char CFileName[MAX_PATH];
            public unsafe fixed char CAlternateFileName[14];
            public int DwFileType;
            public int DwCreatorType;
            public short WFinderFlags;

            public DateTime CreationTime => DateTime.FromFileTime(FtCreationTime.DateTime);
            public DateTime LastAccessTime => DateTime.FromFileTime(FtLastAccessTime.DateTime);
            public DateTime LastWriteTime => DateTime.FromFileTime(FtLastWriteTime.DateTime);
            public long FileSize => ((long)NFileSizeHigh << 32) | NFileSizeLow;
            public string FileName => GetFileName();
            public string AlternateFileName => GetAlternateFileName();

            private unsafe string GetFileName()
            {
                fixed (char* pFileName = CFileName)
                {
                    return Marshal.PtrToStringUni((IntPtr)pFileName);
                }
            }

            private unsafe string GetAlternateFileName()
            {
                fixed (char* pAlternateFileName = CAlternateFileName)
                {
                    return Marshal.PtrToStringUni((IntPtr)pAlternateFileName);
                }
            }
        }

        public delegate IntPtr CreateFileADelegate([In, MarshalAs(UnmanagedType.LPStr)] string lpFileName,
                                                   int dwDesiredAccess,
                                                   int dwSharedMode,
                                                   IntPtr lpSecurityAttributes,
                                                   FileCreationDispositionType dwCreationDisposition,
                                                   int dwFlagsAndAttributes,
                                                   IntPtr hTemplateFile);
        public delegate IntPtr CreateFileWDelegate([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
                                                   int dwDesiredAccess,
                                                   int dwSharedMode,
                                                   IntPtr lpSecurityAttributes,
                                                   FileCreationDispositionType dwCreationDisposition,
                                                   int dwFlagsAndAttributes,
                                                   IntPtr hTemplateFile);
        public delegate IntPtr FindFirstFileADelegate([In, MarshalAs(UnmanagedType.LPStr)] string lpFileName, ref Win32FindDataA lpFindFileData);
        public delegate IntPtr FindFirstFileWDelegate([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName, ref Win32FindDataW lpFindFileData);
        public delegate bool FindCloseDelegate(IntPtr hFindFile);

        public static readonly CreateFileADelegate CreateFileA;
        public static readonly CreateFileWDelegate CreateFileW;
        public static readonly FindFirstFileADelegate FindFirstFileA;
        public static readonly FindFirstFileWDelegate FindFirstFileW;
        public static readonly FindCloseDelegate FindClose;
    }
}
