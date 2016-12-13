using Aaf.Sinc.Utils;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Aaf.Sinc.Transport
{
    internal class FileDispatcher
    {
        private string path;
        private Socket socketSent;
        private string dir;
        private string cmd;
        private string type;

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="path"></param>
        /// <param name="socketSent"></param>
        public FileDispatcher(string dir, string path, Socket socketSent, 
            string cmd = Protocol.SEND_FILE_CMD, string type = Protocol.PATH_TYPE_FILE)
        {
            this.dir = dir;
            this.path = path;
            this.socketSent = socketSent;
            this.cmd = cmd;
            this.type = type;
        }

        /// <summary>
        /// 发送文件
        /// </summary>
        public void Sent()
        {
            var msg = string.Empty;
            if (Protocol.REN_FILE_CMD == cmd)
            {
                msg = string.Join(",", path.Split(',').Select(s => s.Substring(dir.Length) + "|" + type));
            }
            else
            {
                msg = path.Substring(dir.Length) + "|" + type;
            }

            msg = string.Format("0{0}{1}", cmd, msg);

            //将 "msg" 转化为字节流的形式进行传送
            socketSent.Send(Encoding.Default.GetBytes(msg));

            //分割文件发送
            if (Protocol.SEND_FILE_CMD == cmd)
            {
                var pathType = Protocol.GetPathType(path);
                if (Protocol.PATH_TYPE_FILE != pathType) { return; }
                //定义一个读文件流
                using (var read = new FileStream(path, FileMode.Open, FileAccess.Read))
                {

                    //设置缓冲区为1024byte
                    var buff = new byte[Protocol.SOCKET_BUFFER_SIZE];
                    var len = 0;
                    while ((len = read.Read(buff, 0, Protocol.SOCKET_BUFFER_SIZE)) != 0)
                    {
                        //按实际的字节总量发送信息
                        socketSent.Send(buff, 0, len, SocketFlags.None);
                    }
                }

                //将要发送信息的最后加上"END"标识符
                msg = Protocol.SEND_FILE_COMPLETE_CMD;

                //将 "msg" 发送
                socketSent.Send(Encoding.Default.GetBytes(msg));

            }

            socketSent.Close();
            "send data complete.".Verbose();
        }
    }
}