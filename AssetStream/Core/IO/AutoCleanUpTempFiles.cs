using System;

namespace Core.IO
{
    public class AutoCleanUpTempFiles : IDisposable
    {
        public string BaseTempFileName { get; }

        public AutoCleanUpTempFiles(string baseTempFileName)
        {
            BaseTempFileName = baseTempFileName;
        }

        public void Dispose()
        {
            foreach (string file in VirtualFileSystem.ListFiles(VirtualFileSystem.GetParentFolder(BaseTempFileName),
                                                                VirtualFileSystem.GetFileName(BaseTempFileName) + "*",
                                                                System.IO.SearchOption.TopDirectoryOnly))
            {
                try
                {
                    VirtualFileSystem.FileDelete(file);
                }
                catch (SystemException ex)
                {
                    // TODO: debug log
                }
            }
        }
    }
}
