using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace Aaf.Sinc.Transport
{
    /// <summary>
    /// 广播
    /// </summary>
    internal class Broadcast
    {
        public static bool Online = true;
        public void Send()
        {
            var udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            var ep = new IPEndPoint(IPAddress.Parse("192.168.1.255"), Protocol.BROADCAST_PORT);
            //var ep = new IPEndPoint(IPAddress.Broadcast, Protocol.BROADCAST_PORT);
            var serializer = new JavaScriptSerializer();
            var node = string.Format("{0}{1}", Protocol.NODE_STATUS_CMD, serializer.Serialize(new Node
            {
                Name = Dns.GetHostName(),
                ComputerName = Dns.GetHostName(),
                IP = Protocol.LocalIP.ToString(),
                WorkGroup = Protocol.DEFAULT_WORKGROUP,
                Online = true
            }));

            var buff = Encoding.Default.GetBytes(node);

            while (Online)
            {
                udpClient.SendTo(buff, ep);
                Thread.Sleep(Protocol.BROADCAST_HEARTBEAT_INTERVAL);
            }

            node = string.Format("{0}{1}", Protocol.NODE_STATUS_CMD, serializer.Serialize(new Node
            {
                Name = Dns.GetHostName(),
                ComputerName = Dns.GetHostName(),
                IP = Protocol.LocalIP.ToString(),
                WorkGroup = Protocol.DEFAULT_WORKGROUP,
                Online = false
            }));
            buff = Encoding.Default.GetBytes(node);
            udpClient.SendTo(buff, ep);
        }
    }
}