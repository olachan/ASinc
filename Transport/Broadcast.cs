using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Aaf.Sinc.Transport
{
    /// <summary>
    /// 广播
    /// </summary>
    internal class Broadcast
    {

        public void Send()
        {
            Socket udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            var ep = new IPEndPoint(IPAddress.Parse("192.168.1.255"), Protocol.BROADCAST_PORT);
            //var ep = new IPEndPoint(IPAddress.Broadcast, Protocol.BROADCAST_PORT);

            var node = string.Format("{0}{1}:{2}:{3}:{4}", Protocol.NODE_STATUS_CMD, Dns.GetHostName(), Dns.GetHostName(), Protocol.LocalIP, Protocol.DEFAULT_WORKGROUP);

            var buff = Encoding.Default.GetBytes(node);

            while (true)
            {
                udpClient.SendTo(buff, ep);
                Thread.Sleep(Protocol.BROADCAST_HEARTBEAT_INTERVAL);
            }
        }
    }
}