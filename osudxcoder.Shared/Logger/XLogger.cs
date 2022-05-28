using System;

namespace osudxcoder.Shared.Logger
{
    public static class XLogger
    {
        private static void Log(string value, string type, ConsoleColor color = ConsoleColor.Gray)
        {
            var time = DateTime.Now.ToString("HH:mm:ss tt");
            
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{time}] ");

            Console.ForegroundColor = color;
            Console.Write($"[{type}] {value}\n");
            Console.ResetColor();
        }

        public static void Message(string value) => Log(value, nameof(LogType.MESSAGE));
        public static void Info(string value) => Log(value, nameof(LogType.INFO), ConsoleColor.Cyan);
        public static void Debug(string value) => Log(value, nameof(LogType.DEBUG), ConsoleColor.Magenta);
        public static void System(string value) => Log(value, nameof(LogType.SYSTEM), ConsoleColor.Green);
        public static void Warning(string value) => Log(value, nameof(LogType.WARNING), ConsoleColor.Yellow);
        public static void Error(string value) => Log(value, nameof(LogType.ERROR), ConsoleColor.Red);
    }
}