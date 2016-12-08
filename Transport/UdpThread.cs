using Aaf.Sinc.Utils;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Aaf.Sinc.Transport
{
    internal class UdpThread
    {

        /// <summary>
        /// 启动udp通信线程
        /// </summary>
        public void Start()
        {
            var listener = new UdpClient(Protocol.BROADCAST_PORT);
            var ep = new IPEndPoint(IPAddress.Any, Protocol.BROADCAST_PORT);
            var receiveMsg = string.Empty;
            var cmd = string.Empty;
            var nodeInfo = string.Empty;
            byte[] buff = null;
            string[] arr = null;

            while (true)
            {
                buff = listener.Receive(ref ep);
                receiveMsg = Encoding.Default.GetString(buff);
                //receiveMsg.Verbose();
                cmd = receiveMsg.Substring(0, 6);
                nodeInfo = receiveMsg.Substring(6);
                if (cmd == Protocol.NODE_STATUS_CMD)
                {
                    try
                    {
                        arr = nodeInfo.Split(':');
                        var node = new Node
                        {
                            Name = arr[0],
                            ComputerName = arr[1],
                            IP = arr[2],
                            WorkGroup = arr[3]
                        };

                        if (NodeManager.Add(node))
                        {
                            string.Format("{0} online.", node).Info();

                            string.Format("Node count:{0}", NodeManager.Count).Info();
                        }
                    }
                    catch
                    {
                        "One node offline.".Warn();
                    }
                }
            }
        }
    }
}