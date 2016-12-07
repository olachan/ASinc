using System.Collections.Generic;
using System.IO;

namespace Aaf.Sinc
{
    internal class DirectoryList
    {
        private readonly HashSet<string> _names = new HashSet<string>();
        private const string CurrentDir = ".";

        public static ISet<string> GetEntireDirectoryTreeFolderNames()
        {
            var fs = new DirectoryList();
            fs.AddChildDirectories(CurrentDir);
            fs._names.TrimExcess();
            return fs._names;
        }

        private void AddChildDirectories(string dir)
        {
            foreach (var d in Directory.GetDirectories(dir))
            {
                _names.Add(d);
                AddChildDirectories(d);
            }
        }
    }
}