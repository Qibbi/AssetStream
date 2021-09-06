using System;
using System.Runtime.InteropServices;

namespace Native
{
    internal static class MsVcRt
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr MemSetDelegate(IntPtr ptr, byte value, nint count);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int MemICmpDelegate([In] IntPtr buf1, [In] IntPtr buf2, nint size);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr MemCpyDelegate(IntPtr dest, [In] IntPtr src, nint count);
        [UnmanagedFunctionPointer(CallingConvention.FastCall)]
        public delegate nint StrLenDelegate([In] IntPtr str);

        private const string _moduleName = "msvcrt.dll";

        private static readonly IntPtr _hModule;

        public static readonly MemSetDelegate MemSet;
        public static readonly MemICmpDelegate MemICmp;
        public static readonly MemCpyDelegate MemCpy;

        static MsVcRt()
        {
            _hModule = NativeLibrary.Load(_moduleName);
            MemSet = Marshal.GetDelegateForFunctionPointer<MemSetDelegate>(NativeLibrary.GetExport(_hModule, "memset"));
            MemICmp = Marshal.GetDelegateForFunctionPointer<MemICmpDelegate>(NativeLibrary.GetExport(_hModule, "_memicmp"));
            MemCpy = Marshal.GetDelegateForFunctionPointer<MemCpyDelegate>(NativeLibrary.GetExport(_hModule, "memcpy"));
        }
    }
}
