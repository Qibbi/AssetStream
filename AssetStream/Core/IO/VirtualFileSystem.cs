using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Core.IO
{
    public static class VirtualFileSystem
    {
        public sealed class ResolveProviderResult
        {
            public IVirtualFileProvider Provider;
            public string Path;
        }

        public static readonly char DirectorySeparatorChar = '/';
        public static readonly char AltDirectorySeparatorChar = '\\';
        public static readonly string DirectorySeparatorString = DirectorySeparatorChar.ToString();
        public static readonly string AltDirectorySeparatorString = AltDirectorySeparatorChar.ToString();
        public static readonly char[] AllDirectorySeparatorChars = { DirectorySeparatorChar, AltDirectorySeparatorChar };

        private static readonly Random _random = new(Environment.TickCount);
        private static readonly Dictionary<string, IVirtualFileProvider> _providers = new();

        public static readonly IVirtualFileProvider ApplicationLocal;
        public static readonly IVirtualFileProvider ApplicationEmbedded;
        public static readonly IVirtualFileProvider User;

        public static IEnumerable<IVirtualFileProvider> Providers => _providers.Values;
        public static IEnumerable<string> MountPoints => _providers.Keys;

        static VirtualFileSystem()
        {
            ApplicationLocal = new FileSystemProvider("/local", GetParentFolder(Assembly.GetEntryAssembly().Location));
            ApplicationEmbedded = new EmbeddedResourceFileProvider("/embedded", Assembly.GetEntryAssembly());
            string userFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Globals.UserDataLeafName);
            if (!Directory.Exists(userFolder))
            {
                Directory.CreateDirectory(userFolder);
            }
            User = new FileSystemProvider("/user", userFolder);
        }

        private static int LastIndexOfDirectorySeparator(string path)
        {
            int length = path.Length;
            while (--length >= 0)
            {
                char c = path[length];
                if (c == DirectorySeparatorChar || c == AltDirectorySeparatorChar)
                {
                    return length;
                }
            }
            return -1;
        }

        private static int LastIndexOfDot(string path)
        {
            int length = path.Length;
            while (--length >= 0)
            {
                char c = path[length];
                if (c == '.')
                {
                    return length;
                }
            }
            return -1;
        }

        public static void RegisterProvider(IVirtualFileProvider provider)
        {
            if (provider.RootPath is null)
            {
                return;
            }
            if (_providers.ContainsKey(provider.RootPath))
            {
                throw new InvalidOperationException($"A virtual file provider with the root path '{provider.RootPath}' already exists.");
            }

            _providers.Add(provider.RootPath, provider);
        }

        public static void UnregisterProvider(IVirtualFileProvider provider)
        {
            _providers.Where(x => x.Value == provider).Select(x => _providers.Remove(x.Key));
        }

        public static IVirtualFileProvider MountFileSystem(string mountPoint, string path)
        {
            return new FileSystemProvider(mountPoint, path);
        }

        public static IVirtualFileProvider RemountFileSystem(string mountPoint, string path)
        {
            if (mountPoint[^1] != DirectorySeparatorChar)
            {
                mountPoint += DirectorySeparatorChar;
            }
            KeyValuePair<string, IVirtualFileProvider> provider = _providers.FirstOrDefault(FindProvider);
            if (provider.Value is not null)
            {
                ((FileSystemProvider)provider.Value).ChangeBasePath(path);
                return provider.Value;
            }
            return new FileSystemProvider(mountPoint, path);

            bool FindProvider(KeyValuePair<string, IVirtualFileProvider> keyValuePair)
            {
                return keyValuePair.Key == mountPoint;
            }
        }

        public static ResolveProviderResult ResolveProviderUnsafe(string path, bool isResolvingTop)
        {
            if (path.Contains(AltDirectorySeparatorChar))
            {
                path = path.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar);
            }
            for (int idx = path.Length - 1; idx >= 0; --idx)
            {
                char c = path[idx];
                bool isResolvingTopC = idx == path.Length - 1 && isResolvingTop;
                if (!isResolvingTopC && c != DirectorySeparatorChar)
                {
                    continue;
                }
                string providerPath = isResolvingTopC && c != DirectorySeparatorChar
                    ? new StringBuilder(path.Length + 1).Append(path).Append(DirectorySeparatorChar).ToString()
                    : (idx + 1) == path.Length ? path : path.Substring(0, idx + 1);
                if (_providers.TryGetValue(providerPath, out IVirtualFileProvider provider))
                {
                    if (isResolvingTopC)
                    {
                        path = providerPath;
                    }
                    return new ResolveProviderResult { Provider = provider, Path = path[providerPath.Length..] };
                }
            }
            return new ResolveProviderResult();
        }

        public static ResolveProviderResult ResolveProvider(string path, bool isResolvingTop)
        {
            ResolveProviderResult result = ResolveProviderUnsafe(path, isResolvingTop);
            if (result.Provider is null)
            {
                throw new InvalidOperationException($"'{path}' cannot be resolved.");
            }

            return result;
        }

        public static string GetAbsolutePath(string path)
        {
            ResolveProviderResult result = ResolveProvider(path, true);
            return result.Provider.GetAbsolutePath(result.Path);
        }

        public static string ResolvePath(string path)
        {
            ResolveProviderResult result = ResolveProvider(path, false);
            StringBuilder sb = new();
            if (result.Provider.RootPath != ".")
            {
                sb.Append(result.Provider.RootPath);
                sb.Append(DirectorySeparatorChar);
            }
            sb.Append(result.Path);
            return sb.ToString();
        }

        public static bool IsPathRooted(string path)
        {
            foreach (string root in _providers.Keys)
            {
                if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static Stream OpenStream(string path, FileMode mode, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read)
        {
            ResolveProviderResult result = ResolveProviderUnsafe(path, false);
            return result.Provider is null
                ? File.Open(path, mode, access, share)
                : result.Provider.OpenStream(result.Path, mode, access, share);
        }

        public static Stream OpenStream(string path, FileMode mode, FileAccess access, FileShare share, out IVirtualFileProvider provider)
        {
            ResolveProviderResult result = ResolveProvider(path, false);
            provider = result.Provider;
            return result.Provider.OpenStream(result.Path, mode, access, share);
        }

        public static Task<Stream> OpenStreamAsync(string path, FileMode mode, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read)
        {
            return Task<Stream>.Factory.StartNew(() => OpenStream(path, mode, access, share));
        }

        public static string[] ListFiles(string path, string searchPattern, SearchOption searchOption)
        {
            ResolveProviderResult result = ResolveProvider(path, true);
            return result.Provider.ListFiles(result.Path, searchPattern, searchOption).Select(x => result.Provider.RootPath + x).ToArray();
        }

        public static Task<string[]> ListFilesAsync(string path, string searchPattern, SearchOption searchOption)
        {
            ResolveProviderResult result = ResolveProvider(path, true);
            return Task<string[]>.Factory.StartNew(() => result.Provider.ListFiles(result.Path, searchPattern, searchOption).Select(x => result.Provider.RootPath + x).ToArray());
        }

        public static void CreateDirectory(string path)
        {
            ResolveProviderResult result = ResolveProvider(path, true);
            result.Provider.CreateDirectory(result.Path);
        }

        public static bool DirectoryExists(string path)
        {
            ResolveProviderResult result = ResolveProviderUnsafe(path, true);
            return result.Provider is not null && result.Provider.DirectoryExists(result.Path);
        }

        public static bool FileExists(string path)
        {
            ResolveProviderResult result = ResolveProviderUnsafe(path, true);
            return result.Provider is not null && result.Provider.FileExists(result.Path);
        }

        public static void FileDelete(string path)
        {
            ResolveProviderResult result = ResolveProvider(path, true);
            result.Provider.FileDelete(result.Path);
        }

        public static void FileMove(string sourcePath, string destinationPath)
        {
            ResolveProviderResult sourceResult = ResolveProvider(sourcePath, true);
            ResolveProviderResult destinationResult = ResolveProvider(destinationPath, true);
            if (sourceResult.Provider == destinationResult.Provider)
            {
                sourceResult.Provider.FileMove(sourceResult.Path, destinationResult.Path);
            }
            else
            {
                sourceResult.Provider.FileMove(sourceResult.Path, destinationResult.Provider, destinationResult.Path);
            }
        }

        public static long FileSize(string path)
        {
            ResolveProviderResult result = ResolveProvider(path, true);
            return result.Provider.FileSize(result.Path);
        }

        public static DateTime GetLastWriteTime(string path)
        {
            ResolveProviderResult result = ResolveProvider(path, true);
            return result.Provider.GetLastWriteTime(result.Path);
        }

        public static string GetTempFileName(AutoCleanUpTempFiles autoCleanUpTempFiles)
        {
            int tentatives = 0;
            Stream stream = null;
            string result;
            ResolveProviderResult providerResult = ResolveProvider(autoCleanUpTempFiles.BaseTempFileName, true);
            do
            {
                result = providerResult.Path + (_random.Next() + 1).ToString("x");
                try
                {
                    stream = providerResult.Provider.OpenStream(result, FileMode.CreateNew, FileAccess.ReadWrite);
                }
                catch (IOException)
                {
                    if (tentatives++ > 0x00010000)
                    {
                        throw;
                    }
                }
            } while (stream is null);
            stream.Dispose();
            return result;
        }

        public static string BuildPath(string root, string path)
        {
            return root.Substring(0, LastIndexOfDirectorySeparator(root) + 1) + path;
        }

        public static string Combine(string x, string y)
        {
            if (x.Length == 0)
            {
                return y;
            }
            if (y.Length == 0)
            {
                return x;
            }
            char lastX = x[^1];
            char firstY = y[0];
            if (lastX != DirectorySeparatorChar && lastX != AltDirectorySeparatorChar)
            {
                if (firstY != DirectorySeparatorChar && firstY != AltDirectorySeparatorChar)
                {
                    return x + DirectorySeparatorChar + y;
                }
                return x + y;
            }
            else if (firstY != DirectorySeparatorChar && firstY != AltDirectorySeparatorChar)
            {
                return x + y;
            }
            return x + y[1..];
        }

        public static string Combine(params string[] args)
        {
            if (args.Length == 0)
            {
                return string.Empty;
            }
            string result = string.Empty;
            foreach (string str in args)
            {
                result = Combine(result, str);
            }
            return result;
        }

        public static string GetParentFolder(string path)
        {
            int lastSlashIdx = LastIndexOfDirectorySeparator(path);
            while (lastSlashIdx == path.Length - 1)
            {
                path = path[0..^1];
                lastSlashIdx = LastIndexOfDirectorySeparator(path);
            }
            return lastSlashIdx == -1 ? string.Empty : path[..lastSlashIdx];
        }

        public static string GetFileName(string path)
        {
            int lastSlashIdx = LastIndexOfDirectorySeparator(path);
            return path[(lastSlashIdx + 1)..];
        }

        public static string GetFileNameWithoutExtension(string path)
        {
            path = GetFileName(path);
            int lastDotIdx = LastIndexOfDot(path);
            if (lastDotIdx == -1)
            {
                return path;
            }
            return path[..lastDotIdx];
        }

        public static string ResolveAbsolutePath(string path)
        {
            if (!path.Contains(DirectorySeparatorChar + ".."))
            {
                return path;
            }
            List<string> pathElements = path.Split(AllDirectorySeparatorChars).ToList();
            for (int idx = 0; idx < pathElements.Count; ++idx)
            {
                if (pathElements[idx].Length > 1 && (pathElements[idx][0] == DirectorySeparatorChar || pathElements[idx][0] == AltDirectorySeparatorChar))
                {
                    pathElements[idx] = pathElements[idx][0].ToString();
                }
            }
            for (int idx = 0; idx < pathElements.Count; ++idx)
            {
                if (pathElements[idx] == "..")
                {
                    if (idx >= 3 && (pathElements[idx - 1] == DirectorySeparatorString || pathElements[idx - 1] == AltDirectorySeparatorString))
                    {
                        pathElements.RemoveRange(idx - 3, 4);
                        idx -= 4;
                    }
                }
                else if (pathElements[idx] == ".")
                {
                    if (idx + 1 < pathElements.Count && (pathElements[idx + 1] == DirectorySeparatorString || pathElements[idx + 1] == AltDirectorySeparatorString))
                    {
                        pathElements.RemoveRange(idx--, 2);
                    }
                }
            }
            return string.Join(string.Empty, pathElements);
        }

        public static string CreateRelativePath(string target, string source)
        {
            string[] targetDirectories = target.Split(AllDirectorySeparatorChars, StringSplitOptions.RemoveEmptyEntries);
            string[] sourceDirectories = source.Split(AllDirectorySeparatorChars, StringSplitOptions.RemoveEmptyEntries);
            int length = Mathematics.MathUtil.Min(targetDirectories.Length, sourceDirectories.Length);
            int common = 0;
            while (common < length)
            {
                if (targetDirectories[common] != sourceDirectories[common])
                {
                    break;
                }
                ++common;
            }
            StringBuilder sb = new();
            for (int idx = common; idx < sourceDirectories.Length; ++idx)
            {
                sb.Append(".." + DirectorySeparatorChar);
            }
            for (int idx = common; idx < targetDirectories.Length; ++idx)
            {
                sb.Append(targetDirectories[idx]);
                if (idx < targetDirectories.Length - 1)
                {
                    sb.Append(DirectorySeparatorChar);
                }
            }
            return sb.ToString();
        }

        public static string ConvertPathToUrl(string path)
        {
            return path.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar);
        }
    }
}
