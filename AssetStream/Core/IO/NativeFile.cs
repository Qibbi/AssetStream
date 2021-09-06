using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Core.IO
{
    public static class NativeFile
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DirectoryCreate(string path)
        {
            Directory.CreateDirectory(path);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FileExits(string path)
        {
            return File.Exists(path);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FileDelete(string path)
        {
            File.Delete(path);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FileMove(string sourcePath, string destinationPath)
        {
            File.Move(sourcePath, destinationPath);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long FileSize(string path)
        {
            return new FileInfo(path).Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime GetLastWriteTime(string path)
        {
            return new FileInfo(path).LastWriteTime;
        }
    }
}
