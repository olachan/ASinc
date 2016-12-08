using System;

namespace Aaf.Sinc.Utils
{
    public static class ConsoleExtensions
    {
        public static void Time()
        {
            ("T:" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")).Info();
        }

        public static void Verbose(this string value)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine(value);
        }

        public static void Info(this string value)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine(value);
        }

        public static void Error(this string value)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine(value);
        }

        public static void Warn(this string value)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine(value);
        }
    }
}