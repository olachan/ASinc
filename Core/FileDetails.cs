using System;
using System.Globalization;
using System.IO;

namespace Aaf.Sinc
{
    [Serializable]
    internal class FileDetails
    {
        public readonly DateTime LastModified;
        public readonly string NameOfFile;
        public readonly long Size;

        public FileDetails(string nameOfFile, DateTime lastModified, long size)
        {
            NameOfFile = nameOfFile;
            LastModified = lastModified;
            Size = size;
        }

        public static FileDetails Get(string fileName)
        {
            try
            {
                var fi = new FileInfo(fileName);
                return new FileDetails(fileName, fi.LastWriteTime, fi.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error for {0}", fileName);
                throw;
            }
        }

        protected bool Equals(FileDetails other)
        {
            return string.Equals(NameOfFile, other.NameOfFile);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((FileDetails)obj);
        }

        public override int GetHashCode()
        {
            return NameOfFile.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("File {0} of Length {2} updated on {1}", NameOfFile, LastModified, Formatted(Size));
        }

        public static bool IsEqualDateAndSize(FileDetails a, FileDetails b)
        {
            return a.Size == b.Size && a.LastModified == b.LastModified;
        }

        public static string Formatted(long fileLength)
        {
            return fileLength.ToString("#,#", CultureInfo.InvariantCulture);
        }
    }
}