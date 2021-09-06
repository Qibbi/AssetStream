using System;
using System.IO;

namespace Core.IO
{
    public abstract class AVirtualFileProviderBase : IVirtualFileProvider
    {
        public string RootPath { get; init; }

        protected AVirtualFileProviderBase(string rootPath)
        {
            RootPath = rootPath;
            if (RootPath is not null)
            {
                if (RootPath == string.Empty)
                {
                    throw new ArgumentException(null, nameof(rootPath));
                }

                if (RootPath[^1] != VirtualFileSystem.DirectorySeparatorChar)
                {
                    RootPath += VirtualFileSystem.DirectorySeparatorChar;
                }
            }
            VirtualFileSystem.RegisterProvider(this);
        }

        protected virtual string ResolvePath(string url)
        {
            return url;
        }

        public virtual string GetAbsolutePath(string url)
        {
            throw new NotImplementedException();
        }

        public virtual bool TryGetFileLocation(string url, out string filePath, out long fileStart, out long fileEnd)
        {
            filePath = null;
            fileStart = 0L;
            fileEnd = 0L;
            return false;
        }

        public abstract Stream OpenStream(string url, FileMode mode, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read);

        public virtual string[] ListFiles(string url, string searchPattern, SearchOption searchOption)
        {
            throw new NotImplementedException();
        }

        public virtual void CreateDirectory(string url)
        {
            throw new NotImplementedException();
        }

        public virtual bool DirectoryExists(string url)
        {
            throw new NotImplementedException();
        }

        public virtual bool FileExists(string url)
        {
            throw new NotImplementedException();
        }

        public virtual void FileDelete(string url)
        {
            throw new NotImplementedException();
        }

        public virtual void FileMove(string sourceUrl, string destinationUrl)
        {
            throw new NotImplementedException();
        }

        public virtual void FileMove(string sourceUrl, IVirtualFileProvider destinationProvider, string destinationUrl)
        {
            throw new NotImplementedException();
        }

        public virtual long FileSize(string url)
        {
            throw new NotImplementedException();
        }

        public virtual DateTime GetLastWriteTime(string url)
        {
            throw new NotImplementedException();
        }

        public virtual void Dispose()
        {
            VirtualFileSystem.UnregisterProvider(this);
            GC.SuppressFinalize(this);
        }

        public override string ToString()
        {
            return RootPath;
        }
    }
}
