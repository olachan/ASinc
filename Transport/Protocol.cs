using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Aaf.Sinc.Transport
{
    public class Protocol
    {
        /// <summary>
        /// 默认工作组
        /// </summary>
        public const string DEFAULT_WORKGROUP = "WorkGroup";

        /// <summary>
        /// 广播端口
        /// </summary>
        public const int BROADCAST_PORT = 7999;

        /// <summary>
        /// 接受信息的端口
        /// </summary>
        public const int RECEIVE_MSG_PORT = 8001;

        /// <summary>
        /// 广播心跳间隔
        /// </summary>
        public const int BROADCAST_HEARTBEAT_INTERVAL = 2000;

        /// <summary>
        /// 传输Buffer大小
        /// </summary>
        public const int SOCKET_BUFFER_SIZE = 1024;

        /// <summary>
        /// 发送文件指令
        /// </summary>
        public const string SEND_FILE_CMD = "ADD";

        /// <summary>
        /// 删除文件指令
        /// </summary>
        public const string DEL_FILE_CMD = "DEL";

        /// <summary>
        /// 文件重命名指令
        /// </summary>
        public const string REN_FILE_CMD = "REN";

        /// <summary>
        /// 发送文本指令
        /// </summary>
        public const string SEND_TEXT_CMD = "MSG";

        /// <summary>
        /// 文件发送完成指令
        /// </summary>
        public const string SEND_FILE_COMPLETE_CMD = "END";

        /// <summary>
        /// 节点状态指令
        /// </summary>
        public const string NODE_STATUS_CMD = ":NODE:";

        /// <summary>
        /// 获取内网有效IPv4
        /// </summary>
        /// <returns></returns>
        public static IPAddress LocalIP
        {
            get
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                return host.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            }
        }
    }
}