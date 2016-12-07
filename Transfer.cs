using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Aaf.Sinc
{
    internal class Transfer
    {
        public static void ReceiveFile(FileDetails f, Stream s, BinaryReader br)
        {
            new BinaryFormatter().Serialize(s, f.NameOfFile); //Send FileName to source machine.
            if (File.Exists(f.NameOfFile)) File.Delete(f.NameOfFile); //Hidden files have to be deleted first
            using (var fs = new FileStream(f.NameOfFile, FileMode.Create))
                WriteFileContentsReceived(br, fs);
            File.SetLastWriteTime(f.NameOfFile, f.LastModified);
        }

        private static void WriteFileContentsReceived(BinaryReader br, Stream fileStream)
        {
            long i = 0;
            var length = br.ReadInt64();
            for (var bytesToRead = length; bytesToRead > 0; bytesToRead--)
            {
                fileStream.WriteByte(br.ReadByte());
                if (i++ > 0 && i % 50000 == 0)
                    Console.Write("Length {1}. Completed {0}%        \r", Math.Round(i * 100.0 / length), FileDetails.Formatted(length));
            }
            Console.Write("{0,50}\r", "");
        }

        public static void Send(Object o, Stream s)
        {
            new BinaryFormatter().Serialize(s, o);
        }

        public static Object Receive(Stream s)
        {
            return new BinaryFormatter().Deserialize(s);
        }
    }
}