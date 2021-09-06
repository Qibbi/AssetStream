using System;
using System.Runtime.InteropServices;

namespace Native
{
    internal static partial class Kernel32
    {
        [Flags]
        public enum SecurityDescriptorControlType : ushort
        {
            /// <summary>
            /// Indicated that the SID of the owner of the security descriptor was provided by a default mechanism.
            /// This flag can be used by a resource manager to identify objects whose owner was set by a default mechanism.
            /// To set this flag, use the SetSecurityDescriptorOwner function.
            /// </summary>
            OwnerDefaulted = 1 << 0,
            /// <summary>
            /// Indicated that the SID of the security descriptor group was provided by a default mechanism.
            /// This flag can be used by a resource manager to identify objects whose security descriptor group was set by a default mechanism.
            /// To set this flag, use the SetSecurityDescriptorOwner function.
            /// </summary>
            GroupDefaulted = 1 << 1,
            DACLPresent = 1 << 2,
            DACLDefaulted = 1 << 3,
            SACLPresent = 1 << 4,
            SACLDefaulted = 1 << 5,
            DACLInheritReq = 1 << 8,
            SACLInheritReq = 1 << 9,
            DACLAutoInherited = 1 << 10,
            SACLAutoInherited = 1 << 11,
            DACLProtected = 1 << 12,
            SACLProtected = 1 << 13,
            RMControlValid = 1 << 14,
            SelfRelative = 1 << 15
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SecurityDescriptor
        {
            public byte Revision;
            public byte Sbz1;
            public SecurityDescriptorControlType Control;
            public IntPtr Owner;
            public IntPtr Group;
            public IntPtr Sacl;
            public IntPtr Dacl;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SecurityAttributes
        {
            public int NLength;
            public IntPtr LpSecurityDescriptor;
            [MarshalAs(UnmanagedType.Bool)] public bool BInheritHandle;
        }


        private const string _moduleName = "kernel32.dll";

        public static readonly IntPtr HModule;

        static Kernel32()
        {
            HModule = NativeLibrary.Load(_moduleName);
            // FileApi
            CreateFileA = Marshal.GetDelegateForFunctionPointer<CreateFileADelegate>(NativeLibrary.GetExport(HModule, nameof(CreateFileA)));
            CreateFileW = Marshal.GetDelegateForFunctionPointer<CreateFileWDelegate>(NativeLibrary.GetExport(HModule, nameof(CreateFileW)));
            FindFirstFileA = Marshal.GetDelegateForFunctionPointer<FindFirstFileADelegate>(NativeLibrary.GetExport(HModule, nameof(FindFirstFileA)));
            FindFirstFileW = Marshal.GetDelegateForFunctionPointer<FindFirstFileWDelegate>(NativeLibrary.GetExport(HModule, nameof(FindFirstFileW)));
            FindClose = Marshal.GetDelegateForFunctionPointer<FindCloseDelegate>(NativeLibrary.GetExport(HModule, nameof(FindClose)));
            // WinBase
            LStrLenA = Marshal.GetDelegateForFunctionPointer<LStrLenADelegate>(NativeLibrary.GetExport(HModule, "lstrlenA"));
            LStrLen2A = Marshal.GetDelegateForFunctionPointer<LStrLen2ADelegate>(NativeLibrary.GetExport(HModule, "lstrlenA"));
            SetCurrentDirectoryW = Marshal.GetDelegateForFunctionPointer<SetCurrentDirectoryWDelegate>(NativeLibrary.GetExport(HModule, nameof(SetCurrentDirectoryW)));
            GetCurrentDirectoryW = Marshal.GetDelegateForFunctionPointer<GetCurrentDirectoryWDelegate>(NativeLibrary.GetExport(HModule, nameof(GetCurrentDirectoryW)));
        }

        [DllImport(_moduleName, EntryPoint = "LoadLibraryA")] internal static extern IntPtr Load(string lpLibFileName);
        [DllImport(_moduleName, EntryPoint = "GetProcAddress")] internal static extern IntPtr GetExport(IntPtr hModule, string lpProcName);
        [DllImport(_moduleName, EntryPoint = "FreeLibrary")] internal static extern bool Close(IntPtr hLibModule);
    }
}
