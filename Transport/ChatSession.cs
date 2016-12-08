using Aaf.Sinc.Utils;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Aaf.Sinc.Transport
{
    /// <summary>
    /// 聊天会话
    /// </summary>
    internal class ChatSession
    {
        private static string fileName = string.Empty;
        private Socket chat;
        private string pack;
        private string dir;

        /// <summary>
        /// 初始化构造方法
        /// </summary>
        /// <param name="chat"></param>
        /// <param name="dir"></param>
        public ChatSession(Socket chat,string dir)
        {
            this.chat = chat;
            this.dir = dir;
        }

        /// <summary>
        /// 对信息进行处理
        /// </summary>
        public void Start()
        {
            //获取远程主机的IP地址和端口号
            var ep = chat.RemoteEndPoint as IPEndPoint;

            //设置
            byte[] buff = new byte[Protocol.SOCKET_BUFFER_SIZE];
            int len;
            while ((len = chat.Receive(buff)) != 0)
            {
                dir.Verbose();
                var msg = Encoding.Default.GetString(buff, 0, len);
                pack = msg.Substring(0, 1);
                var cmd = msg.Substring(1, 3);
                if (cmd == Protocol.SEND_FILE_CMD)
                {
                    msg = msg.Substring(4);
                    fileName = dir+msg;
                    string.Format("+{0}.", fileName).Info();
                    var fi = new FileInfo(fileName);
                    if (!Directory.Exists(fi.DirectoryName)) Directory.CreateDirectory(fi.DirectoryName);
                    Receive(ep, buff, ref len, ref msg);
                }
                else if (cmd == Protocol.DEL_FILE_CMD)
                {
                    msg = msg.Substring(4);
                    fileName = dir + msg;
                    string.Format("-{0}.", fileName).Info();
                    if (File.Exists(fileName)) File.Delete(fileName);
                }
                else if (cmd == Protocol.REN_FILE_CMD)
                {
                    msg = msg.Substring(4);
                    var arr = msg.Split(',');
                    fileName = dir+arr[0];
                    var oldFileName= dir+arr[1];
                    string.Format("%{0} to {1}.", oldFileName, fileName).Info();
                    if (File.Exists(oldFileName))
                    {
                        if (File.Exists(fileName)) File.Delete(fileName);
                        File.Move(oldFileName, fileName);
                    }
                }
                else if (cmd == Protocol.SEND_TEXT_CMD)
                {
                    msg = Encoding.Default.GetString(buff);
                }
            }
            chat.Close();
        }

        /// <summary>
        /// 接收文件
        /// </summary>
        /// <param name="ep">IP地址</param>
        /// <param name="buff">文件字节缓冲区</param>
        /// <param name="len">传输文件大小</param>
        /// <param name="msg">命令消息</param>
        private void Receive(IPEndPoint ep, byte[] buff, ref int len, ref string msg)
        {
           
            var writer = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            while ((len = chat.Receive(buff)) != 0)
            {
                msg = Encoding.Default.GetString(buff, 0, len);
                if (msg == Protocol.SEND_FILE_COMPLETE_CMD)
                {
                    break;
                }
                writer.Write(buff, 0, len);
            }
            writer.Write(buff, 0, len);
            writer.Close();
        }
    }
}