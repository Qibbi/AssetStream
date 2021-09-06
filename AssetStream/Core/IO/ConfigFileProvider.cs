using Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Core.IO
{
    public class ConfigFileProvider : AVirtualFileProviderBase
    {
        private readonly List<string> _theSearchPaths = new();
        private readonly List<BigFile> _theBigFiles = new();

        public static readonly char DirectorySeparatorChar = Path.DirectorySeparatorChar;
        public static readonly char AltDirectorySeparatorChar = Path.AltDirectorySeparatorChar;

        public ConfigFileProvider(string rootPath, string configPath) : base(rootPath)
        {
            LoadConfig(configPath);
        }

        private static string ConvertUrlToPath(string url)
        {
            return url.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar);
        }

        private static bool IsNewLine(char c)
        {
            return c is '\n' or '\r';
        }

        private static bool IsWhiteSpace(char c)
        {
            return IsNewLine(c) || c is '\t' or ' ';
        }

        private static string GetPath(string command, string directory, int startIndex = 0)
        {
            string path = command[startIndex..];
            StringBuilder result = new();
            if (path.Length > 0 && (path.IndexOf(':') != -1 || path[0] == DirectorySeparatorChar || path[0] == AltDirectorySeparatorChar))
            {
                result.Append(path);
            }
            else
            {
                result.Append(directory);
                char end = directory[^1];
                if (end != DirectorySeparatorChar && end != AltDirectorySeparatorChar)
                {
                    result.Append(DirectorySeparatorChar);
                }
                result.Append(path);
            }
            return result.ToString();
        }

        private static string GetPaths(string command, string directory, int startIndex = 0)
        {
            string path = command[startIndex..];
            int delimIndex = path.IndexOf(';');
            if (delimIndex == -1)
            {
                return GetPath(path, directory);
            }
            StringBuilder sb = new();
            sb.Append(GetPath(path.Substring(0, delimIndex), directory));
            sb.Append(';');
            sb.Append(GetPaths(path, directory, delimIndex + 1));
            return sb.ToString();
        }

        private static void GetLine(string config, ref int startIndex, int length, out string command)
        {
            command = config.Substring(startIndex, length);
            startIndex += length;
        }

        private static bool OpenFileStream(string path, FileMode mode, FileAccess access, FileShare share, out Stream result)
        {
            if (File.Exists(path))
            {
                result = new FileStream(path, mode, access, share);
                return true;
            }
            result = null;
            return false;
        }

        private void AddBigs(string command, int startIndex, bool isRecurse)
        {
            throw new NotImplementedException();
        }

        private void SetSearchPath(string searchPath)
        {
            _theSearchPaths.Clear();
            _theSearchPaths.AddRange(searchPath.Split(';'));
        }

        private void AddSearchPath(string searchPath)
        {
            _theSearchPaths.AddRange(searchPath.Split(';'));
        }

        private void ProcessLine(string command, string directory)
        {
            const string addBig = "add-big ";
            const string addBigs = "add-bigs ";
            const string addBigsRecurse = "add-bigs-recurse ";
            const string setSearchPath = "set-search-path ";
            const string addConfig = "add-config ";
            const string tryAddConfig = "try-add-config ";
            const string addSearchPath = "add-search-path ";
            if (command.StartsWith(addBig, StringComparison.Ordinal))
            {
                string path = GetPath(command, directory, addBig.Length);
                _theBigFiles.Add(new BigFile(path));
            }
            else if (command.StartsWith(addBigs, StringComparison.Ordinal))
            {
                AddBigs(command, addBigs.Length, false);
            }
            else if (command.StartsWith(addBigsRecurse, StringComparison.Ordinal))
            {
                AddBigs(command, addBigsRecurse.Length, true);
            }
            else if (command.StartsWith(setSearchPath, StringComparison.Ordinal))
            {
                SetSearchPath(GetPaths(command, directory, setSearchPath.Length));
            }
            else if (command.StartsWith(addConfig, StringComparison.Ordinal))
            {
                string path = GetPath(command, directory, addConfig.Length);
                LoadConfig(path);
            }
            else if (command.StartsWith(tryAddConfig, StringComparison.Ordinal))
            {
                string path = GetPath(command, directory, tryAddConfig.Length);
                if (File.Exists(path))
                {
                    LoadConfig(path);
                }
            }
            else if (command.StartsWith(addSearchPath, StringComparison.Ordinal))
            {
                AddSearchPath(GetPaths(command, directory, addSearchPath.Length));
            }
        }

        private void ProcessConfig(string config, string directory)
        {
            if (directory is null)
            {
                StringBuilder sb = new(Kernel32.MAX_PATH);
                Kernel32.GetCurrentDirectoryW(Kernel32.MAX_PATH, sb);
                directory = sb.ToString();
            }
            for (int idx = 0; idx < config.Length;)
            {
                while (idx < config.Length && IsWhiteSpace(config[idx]))
                {
                    ++idx;
                }
                if (idx == config.Length)
                {
                    break;
                }
                int end = idx;
                do
                {
                    if (IsNewLine(config[end]))
                    {
                        break;
                    }
                    ++end;
                }
                while (end < config.Length);
                while (IsWhiteSpace(config[end - 1]))
                {
                    --end;
                }
                int length = end - idx;
                GetLine(config, ref idx, length, out string command);
                if (!command.Contains(' '))
                {
                    command += ' ';
                }
                ProcessLine(command, directory);
            }
        }

        private bool OpenBigStream(string path, out Stream result)
        {
            foreach (BigFile big in _theBigFiles)
            {
                if (big.TryGet(path, out BigFile.Entry entry))
                {
                    result = big.OpenStream(entry);
                    return true;
                }
            }
            result = null;
            return false;
        }

        private void LoadConfig(string config)
        {
            ProcessConfig(File.ReadAllText(config), Path.GetDirectoryName(config));
        }

        private bool FileExistsInternal(string path)
        {
            path = path.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar);
            foreach (string searchPath in _theSearchPaths)
            {
                if (searchPath.Length >= 4 && searchPath.StartsWith("big:", StringComparison.Ordinal))
                {
                    foreach (BigFile big in _theBigFiles)
                    {
                        if (big.Contains(path))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    if (File.Exists(path))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override Stream OpenStream(string url, FileMode mode, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read)
        {
            url = ConvertUrlToPath(url);
            foreach (string searchPath in _theSearchPaths)
            {
                if (searchPath.StartsWith("big:", StringComparison.Ordinal))
                {
                    string bigSearchPath = searchPath[4..];
                    foreach (BigFile bigFile in _theBigFiles)
                    {
                        if (bigFile.TryGet(Path.Combine(bigSearchPath, url), out BigFile.Entry entry))
                        {
                            return bigFile.OpenStream(entry);
                        }
                    }
                }
                else
                {
                    string pathSearchPath = Path.Combine(searchPath, url);
                    if (File.Exists(pathSearchPath))
                    {
                        return File.Open(pathSearchPath, (FileMode)mode, (FileAccess)access, (FileShare)share);
                    }
                }
            }
            return null;
        }

        public override bool FileExists(string url)
        {
            url = ConvertUrlToPath(url);
            foreach (string searchPath in _theSearchPaths)
            {
                if (searchPath.StartsWith("big:", StringComparison.Ordinal))
                {
                    string bigSearchPath = searchPath[4..];
                    foreach (BigFile bigFile in _theBigFiles)
                    {
                        if (bigFile.Contains(Path.Combine(bigSearchPath, url)))
                        {
                            return true;
                        }
                    }
                }
                else if (File.Exists(Path.Combine(searchPath, url)))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
