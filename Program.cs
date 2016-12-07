using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Aaf.Sinc
{
    internal class Program
    {
        public static NotifyIcon notificationIcon;

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        private static IntPtr ThisConsole = GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static Int32 showWindow = 1; //0 - SW_HIDE - Hides the window and activates another window.

        private const string CopyRight = "Power by Ola Chan. Details at http://znzs.com";

        private static Client client = null;

        private static Server server = null;

        private static string sourcePath = AppDomain.CurrentDomain.BaseDirectory;

        private static string destPath = AppDomain.CurrentDomain.BaseDirectory;

        private static string filter = ConfigurationManager.AppSettings["FileFilter"];

        private static void Main(string[] args)
        {
            Console.WriteLine(CopyRight);

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

            var isServer = true;
            bool.TryParse(ConfigurationManager.AppSettings["IsServer"], out isServer);

            if (isServer)
                StartServer();
            else
                StartClient();
        }

        private static void StartClient()
        {
            server = new Server(Boradcast.GetLocalIP());
            var stateTimer = new System.Threading.Timer((s) =>
            {
                if (!server.isServerConected)
                    Boradcast.Send();
            }, null, 0, 3000);

            server.startServer();
        }

        private static void StartServer()
        {
            //source
            var hostIP = string.Empty;
            Boradcast.ReceiveListener(out hostIP);
            client = new Client(hostIP, () =>
            {
                // Create a new FileSystemWatcher and set its properties.
                FileSystemWatcher watcher = new FileSystemWatcher();
                watcher.Path = sourcePath;
                /* Watch for changes in LastAccess and LastWrite times, and
                   the renaming of files or directories. */
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                   | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                // Only watch text files.
                watcher.Filter = filter;
                watcher.IncludeSubdirectories = true;

                // Add event handlers.
                watcher.Changed += new FileSystemEventHandler(OnChanged);
                watcher.Created += new FileSystemEventHandler(OnChanged);
                watcher.Deleted += new FileSystemEventHandler(OnDeleted);
                watcher.Renamed += new RenamedEventHandler(OnRenamed);

                // Begin watching.
                watcher.EnableRaisingEvents = true;
            });

            // Wait for the user to quit the program.
            Console.WriteLine("Press \'q\' to quit.");
            while (Console.Read() != 'q') ;
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
            if (client.isServerConected) client.sendFile(e.FullPath, destPath, sourcePath);
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
            if (client.isServerConected) client.sendFile(e.FullPath, destPath, sourcePath);
        }

        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
            //if (client.isServerConected) client.sendFile(e.FullPath, destPath, sourcePath);
        }

        private static void notificationIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                //reserve right click for context menu
                showWindow = ++showWindow % 2;
                ShowWindow(ThisConsole, showWindow);
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