using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace GameArchipelago.Core
{
    internal static class History
    {
        private const long MaxSize = 8 * 1024 * 1024;
        private static readonly object Sync = new object();
        private static string _directory;
        private static string _path;

        public static void Init(string directory)
        {
            _directory = Path.Combine(directory, "history");
        }

        public static void Open(string key)
        {
            if (_directory == null || string.IsNullOrEmpty(key)) return;
            try
            {
                Directory.CreateDirectory(_directory);
                string path = Path.Combine(_directory, key + ".log");
                var info = new FileInfo(path);
                if (info.Exists && info.Length > MaxSize)
                {
                    string old = Path.Combine(_directory, key + ".old.log");
                    if (File.Exists(old)) File.Delete(old);
                    File.Move(path, old);
                }
                _path = path;
            }
            catch (Exception ex)
            {
                Log.Error("Could not open the history.", ex);
                _path = null;
            }
        }

        public static string CurrentPath => _path;

        public static void Write(string kind, params object[] fields)
        {
            string path = _path;
            if (path == null) return;
            var line = new StringBuilder();
            line.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append(" | ").Append(kind.PadRight(10));
            for (int i = 0; i + 1 < fields.Length; i += 2)
            {
                line.Append(" | ").Append(fields[i]).Append('=');
                line.Append(Format(fields[i + 1]));
            }
            line.Append(Environment.NewLine);
            lock (Sync)
            {
                try
                {
                    File.AppendAllText(path, line.ToString(), Encoding.UTF8);
                }
                catch
                {
                }
            }
        }

        private static string Format(object value)
        {
            switch (value)
            {
                case null:
                    return "null";
                case string text:
                    return "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ") + "\"";
                case IEnumerable<long> numbers:
                    return "[" + string.Join(",", numbers) + "]";
                case IEnumerable<string> texts:
                    return "[" + string.Join(",", texts.Select(Format)) + "]";
                case bool flag:
                    return flag ? "true" : "false";
                default:
                    return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public static string Versions()
        {
            var parts = new List<string>();
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                if (name == "GameArchipelago" || name == "MissionLauncher" || name == "GameEnvironment" || name == "GameTraps"
                    || name == "GameCollectibles" || name.StartsWith("ScriptHookVDotNet") || name.StartsWith("LemonUI")
                    || name == "Archipelago.MultiClient.Net")
                    parts.Add($"{name} {assembly.GetName().Version}");
            }
            parts.Sort(StringComparer.Ordinal);
            return string.Join(", ", parts);
        }

        public static string ExportReport(string modsDirectory, string saveKey)
        {
            string reports = Path.Combine(modsDirectory, "GameArchipelago", "reports");
            Directory.CreateDirectory(reports);
            string file = Path.Combine(reports, $"report-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
            string game = Directory.GetParent(modsDirectory)?.FullName ?? modsDirectory;
            var sources = new List<Tuple<string, string>>
            {
                Tuple.Create(Path.Combine(game, "ScriptHookVDotNet.log"), "ScriptHookVDotNet.log"),
                Tuple.Create(Path.Combine(game, "ScriptHookV.log"), "ScriptHookV.log"),
            };
            foreach (string mod in new[] { "GameArchipelago", "MissionLauncher", "GameEnvironment", "GameTraps", "GameCollectibles" })
            {
                string folder = Path.Combine(modsDirectory, mod);
                if (!Directory.Exists(folder)) continue;
                foreach (string path in Directory.GetFiles(folder))
                {
                    string name = Path.GetFileName(path);
                    if (name.Equals("connection.json", StringComparison.OrdinalIgnoreCase)) continue;
                    if (name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) continue;
                    sources.Add(Tuple.Create(path, mod + "/" + name));
                }
            }
            if (!string.IsNullOrEmpty(saveKey))
            {
                sources.Add(Tuple.Create(Path.Combine(modsDirectory, "GameArchipelago", "saves", saveKey + ".json"), "GameArchipelago/saves/" + saveKey + ".json"));
                sources.Add(Tuple.Create(Path.Combine(modsDirectory, "GameArchipelago", "history", saveKey + ".log"), "GameArchipelago/history/" + saveKey + ".log"));
                sources.Add(Tuple.Create(Path.Combine(modsDirectory, "GameArchipelago", "history", saveKey + ".old.log"), "GameArchipelago/history/" + saveKey + ".old.log"));
            }
            using (ZipArchive zip = ZipFile.Open(file, ZipArchiveMode.Create))
            {
                foreach (Tuple<string, string> source in sources)
                {
                    if (!File.Exists(source.Item1)) continue;
                    try
                    {
                        ZipArchiveEntry entry = zip.CreateEntry(source.Item2);
                        using (Stream input = new FileStream(source.Item1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (Stream output = entry.Open())
                            input.CopyTo(output);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"Report: could not add {source.Item1}: {ex.Message}");
                    }
                }
                ZipArchiveEntry info = zip.CreateEntry("versions.txt");
                using (var writer = new StreamWriter(info.Open(), Encoding.UTF8))
                {
                    writer.WriteLine(Versions());
                    writer.WriteLine("Created " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                }
            }
            return file;
        }
    }
}
