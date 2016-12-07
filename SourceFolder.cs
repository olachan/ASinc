using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Aaf.Sinc
{
    internal class CustomTCP
    {
        protected const int MaxChunkSize = 1024;

        protected Int32 port { get; set; }
        protected string serverIP { get; set; }
        protected TcpClient client { get; set; }
        protected static NetworkStream stream { get; set; }

        protected void sendData(NetworkStream stream, Byte[] data)
        {
            // Send the message to the connected TcpServer.
            stream.Write(data, 0, data.Length);
        }

        protected String receiveData(NetworkStream stream)
        {
            // Buffer to store the response bytes.
            Byte[] data = new Byte[MaxChunkSize];

            // String to store the response ASCII representation.
            String responseData = String.Empty;

            // Read the first batch of the TcpServer response bytes.
            Int32 bytes = stream.Read(data, 0, data.Length);
            responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
            Console.WriteLine("Received: {0}", responseData);

            return responseData;
        }

        protected static Byte[] textToBytes(string text)
        {
            return System.Text.Encoding.ASCII.GetBytes(text);
        }

        public virtual void disconnect()
        {
        }

        public bool isServerConected
        {
            get
            {
                if (client == null) return false;
                return client.Connected;
            }
        }
    }

    [Serializable]
    public class FileProperties
    {
        public string FileName { get; set; }
        public string FullName { get; set; }
        public string DestPath { get; set; }
        public double FileSize { get; set; }

        public FileAttributes fileAttributes { get; set; }
        public System.Security.AccessControl.FileSecurity FileSecurity { get; set; }
        public DateTime creationTime { get; set; }
        public DateTime lastAccessTime { get; set; }
        public DateTime lastWriteTime { get; set; }
    }

    /// <summary>
    /// <code>var s = new Server("192.168.0.196");s.startServer()</code>
    /// </summary>
    internal class Server : CustomTCP
    {
        private System.IO.FileStream _FileStream;
        private static TcpListener server;
        private static bool disconect;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="localAddr">The ip address of the server</param>
        /// <param name="port">on what port the server going to be listening to?</param>
        /// <param name="autoStartServer">start listening for connections now? you may call the startserver() method latter...</param>
        public Server(string localAddr, Int32 port = 13000, bool autoStartServer = false)
        {
            this.port = port;
            this.serverIP = localAddr;

            if (autoStartServer)
                start();
        }

        /// <summary>
        /// Start listening for connections
        /// </summary>
        public void startServer()
        {
            start();
        }

        public override void disconnect()
        {
            // Close everything.
            stream.Close();
            client.Close();
            server.Stop();
            disconect = true;
        }

        private void start()
        {
            server = null;

            try
            {
                // TcpListener server = new TcpListener(port);
                server = new TcpListener(IPAddress.Parse(serverIP), port);

                // Start listening for client requests.
                server.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[MaxChunkSize];
                String data = null;

                // Enter the listening loop.
                while (disconect == false)
                {
                    Console.WriteLine("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    // Get a stream object for reading and writing
                    stream = client.GetStream();

                    int i;
                    try
                    {
                        // Loop to receive all the data sent by the client.
                        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            // Translate data bytes to a ASCII string.
                            data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                            Console.WriteLine("Received: {0}", data);

                            if (data.ToUpper().Contains("<sendFile>".ToUpper()))
                            {
                                receiveFile(bytes);
                            }

                            continue;
                        }
                    }
                    catch { }

                    // Shutdown and end connection
                    client.Close();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }

        private void receiveFile(Byte[] bytes)
        {
            // send 1
            sendData(stream, textToBytes("<1>"));

            // receive 2
            int length = stream.Read(bytes, 0, bytes.Length);
            byte[] tempA = new byte[length];
            for (int k = 0; k < length; k++)
                tempA[k] = bytes[k];

            Stream ms = new MemoryStream(tempA);
            FileProperties p = new FileProperties();
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(p.GetType());

            try
            {
                p = (FileProperties)x.Deserialize(ms);

                if (Directory.Exists(p.DestPath))
                {
                    //send 3
                    sendData(stream, textToBytes("<3>"));
                }
                else
                {
                    //send 3
                    sendData(stream, textToBytes("<no>"));
                    return;
                }
            }
            catch
            {
                //send 3
                sendData(stream, textToBytes("<no>"));
                return;
            }

            int i;
            string temp = p.FullName + ".temp";
            var dir = temp.Substring(0, temp.LastIndexOf("\\"));
            Directory.CreateDirectory(dir);

            _FileStream = new System.IO.FileStream(temp, System.IO.FileMode.Create, System.IO.FileAccess.Write);

            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                if (i == 11 & System.Text.Encoding.ASCII.GetString(bytes, 0, i).ToUpper().Equals("</sendFile>".ToUpper()))
                {
                    _FileStream.Close();

                    Console.WriteLine("D!");

                    File.SetAttributes(temp, p.fileAttributes);
                    File.SetAccessControl(temp, p.FileSecurity);
                    File.SetCreationTime(temp, p.creationTime);
                    File.SetLastAccessTime(temp, p.lastAccessTime);
                    File.SetLastWriteTime(temp, p.lastWriteTime);

                    if (File.Exists(p.FullName)) File.Delete(p.FullName);

                    File.Move(temp, p.FullName);

                    //sendData(stream, textToBytes("<done>"));

                    Console.WriteLine("Done!");

                    return;
                }
                _FileStream.Write(bytes, 0, i);
            }

            return;
        }
    }

    internal class SourceFolder
    {
        public const int Port = 8901;
        public const int Timeout = 600000;
        public const int BufferSize = 1024;

        public const string EndConnection =
            "**End of Communiction with Client. All Files Transferred. Program by Ola Chan.**";

        private readonly string _ipAddress;
        private BinaryWriter _bw;
        private Socket _socket;
        private Stream _stream;

        public SourceFolder(string ipAddress)
        {
            _ipAddress = ipAddress;
        }

        public void Start()
        {
            WaitForClientToConnect();
            try
            {
                TransferInfo();
            }
            finally
            {
                _bw.Close();
            }
        }

        private void WaitForClientToConnect()
        {
            IPAddress ipAddress = IPAddress.Parse(_ipAddress);
            var listener = new TcpListener(ipAddress, Port);
            listener.Start();
            _socket = listener.AcceptSocket();
            _stream = new BufferedStream(new NetworkStream(_socket));
            _bw = new BinaryWriter(_stream);
            Console.WriteLine("Destination machine connected.");
            listener.Stop();
        }

        private void TransferInfo()
        {
            if (!DirectoryCheck.AreDirectoryNamesMatching(_stream))
            {
                Transfer.Receive(_stream); //Wait for EndConnection message
                return;
            }
            SendFolderNames();
            SendFileNames();
            SendSpecificFileContents();
        }

        private void SendSpecificFileContents()
        {
            Console.WriteLine("Waiting for fileName that has to be transferred.");
            while (true)
            {
                var fileName = (string)Transfer.Receive(_stream);
                if (fileName == EndConnection) return;
                new SendFile(fileName, _bw).Start();
            }
        }

        private void SendFileNames()
        {
            Console.WriteLine("Sending List of all Files.");
            SendObject(FileList.GetEntireDirectoryTreeFileNames());
        }

        private void SendFolderNames()
        {
            Console.WriteLine("Sending Folder List.");
            SendObject(DirectoryList.GetEntireDirectoryTreeFolderNames());
        }

        private void SendObject(Object o)
        {
            Transfer.Send(o, _stream);
        }
    }
}