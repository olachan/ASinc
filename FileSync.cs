using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Aaf.Sinc
{
    internal class FileSync
    {
        private readonly ISet<FileDetails> _expectedFiles;
        private ISet<FileDetails> _currentFiles;

        public FileSync(ISet<FileDetails> destinationFiles)
        {
            _expectedFiles = destinationFiles;
        }

        public FileSync DeleteExtraFiles()
        {
            _currentFiles = FileList.GetEntireDirectoryTreeFileNames();
            DeleteFiles(Minus(_currentFiles, _expectedFiles));
            return this;
        }

        public IEnumerable<FileDetails> GetFilesToBeTransferred()
        {
            var currentFilesDict = _currentFiles.ToDictionary(f => f.NameOfFile);
            FileDetails desti = null;
            return _expectedFiles.Where(source => !currentFilesDict.TryGetValue(source.NameOfFile, out desti)
                                                  || !FileDetails.IsEqualDateAndSize(source, desti));
        }

        private static void DeleteFiles(IEnumerable<FileDetails> files)
        {
            foreach (var f in files)
            {
                Console.WriteLine("Deleting {0}", f.NameOfFile);
                File.Delete(f.NameOfFile);
            }
        }

        public static ISet<FileDetails> Minus(ISet<FileDetails> a, ISet<FileDetails> b)
        {
            var r = new HashSet<FileDetails>(a);
            r.ExceptWith(b);
            return r;
        }
    }
}