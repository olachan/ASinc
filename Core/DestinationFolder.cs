using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace Aaf.Sinc
{
    /// <summary>
    /// <code>var c = new Client("192.168.0.196"); c.sendFile(@"A:\Users\Ola\Desktop\a.mp4");</code>
    /// </summary>
    internal class Client : CustomTCP
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serverIP">the ip address of the server</param>
        /// <param name="port">through what port is the connection going to be established</param>
        public Client(string serverIP, Action watchFiles = null, Int32 port = 13000, bool autoConnect = true)
        {
            this.port = port;
            this.serverIP = serverIP;

            if (autoConnect)
                connect(watchFiles);
        }

        public bool connect(Action watchFiles = null)
        {
            Byte[] data = System.Text.Encoding.ASCII.GetBytes("connect");

            // Create a TcpClient.
            // Note, for this client to work you need to have a TcpServer
            // connected to the same address as specified by the server, port
            // combination.
            try
            {
                client = new TcpClient(serverIP, port);

                // Get a client stream for reading and writing.
                //  Stream stream = client.GetStream();
                stream = client.GetStream();
                if (watchFiles != null)
                    watchFiles();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public override void disconnect()
        {
            // Close everything.
            stream.Close();
            client.Close();
        }

        private static void ConnectOld(String server, Byte[] data)
        {
            try
            {
                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer
                // connected to the same address as specified by the server, port
                // combination.
                Int32 port = 13000;
                TcpClient client = new TcpClient(server, port);

                // Get a client stream for reading and writing.
                //  Stream stream = client.GetStream();

                NetworkStream stream = client.GetStream();

                // Send the message to the connected TcpServer.
                stream.Write(data, 0, data.Length);

                // Receive the TcpServer.response.

                // Buffer to store the response bytes.
                data = new Byte[256];

                // String to store the response ASCII representation.
                String responseData = String.Empty;

                // Read the first batch of the TcpServer response bytes.
                Int32 bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", responseData);

                // Close everything.
                stream.Close();
                client.Close();
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }

            Console.WriteLine("\n Press Enter to continue...");
            Console.Read();
        }

        public void sendFile(string file, string destPath = "c:\\", string srcPath = "")
        {
            if (!File.Exists(file)) return;
            //let server know what you are going to be doing...
            sendData(stream, textToBytes("<sendFile>"));

            FileProperties p = new FileProperties
            {
                creationTime = File.GetCreationTime(file),
                fileAttributes = File.GetAttributes(file),
                FileSecurity = File.GetAccessControl(file),
                lastAccessTime = File.GetLastAccessTime(file),
                lastWriteTime = File.GetLastWriteTime(file),
                DestPath = destPath,
                FileName = Path.GetFileName(file),
                FullName = Path.Combine(destPath, file.Substring(srcPath.Length).TrimStart('\\'))
            };

            // receive 1
            if (!receiveData(stream).ToUpper().Contains("<1>".ToUpper()))
            {
                Console.WriteLine("Error comunicating with server");
                return;
            }

            // send object p to server
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(p.GetType());
            x.Serialize(stream, p); // send 2

            //recieve 3
            if (!receiveData(stream).ToUpper().Contains("<3>".ToUpper()))
            {
                Console.WriteLine("Error incorrect parameters sent to server");
                return;
            }

            using (System.IO.FileStream streamFile = new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                var totalLength = streamFile.Length;
                var tmpLength = 0;

                while (true)
                {
                    byte[] chunk = new byte[MaxChunkSize];

                    int index = 0;
                    // There are various different ways of structuring this bit of code.
                    // Fundamentally we're trying to keep reading in to our chunk until
                    // either we reach the end of the stream, or we've read everything we need.
                    while (index < chunk.Length)
                    {
                        int bytesRead = streamFile.Read(chunk, index, chunk.Length - index);

                        if (bytesRead == 0)
                        {
                            break;
                        }
                        if (bytesRead < MaxChunkSize)
                        {
                            byte[] temp = new byte[bytesRead];

                            for (var i = 0; i < bytesRead; i++)
                                temp[i] = chunk[i];

                            chunk = temp;
                        }

                        index += bytesRead;
                    }
                    if (index != 0) // Our previous chunk may have been the last one
                    {
                        sendData(stream, chunk); // index is the number of bytes in the chunk
                        tmpLength += index;
                    }
                    if (tmpLength >= totalLength) // We didn't read a full chunk: we're done
                    {
                        sendData(stream, textToBytes("</sendFile>".ToUpper()));

                        //receiveData(stream);//wait recall missing to check results

                        return;
                    }
                }
            }
        }
    }

    internal class DestinationFolder
    {
        private readonly string _sourceIP;
        private BinaryReader _br;
        private Stream _stream;

        public DestinationFolder(string sourceIP)
        {
            _sourceIP = sourceIP;
        }

        public void Start()
        {
            var client = new TcpClient(_sourceIP, SourceFolder.Port);
            _stream = new BufferedStream(client.GetStream());
            _br = new BinaryReader(_stream);
            try
            {
                StartTranfer();
            }
            finally
            {
                Transfer.Send(SourceFolder.EndConnection, _stream);
                _br.Close();
                client.Close();
            }
        }

        private void StartTranfer()
        {
            if (!DirectoryCheck.AreDirectoryNamesMatching(_stream)) return;
            SyncFolders();
            SyncFiles();
        }

        private void SyncFiles()
        {
            Console.WriteLine("Receiving List of files.");
            var destinationFiles = (ISet<FileDetails>)Transfer.Receive(_stream);
            Console.WriteLine("Deleting extra files.");
            foreach (FileDetails f in new FileSync(destinationFiles).DeleteExtraFiles().GetFilesToBeTransferred())
            {
                Console.WriteLine("Receiving {0}", f);
                Transfer.ReceiveFile(f, _stream, _br);
            }
        }

        private void SyncFolders()
        {
            Console.WriteLine("Receiving Folder List.");
            var desitnationFolders = (ISet<string>)Transfer.Receive(_stream);
            Console.WriteLine("Deleting/Creating folders.");
            new FolderSync(desitnationFolders).MakeTreeEqual();
        }
    }
}