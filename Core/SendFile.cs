using System;
using System.IO;

namespace Aaf.Sinc
{
    internal class SendFile
    {
        private readonly BinaryWriter _bw;
        private readonly string _fileName;

        internal SendFile(string fileName, BinaryWriter bw)
        {
            _fileName = fileName;
            _bw = bw;
        }

        public void Start()
        {
            Console.WriteLine("Sending File {0}", _fileName);
            if (!File.Exists(_fileName)) _bw.Write((Int64)0);
            else SendTheFile();
            _bw.Flush();
        }

        private void SendTheFile()
        {
            FileStream fs = OpenFileStream();
            if (fs == null)
            {
                SendFileLengthAndContents(0, null);
                return;
            }
            using (fs)
                SendFileLengthAndContents(new FileInfo(_fileName).Length, fs);
        }

        private FileStream OpenFileStream()
        {
            try
            {
                return new FileStream(_fileName, FileMode.Open);
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine("No permission to read file {0}. Skipping", _fileName);
                return null;
            }
        }

        private void SendFileLengthAndContents(Int64 length, Stream fileStream)
        {
            _bw.Write(length);
            for (long i = 0L; i < length; ++i)
            {
                _bw.Write((byte)fileStream.ReadByte());
                if (i > 0 && i % 50000 == 0)
                    Console.Write("Length {1}. Completed {0}%        \r", Math.Round(i * 100.0 / length), FileDetails.Formatted(length));
            }
            Console.Write("{0,50}\r", "");
        }
    }
}