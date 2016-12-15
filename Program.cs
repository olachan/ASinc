using Aaf.Sinc.SharpConfig;
using Aaf.Sinc.Transport;
using Aaf.Sinc.Utils;
using NetFwTypeLib;
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

            //SetFirewall();

            Init();

            Console.ReadLine();
        }

        /// <summary>
        /// 根据配置初始化
        /// </summary>
        private static void Init()
        {
            selfIP = Protocol.LocalIP;
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

                Task.Factory.StartNew(() =>
                {
                    "FileWatcher thread start.".Verbose();
                    RunJob();
                });
                
            }
            else
            {
                "Current node is Salve.".Verbose();
            }

            Task.Factory.StartNew(() =>
            {
                "UDP receive thread start.".Verbose();
                new UdpThread().Start();
            });

            Task.Factory.StartNew(()=> {
                "UDP broadcast thread start.".Verbose();
                new Broadcast().Send();
            });

            Task.Factory.StartNew(() =>
            {
                "TCP receive thread start.".Verbose();
                Receive();
            });
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
                Task.Factory.StartNew(s =>
                {
                    //接受其他主机发送的信息
                    new Session((Socket)s, sourceDir).Start();
                }, chat);
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
        }

        private static void RunJob()
        {
            var job = new Job();
            while (true)
            {
                var ips = NodeManager.IPs.Where(x => x != selfIP.ToString()).ToList();
                var count = ips.Count;
                if (Jobs.TryDequeue(out job))
                {
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

        private static void SetFirewall()
        {
            Type fwRule = Type.GetTypeFromProgID("HNetCfg.FWRule");
            Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);

            foreach (INetFwRule rule in fwPolicy2.Rules)
                if (rule.Name == "ASinc") return;

            // create a new rule
            INetFwRule2 inboundTCPRule = (INetFwRule2)Activator.CreateInstance(fwRule);
            inboundTCPRule.Enabled = true;
            //Allow through firewall
            inboundTCPRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            //Using protocol TCP
            inboundTCPRule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;

            inboundTCPRule.LocalPorts = "9528";
            inboundTCPRule.Name = "ASinc";
            inboundTCPRule.Profiles = fwPolicy2.CurrentProfileTypes;

            // create a new rule
            INetFwRule2 inboundUDPRule = (INetFwRule2)Activator.CreateInstance(fwRule);
            inboundUDPRule.Enabled = true;
            //Allow through firewall
            inboundUDPRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            //Using protocol UDP
            inboundUDPRule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP; ;

            inboundUDPRule.LocalPorts = "9527";
            inboundUDPRule.Name = "ASinc";
            inboundUDPRule.Profiles = fwPolicy2.CurrentProfileTypes;

            // add the rule

            fwPolicy2.Rules.Add(inboundTCPRule);
            fwPolicy2.Rules.Add(inboundUDPRule);
        }
    }
}