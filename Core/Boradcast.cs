using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Aaf.Sinc
{
    public class Boradcast
    {
        private const int listenPort = 11000;

        public static void Send()
        {
            //Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
            //ProtocolType.Udp);

            //IPAddress broadcast = IPAddress.Parse("192.168.1.255");

            //byte[] sendbuf = Encoding.ASCII.GetBytes(CopyRight);
            //IPEndPoint ep = new IPEndPoint(broadcast, 11000);

            //s.SendTo(sendbuf, ep);

            UdpClient client = new UdpClient();
            IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast, listenPort);
            byte[] bytes = Encoding.ASCII.GetBytes("Foo");
            client.Send(bytes, bytes.Length, ip);
            client.Close();

            Console.WriteLine("Beating...");
        }

        public static void ReceiveListener(out string hostIP)
        {
            bool done = false;
            hostIP = "192.168.1.1";
            UdpClient listener = new UdpClient(listenPort);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);

            try
            {
                while (!done)
                {
                    Console.WriteLine("Waiting for broadcast");
                    byte[] bytes = listener.Receive(ref groupEP);

                    Console.WriteLine("Received broadcast from {0} : {1}",
                        groupEP.ToString(),
                        Encoding.ASCII.GetString(bytes, 0, bytes.Length));
                    hostIP = groupEP.Address.ToString();
                    done = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                listener.Close();
            }
        }

        public static string GetLocalIP()
        {
            // Getting Ip address of local machine...
            // First get the host name of local machine.
            var strHostName = Dns.GetHostName();
            //Console.WriteLine("Local Machine's Host Name: " + strHostName);
            // Then using host name, get the IP address list..
            var ipEntry = Dns.GetHostEntry(strHostName);
            var addr = ipEntry.AddressList;

            for (int i = 0; i < addr.Length; i++)
            {
                if (addr[i].AddressFamily == AddressFamily.InterNetwork)
                    return addr[i].ToString();
            }
            return "127.0.0.1";
        }
    }
}