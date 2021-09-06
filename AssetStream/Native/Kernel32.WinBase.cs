using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Native
{
    internal static partial class Kernel32
    {
        public delegate int LStrLenADelegate([In, MarshalAs(UnmanagedType.LPStr)] string lpString);
        public delegate int LStrLen2ADelegate([In] IntPtr lpString);
        public delegate bool SetCurrentDirectoryWDelegate([In, MarshalAs(UnmanagedType.LPWStr)] string lpPathName);
        public delegate int GetCurrentDirectoryWDelegate(int nBufferLength, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpBuffer);

        public static readonly LStrLenADelegate LStrLenA;
        public static readonly LStrLen2ADelegate LStrLen2A;
        public static readonly SetCurrentDirectoryWDelegate SetCurrentDirectoryW;
        public static readonly GetCurrentDirectoryWDelegate GetCurrentDirectoryW;
    }
}
