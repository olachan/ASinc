using System;
using System.IO;

namespace Aaf.Sinc
{
    internal class DirectoryCheck
    {
        public static string GetCurrentDirectoryName()
        {
            var s = Directory.GetCurrentDirectory();
            return s.Substring(s.LastIndexOf('\\') + 1);
        }

        public static bool AreDirectoryNamesMatching(Stream stream)
        {
            var myDir = GetCurrentDirectoryName();
            Transfer.Send(myDir, stream);
            var remoteDir = (string)Transfer.Receive(stream);
            if (myDir != remoteDir)
                PrintError(string.Format("Current directory name {0}, does not match remote directory name {1}", myDir, remoteDir));
            return myDir == remoteDir;
        }

        private static void PrintError(string message)
        {
            Console.Error.WriteLine(message);
        }
    }
}