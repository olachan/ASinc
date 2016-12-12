using Aaf.Sinc.Utils;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web.Script.Serialization;

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
            var nodeModel = string.Empty;
            byte[] buff = null;
            //string[] arr = null;

            var serializer = new JavaScriptSerializer();

            while (true)
            {
                buff = listener.Receive(ref ep);
                receiveMsg = Encoding.Default.GetString(buff);
                //receiveMsg.Verbose();
                cmd = receiveMsg.Substring(0, 6);
                nodeModel = receiveMsg.Substring(6);
                if (cmd == Protocol.NODE_STATUS_CMD)
                {
                    try
                    {
                        //arr = nodeModel.Split(':');
                        //var node = new Node
                        //{
                        //    Name = arr[0],
                        //    ComputerName = arr[1],
                        //    IP = arr[2],
                        //    WorkGroup = arr[3]
                        //};

                        var node = serializer.Deserialize<Node>(nodeModel);

                        if (node.Online )
                        {
                            if (NodeManager.Add(node))
                            {
                                node.ToString().Info();

                                string.Format("Node count:{0}", NodeManager.Count).Info();
                            }
                        }
                        else{
                            NodeManager.Remove(node);
                            node.ToString().Info();
                            string.Format("Node count:{0}", NodeManager.Count).Info();
                        }

                    }
                    catch
                    {
                        "one node err.".Warn();
                    }
                }
            }
        }
    }
}