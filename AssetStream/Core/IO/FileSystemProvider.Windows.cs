using System;
using System.IO;
using System.Linq;

namespace Core.IO
{
    public partial class FileSystemProvider
    {
        public override string GetAbsolutePath(string url)
        {
            return ConvertUrlToFullPath(url);
        }

        public override bool TryGetFileLocation(string url, out string filePath, out long fileStart, out long fileEnd)
        {
            filePath = ConvertUrlToFullPath(url);
            fileStart = 0L;
            fileEnd = -1L;
            return true;
        }

        public override Stream OpenStream(string url, FileMode mode, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read)
        {
            return _localBasePath is not null && url.Split(VirtualFileSystem.AllDirectorySeparatorChars).Contains("..")
                ? throw new InvalidOperationException("Unable to process relative path without a base.")
                : new FileStream(ConvertUrlToFullPath(url), (FileMode)mode, (FileAccess)access, (FileShare)share);
        }

        public override string[] ListFiles(string url, string searchPattern, SearchOption searchOption)
        {
            return DirectoryExists(url)
                ? Directory.GetFiles(ConvertUrlToFullPath(url), searchPattern, (SearchOption)searchOption).Select(ConvertFullPathToUrl).ToArray()
                : Array.Empty<string>();
        }

        public override DateTime GetLastWriteTime(string url)
        {
            return NativeFile.GetLastWriteTime(ConvertUrlToFullPath(url));
        }
    }
}
