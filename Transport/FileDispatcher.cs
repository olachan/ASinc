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

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="path"></param>
        /// <param name="socketSent"></param>
        public FileDispatcher(string dir, string path, Socket socketSent, string cmd = "ADD")
        {
            this.dir = dir;
            this.path = path;
            this.socketSent = socketSent;
            this.cmd = cmd;
        }

        /// <summary>
        /// 发送文件
        /// </summary>
        public void Sent()
        {
            var msg = string.Empty;
            if (cmd == Protocol.REN_FILE_CMD)
            {
                msg = string.Join(",",path.Split(',').Select(s=>s.Substring(dir.Length)));
            }
            else
            {
                msg = path.Substring(dir.Length);
            }

            msg = string.Format("0{0} {1}", cmd, msg);

            //将 "msg" 转化为字节流的形式进行传送
            socketSent.Send(Encoding.Default.GetBytes(msg));
            if (cmd == Protocol.SEND_FILE_CMD)
            {
                //定义一个读文件流
                var read = new FileStream(path, FileMode.Open, FileAccess.Read);

                //设置缓冲区为1024byte
                byte[] buff = new byte[Protocol.SOCKET_BUFFER_SIZE];
                int len = 0;
                while ((len = read.Read(buff, 0, Protocol.SOCKET_BUFFER_SIZE)) != 0)
                {
                    //按实际的字节总量发送信息
                    socketSent.Send(buff, 0, len, SocketFlags.None);
                }

                //将要发送信息的最后加上"END"标识符
                msg = Protocol.SEND_FILE_COMPLETE_CMD;

                //将 "msg" 发送
                socketSent.Send(Encoding.Default.GetBytes(msg));
                read.Close();
            }

            socketSent.Close();

        }
    }
}