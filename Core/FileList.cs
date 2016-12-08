using System.Collections.Generic;
using System.IO;

namespace Aaf.Sinc
{
    internal class FileList
    {
        private const string CurrentDir = ".";
        private readonly HashSet<FileDetails> _files = new HashSet<FileDetails>();

        public static ISet<FileDetails> GetEntireDirectoryTreeFileNames()
        {
            var fs = new FileList();
            fs.AddChildFiles(CurrentDir);
            fs._files.TrimExcess();
            return fs._files;
        }

        private void AddChildFiles(string dir)
        {
            foreach (var f in Directory.GetFiles(dir))
                _files.Add(FileDetails.Get(f));
            foreach (var d in Directory.GetDirectories(dir))
                AddChildFiles(d);
        }
    }
}