using System;
using System.IO;
using System.Text;

namespace GameCollectibles.Core
{
    internal static class Log
    {
        private const long MaxSize = 2 * 1024 * 1024;
        private static readonly object Sync = new object();
        private static string _path;

        public static bool Verbose { get; set; }

        public static void Init(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
                _path = Path.Combine(directory, "GameCollectibles.log");
                var info = new FileInfo(_path);
                if (info.Exists && info.Length > MaxSize)
                {
                    string old = Path.Combine(directory, "GameCollectibles.old.log");
                    if (File.Exists(old)) File.Delete(old);
                    File.Move(_path, old);
                }
                File.AppendAllText(_path,
                    $"{System.Environment.NewLine}=== GameCollectibles {typeof(Log).Assembly.GetName().Version} - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={System.Environment.NewLine}",
                    Encoding.UTF8);
            }
            catch
            {
                _path = null;
            }
        }

        public static void Info(string message) => Write("INFO ", message);
        public static void Warn(string message) => Write("WARN ", message);
        public static void Error(string message, Exception ex) => Write("ERROR", message + System.Environment.NewLine + ex);

        public static void Debug(string message)
        {
            if (Verbose) Write("DEBUG", message);
        }

        private static void Write(string level, string message)
        {
            if (_path == null) return;
            lock (Sync)
            {
                try
                {
                    File.AppendAllText(_path, $"[{DateTime.Now:HH:mm:ss.fff}] {level} {message}{System.Environment.NewLine}", Encoding.UTF8);
                }
                catch
                {
                }
            }
        }
    }
}
