using System.Collections.Generic;
using System.IO;

namespace Aaf.Sinc
{
    internal class FolderSync
    {
        private readonly ISet<string> _expectedFolders;
        private ISet<string> _currentFolders;

        public FolderSync(ISet<string> expectedFolders)
        {
            _expectedFolders = expectedFolders;
        }

        public void MakeTreeEqual()
        {
            _currentFolders = DirectoryList.GetEntireDirectoryTreeFolderNames();
            DeleteExtraFolders();
            CreateAbsentFolders();
        }

        private void DeleteExtraFolders()
        {
            var sortWithChildDirFirst = Minus(_currentFolders, _expectedFolders);
            sortWithChildDirFirst.Sort((a, b) => b.CompareTo(a));
            foreach (var d in sortWithChildDirFirst)
                Directory.Delete(d, true);
        }

        private void CreateAbsentFolders()
        {
            var sortWithChildDirLast = Minus(_expectedFolders, _currentFolders);
            sortWithChildDirLast.Sort();
            foreach (var d in sortWithChildDirLast)
                Directory.CreateDirectory(d);
        }

        public static List<string> Minus(ISet<string> a, ISet<string> b)
        {
            var r = new HashSet<string>(a);
            r.ExceptWith(b);
            return new List<string>(r);
        }
    }
}