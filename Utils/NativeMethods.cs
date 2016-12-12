using Aaf.Sinc.Transport;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Aaf.Sinc.Utils
{
    internal static class NativeMethods
    {
        internal enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GetConsoleWindow();

        internal static IntPtr ThisConsole = GetConsoleWindow();

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        #region Trap application termination

        [DllImport("Kernel32")]
        internal static extern bool SetConsoleCtrlHandler(AppEventHandler handler, bool add);

        internal delegate bool AppEventHandler(CtrlType sig);

        internal static AppEventHandler handler;

        internal static bool Handler(CtrlType sig)
        {
            Broadcast.Online = false;
            Thread.Sleep(3000);

            Console.WriteLine("Exiting system due to external CTRL-C, or process kill, or shutdown");
            MessageBox.Show("U are stopping service.", "Warning", MessageBoxButtons.OK);
            Environment.Exit(-1);
            return false;
        }

        #endregion Trap application termination

        #region Disable close button

        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        internal static extern IntPtr GetSystemMenu(IntPtr hWnd, IntPtr bRevert);

        [DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        internal static extern IntPtr RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        /// <summary>
        /// 禁用关闭按钮
        /// </summary>
        /// <param name="consoleName">控制台名字</param>
        internal static void DisableCloseButton(string title)
        {
            //线程睡眠, 确保closebtn中能够正常FindWindow, 否则有时会Find失败。。
            Thread.Sleep(100);

            IntPtr windowHandle = FindWindow(null, title);
            IntPtr closeMenu = GetSystemMenu(windowHandle, IntPtr.Zero);
            uint SC_CLOSE = 0xF060;
            RemoveMenu(closeMenu, SC_CLOSE, 0x0);
        }

        internal static bool IsExistsConsole(string title)
        {
            IntPtr windowHandle = FindWindow(null, title);
            if (windowHandle.Equals(IntPtr.Zero)) return false;

            return true;
        }

        #endregion Disable close button


    }
}