using System;
using System.IO;

namespace Core.IO
{
    public interface IVirtualFileProvider : IDisposable
    {
        string RootPath { get; }

        string GetAbsolutePath(string url);

        bool TryGetFileLocation(string url, out string filePath, out long fileStart, out long fileEnd);

        Stream OpenStream(string url, FileMode mode, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read);

        string[] ListFiles(string url, string searchPattern, SearchOption searchOption);

        void CreateDirectory(string url);

        bool DirectoryExists(string url);

        bool FileExists(string url);

        void FileDelete(string url);

        void FileMove(string sourceUrl, string destinationUrl);

        void FileMove(string sourceUrl, IVirtualFileProvider destinationProvider, string destinationUrl);

        long FileSize(string url);

        DateTime GetLastWriteTime(string url);
    }
}
