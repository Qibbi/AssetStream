using System;
using System.IO;
using System.Reflection;

namespace Core.IO
{
    public class EmbeddedResourceFileProvider : AVirtualFileProviderBase
    {
        public static readonly char DirectorySeparatorChar = Path.DirectorySeparatorChar;
        public static readonly char AltDirectorySeparatorChar = Path.AltDirectorySeparatorChar;

        private static readonly char _directorySeparatorChar = '.';

        private Assembly _assembly;
        private string[] _resourceNames;

        public EmbeddedResourceFileProvider(string rootPath, Assembly assembly) : base(rootPath)
        {
            SetAssembly(assembly);
        }

        private void SetAssembly(Assembly assembly)
        {
            _assembly = assembly;
            _resourceNames = _assembly.GetManifestResourceNames();
        }

        protected virtual string ConvertUrlToFullPath(string url)
        {
            return $"{Path.GetFileNameWithoutExtension(_assembly.Location)}.Resources.{url.Replace(VirtualFileSystem.DirectorySeparatorChar, _directorySeparatorChar)}";
        }

        protected virtual string ConvertFullPathToUrl(string path)
        {
            return path[(Path.GetFileNameWithoutExtension(_assembly.Location).Length + 11)..].Replace(_directorySeparatorChar, VirtualFileSystem.DirectorySeparatorChar);
        }

        public override void CreateDirectory(string url)
        {
            throw new NotSupportedException();
        }

        public override bool DirectoryExists(string url)
        {
            if (!url.EndsWith(VirtualFileSystem.DirectorySeparatorChar))
            {
                url += VirtualFileSystem.DirectorySeparatorChar;
            }
            string path = ConvertUrlToFullPath(url);
            foreach (string resourceName in _resourceNames)
            {
                if (resourceName.StartsWith(path, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool FileExists(string url)
        {
            string path = ConvertUrlToFullPath(url);
            foreach (string resourceName in _resourceNames)
            {
                if (resourceName.Equals(path, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        public override void FileDelete(string url)
        {
            throw new NotSupportedException();
        }

        public override void FileMove(string sourceUrl, string destinationUrl)
        {
            throw new NotSupportedException();
        }

        public override void FileMove(string sourceUrl, IVirtualFileProvider destinationProvider, string destinationUrl)
        {
            throw new NotSupportedException();
        }

        public override long FileSize(string url)
        {
            using Stream stream = OpenStream(url, FileMode.Open);
            return stream.Length;
        }

        public override string GetAbsolutePath(string url)
        {
            return _assembly.Location + ':' + ConvertUrlToFullPath(url);
        }

        public override bool TryGetFileLocation(string url, out string filePath, out long fileStart, out long fileEnd)
        {
            throw new NotSupportedException();
        }

        public override Stream OpenStream(string url, FileMode mode, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read)
        {
            return mode != FileMode.Open || access != FileAccess.Read || share != FileShare.Read
                ? throw new NotSupportedException()
                : _assembly.GetManifestResourceStream(ConvertUrlToFullPath(url));
        }

        public override DateTime GetLastWriteTime(string url)
        {
            throw new NotSupportedException();
        }
    }
}
