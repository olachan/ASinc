using Aaf.Sinc.SharpConfig;
using Aaf.Sinc.Transport;
using Aaf.Sinc.Utils;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Aaf.Sinc
{
    internal class Program
    {
        private const string TITLE = "ASinc";
        private const string CopyRight = "Power by Ola Chan. Details at http://chenzheng.com";
        private const string PARAMS_CONFIG_FILE = "Config\\params.ini";

        private static NotifyIcon notificationIcon;

        /// <summary>
        /// 0 - SW_HIDE - Hides the window and activates another window.
        /// </summary>
        private static Int32 showWindow = 1;

        /// <summary>
        /// 任务队列
        /// </summary>
        private static ConcurrentQueue<Job> Jobs = new ConcurrentQueue<Job>();

        private static string sourceDir = string.Empty;
        private static bool isMaster = false;
        private static IPAddress selfIP = null;

        /// <summary>
        /// 定义作为服务器端接受信息套接字
        /// </summary>
        public static Socket socketReceive = null;

        /// <summary>
        /// 定义接受信息的IP地址和端口号
        /// </summary>
        public static IPEndPoint ipReceive = null;

        /// <summary>
        /// 定义作为客户端发送信息套接字
        /// </summary>
        public static Socket socketSent = null;

        /// <summary>
        /// 定义发送信息的IP地址和端口号
        /// </summary>
        public static IPEndPoint ipSent = null;

        /// <summary>
        /// 定义接受信息的套接字
        /// </summary>
        public static Socket chat = null;

        [STAThread]
        private static void Main(string[] args)
        {
            CopyRight.Verbose();

            new Thread(
            delegate ()
            {
                notificationIcon = new NotifyIcon()
                {
                    Icon = new Icon(@"app.ico")
                };

                notificationIcon.ContextMenuStrip = new ContextMenuStrip();
                notificationIcon.ContextMenuStrip.Items.AddRange(new ToolStripItem[] { new ToolStripMenuItem() });
                notificationIcon.ContextMenuStrip.Items[0].Text = "Exit";
                notificationIcon.ContextMenuStrip.Items[0].Click += new EventHandler(smoothExit);

                notificationIcon.Visible = true;

                notificationIcon.MouseClick += notificationIcon_MouseClick;
                Application.Run();
            }).Start();

            Console.Title = TITLE;
            NativeMethods.DisableCloseButton(Console.Title);

            // Some biolerplate to react to close window event, CTRL-C, kill, etc
            NativeMethods.handler += new NativeMethods.AppEventHandler(NativeMethods.Handler);
            NativeMethods.SetConsoleCtrlHandler(NativeMethods.handler, true);
            "CTRL-C to stop service.".Warn();

            Init();

            Console.ReadLine();
        }

        /// <summary>
        /// 根据配置初始化
        /// </summary>
        private static void Init()
        {
            var cfg = Configuration.LoadFromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PARAMS_CONFIG_FILE));
            sourceDir = cfg["General"]["SourceDir"].StringValue;
            if (string.IsNullOrEmpty(sourceDir) || sourceDir == ".") sourceDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!Directory.Exists(sourceDir)) Directory.CreateDirectory(sourceDir);
            isMaster = cfg["General"]["IsMaster"].BoolValue;

            if (isMaster)
            {
                "Current node is Master.".Verbose();
                var fileWatcher = new FileWatcher(sourceDir, "*.*", true);
                fileWatcher.OnChanged += new FileSystemEventHandler(OnChanged);
                fileWatcher.OnCreated += new FileSystemEventHandler(OnCreated);
                fileWatcher.OnRenamed += new RenamedEventHandler(OnRenamed);
                fileWatcher.OnDeleted += new FileSystemEventHandler(OnDeleted);
                fileWatcher.Start();

                var jobThread = new Thread(() =>
                  {
                      RunJob();
                  });

                jobThread.Start();
            }
            else
            {
                "Current node is Salve.".Verbose();
            }
            selfIP = Protocol.LocalIP;

            var startUdpThread = new UdpThread();
            var tUdpThread = new Thread(new ThreadStart(startUdpThread.Start));
            tUdpThread.IsBackground = true;
            tUdpThread.Start();

            var broadCast = new Broadcast();
            var tBroadCast = new Thread(new ThreadStart(broadCast.Send));
            tBroadCast.IsBackground = true;
            tBroadCast.Start();

            var receive = new Thread(new ThreadStart(Receive));
            receive.IsBackground = true;
            receive.Start();
        }

        /// <summary>
        /// 处理接受到的信息
        /// </summary>
        private static void Receive()
        {
            try
            {
                //初始化接受套接字: 寻址方案, 以字符流方式和Tcp通信
                socketReceive = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //获取本机IP地址并设置接受信息的端口
                ipReceive = new IPEndPoint(selfIP, Protocol.RECEIVE_MSG_PORT);

                //将本机IP地址和接受端口绑定到接受套接字
                socketReceive.Bind(ipReceive);

                //监听端口, 并设置监听缓存大小为1024byte
                socketReceive.Listen(Protocol.SOCKET_BUFFER_SIZE);
            }
            catch (Exception err)
            {
                err.Message.Error();
            }

            //定义接受信息时缓冲区
            var buff = new byte[Protocol.SOCKET_BUFFER_SIZE];

            //连续接受客户端发送过来的信息
            while (true)
            {
                //定义一个chat套接字用来接受信息
                var chat = socketReceive.Accept();

                //定义一个处理信息的对象
                var cs = new Session(chat, sourceDir);

                //定义一个新的线程用来接受其他主机发送的信息
                var newThread = new Thread(new ThreadStart(cs.Start));

                //启动新的线程
                newThread.Start();
            }
        }

        private static void OnCreated(object source, FileSystemEventArgs e)
        {
            ConsoleExtensions.Time();
            string.Format("{0} was created", e.FullPath).Info();
            Send(e.FullPath, Protocol.SEND_FILE_CMD, Protocol.GetPathType(e.FullPath));
        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            ConsoleExtensions.Time();
            string.Format("{0} was changed", e.FullPath).Info();
            Send(e.FullPath, Protocol.SEND_FILE_CMD, Protocol.GetPathType(e.FullPath));
        }

        private static void OnDeleted(object source, FileSystemEventArgs e)
        {
            ConsoleExtensions.Time();
            string.Format("{0} was deleted", e.FullPath).Info();
            Send(e.FullPath, Protocol.DEL_FILE_CMD, Protocol.GetPathType(e.FullPath));
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            ConsoleExtensions.Time();
            string.Format("{0} was renamed to {1}", e.OldFullPath, e.FullPath).Info();
            Send(e.FullPath + "," + e.OldFullPath, Protocol.REN_FILE_CMD, Protocol.GetPathType(e.OldFullPath));
        }

        private static void Send(string path, string cmd = Protocol.SEND_FILE_CMD,
            string type = Protocol.PATH_TYPE_FILE)
        {
            if (IgnoreHepler.IsMatch(path))
            {
                string.Format("{0} was matched by ignore rules. so syn pass.", path).Warn();
                return;
            }

            Jobs.Enqueue(new Job { Path = path, Cmd = cmd, PathType = type });

            //var ip = string.Empty;
            //LanSocket socketConnet = null;
            //FileDispatcher fileDispatcher = null;
            //Thread tConnection = null;
            //Thread tSentFile = null;
            //var ips = NodeManager.IPs;
            //for (int i = 0; i < ips.Count; i++)
            //{
            //    ip = ips[i];
            //    if (ip == selfIP.ToString()) continue;

            //    //初始化接受套接字: 寻址方案, 以字符流方式和Tcp通信
            //    socketSent = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //    //设置服务器IP地址和端口
            //    ipSent = new IPEndPoint(IPAddress.Parse(ip), Protocol.RECEIVE_MSG_PORT);

            //    //与服务器进行连接
            //    socketConnet = new LanSocket(socketSent, ipSent);
            //    tConnection = new Thread(new ThreadStart(socketConnet.Connect));
            //    tConnection.Start();
            //    Thread.Sleep(100);

            //    //将要发送的文件加上"DAT"标识符
            //    fileDispatcher = new FileDispatcher(sourceDir, path, socketSent, cmd, type);
            //    tSentFile = new Thread(new ThreadStart(fileDispatcher.Sent));
            //    tSentFile.Start();
            //}
        }

        private static void RunJob()
        {
            var job = new Job();
            while (true)
            {
                if (Jobs.TryDequeue(out job))
                {
                    var ips = NodeManager.IPs.Where(x => x != selfIP.ToString()).ToList();
                    var count = ips.Count;
                    //var tasks = new Task[count];
                    for (var i = 0; i < count; i++)
                    {
                        ////初始化接受套接字: 寻址方案, 以字符流方式和Tcp通信
                        //socketSent = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                        ////设置服务器IP地址和端口
                        //ipSent = new IPEndPoint(IPAddress.Parse(ips[i]), Protocol.RECEIVE_MSG_PORT);

                        ////与服务器进行连接
                        //new LanSocket(socketSent, ipSent).Connect();

                        ////var socketConnet = new LanSocket(socketSent, ipSent);
                        ////var tConnection = new Thread(new ThreadStart(socketConnet.Connect));
                        ////tConnection.Start();

                        //Thread.Sleep(90);

                        ////发送文件
                        //new FileDispatcher(sourceDir,
                        //    job.Path, socketSent,
                        //    job.Cmd, job.PathType).Sent();

                        var task = Task.Factory.StartNew(ip =>
                        {
                            //初始化接受套接字: 寻址方案, 以字符流方式和Tcp通信
                            socketSent = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                            //设置服务器IP地址和端口
                            ipSent = new IPEndPoint(IPAddress.Parse((string)ip), Protocol.RECEIVE_MSG_PORT);

                            //与服务器进行连接
                            new LanSocket(socketSent, ipSent).Connect();

                            //var socketConnet = new LanSocket(socketSent, ipSent);
                            //var tConnection = new Thread(new ThreadStart(socketConnet.Connect));
                            //tConnection.Start();

                            Thread.Sleep(90);

                            //发送文件
                            new FileDispatcher(sourceDir,
                                job.Path, socketSent,
                                job.Cmd, job.PathType).Sent();

                        }, ips[i]);
                        Task.WaitAll(task);
                    }
                    string.Format("job [{0}] complete.", job).Verbose();
                }
                else
                    Thread.Sleep(Protocol.BROADCAST_HEARTBEAT_INTERVAL);
            }
        }

        private static void notificationIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                //reserve right click for context menu
                showWindow = ++showWindow % 2;
                NativeMethods.ShowWindow(NativeMethods.GetConsoleWindow(), showWindow);
            }
        }

        private static void smoothExit(object sender, EventArgs e)
        {
            notificationIcon.Visible = false;
            Application.Exit();
            Environment.Exit(1);
        }
    }
}