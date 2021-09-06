using System;
using System.IO;

namespace Core.IO
{
    public partial class FileSystemProvider : AVirtualFileProviderBase
    {
        public static readonly char VolumeSeparatorChar = Path.VolumeSeparatorChar;
        public static readonly char DirectorySeparatorChar = Path.DirectorySeparatorChar;
        public static readonly char AltDirectorySeparatorChar = Path.AltDirectorySeparatorChar;

        private string _localBasePath;

        public FileSystemProvider(string rootPath, string localBasePath) : base(rootPath)
        {
            ChangeBasePath(localBasePath);
        }

        protected virtual string ConvertUrlToFullPath(string url)
        {
            return _localBasePath is null
                ? url.Replace(VirtualFileSystem.DirectorySeparatorChar, DirectorySeparatorChar)
                : _localBasePath + url.Replace(VirtualFileSystem.DirectorySeparatorChar, DirectorySeparatorChar);
        }

        protected virtual string ConvertFullPathToUrl(string path)
        {
            return _localBasePath is null
                ? path.Replace(DirectorySeparatorChar, VirtualFileSystem.DirectorySeparatorChar)
                : !path.StartsWith(_localBasePath, StringComparison.OrdinalIgnoreCase)
                ? throw new InvalidOperationException($"Path '{path}' does not belong to this file provider.")
                : path[_localBasePath.Length..].Replace(DirectorySeparatorChar, VirtualFileSystem.DirectorySeparatorChar);
        }

        public void ChangeBasePath(string path)
        {
            _localBasePath = path;
            if (_localBasePath is not null)
            {
                _localBasePath = _localBasePath.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar);
                if (!_localBasePath.EndsWith(DirectorySeparatorChar))
                {
                    _localBasePath += DirectorySeparatorChar;
                }
            }
        }

        public override void CreateDirectory(string url)
        {
            string path = ConvertUrlToFullPath(url);
            try
            {
                NativeFile.DirectoryCreate(path);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not create directory '{path}'.", ex);
            }
        }

        public override bool DirectoryExists(string url)
        {
            return NativeFile.DirectoryExists(ConvertUrlToFullPath(url));
        }

        public override bool FileExists(string url)
        {
            return NativeFile.FileExits(ConvertUrlToFullPath(url));
        }

        public override void FileDelete(string url)
        {
            NativeFile.FileDelete(ConvertUrlToFullPath(url));
        }

        public override void FileMove(string sourceUrl, string destinationUrl)
        {
            NativeFile.FileMove(ConvertUrlToFullPath(sourceUrl), ConvertUrlToFullPath(destinationUrl));
        }

        public override void FileMove(string sourceUrl, IVirtualFileProvider destinationProvider, string destinationUrl)
        {
            if (destinationProvider is FileSystemProvider provider)
            {
                provider.CreateDirectory(destinationUrl.Substring(0, destinationUrl.LastIndexOf(VirtualFileSystem.DirectorySeparatorChar)));
                NativeFile.FileMove(ConvertUrlToFullPath(sourceUrl), provider.ConvertUrlToFullPath(destinationUrl));
            }
            else
            {
                using (Stream source = OpenStream(sourceUrl, FileMode.Open),
                       destination = destinationProvider.OpenStream(destinationUrl, FileMode.CreateNew, FileAccess.Write))
                {
                    source.CopyTo(destination);
                }
                NativeFile.FileDelete(sourceUrl);
            }
        }

        public override long FileSize(string url)
        {
            return NativeFile.FileSize(ConvertUrlToFullPath(url));
        }
    }
}
