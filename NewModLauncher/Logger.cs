using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace NewModLauncher
{
    public static class Logger
    {
        private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "launcher.log");
        private static readonly object LockObj = new();

        public static void Info(string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0) =>
            Log("INFO", message, file, member, line);

        public static void Warn(string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0) =>
            Log("WARN", message, file, member, line);

        public static void Error(string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0) =>
            Log("ERROR", message, file, member, line);

        public static void Debug(string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0) =>
            Log("DEBUG", message, file, member, line);

        public static void Log(string level, string message, string file = "", string member = "", int line = 0)
        {
            lock (LockObj)
            {
                var fileName = Path.GetFileName(file);
                string logEntry = $"[NewModLauncher] [{level}] [{fileName}:{member}:{line}]: {message}{Environment.NewLine}";
                File.AppendAllText(LogPath, logEntry);
            }
        }
    }
}
