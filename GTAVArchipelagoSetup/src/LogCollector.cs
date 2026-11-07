using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace GTAVArchipelagoSetup
{
    internal static class LogCollector
    {
        private const long MaxFileSize = 20L * 1024 * 1024;
        private static readonly string[] RootPatterns = { "ScriptHookV*.log", "ScriptHookVDotNet*.log", "ScriptHookVDotNet.ini", "asiloader.log", "*.asi.log" };
        private static readonly string[] ModPatterns = { "*.log", "config.json", "state.json", "install.json", "collected.json" };
        private static readonly string[] Excluded = { "connection.json" };

        public static string Collect(string game, string setupLog)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (!Directory.Exists(desktop)) desktop = Path.GetTempPath();
            string zip = Path.Combine(desktop, $"GTAV-Archipelago-logs-{DateTime.Now:yyyyMMdd-HHmm}.zip");
            if (File.Exists(zip)) File.Delete(zip);
            var added = new List<string>();
            using (ZipArchive archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                bool gameValid = GameLocator.IsGameFolder(game);
                if (gameValid)
                {
                    foreach (string pattern in RootPatterns)
                        foreach (string file in SafeFiles(game, pattern, SearchOption.TopDirectoryOnly))
                            Add(archive, file, "game/" + Path.GetFileName(file), added);
                    string mods = Path.Combine(game, Installer.ModsFolder);
                    if (Directory.Exists(mods))
                    {
                        foreach (string pattern in ModPatterns)
                            foreach (string file in SafeFiles(mods, pattern, SearchOption.AllDirectories))
                            {
                                if (Excluded.Contains(Path.GetFileName(file), StringComparer.OrdinalIgnoreCase)) continue;
                                if (file.IndexOf("_online_backup", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                                Add(archive, file, "mods/" + Relative(mods, file), added);
                            }
                        string saves = Path.Combine(mods, "GameArchipelago", "saves");
                        foreach (string file in SafeFiles(saves, "*.json", SearchOption.TopDirectoryOnly))
                            Add(archive, file, "mods/GameArchipelago/saves/" + Path.GetFileName(file), added);
                    }
                }
                WriteText(archive, "summary.txt", Summary(game, gameValid, added));
                WriteText(archive, "setup.log", setupLog ?? string.Empty);
            }
            return zip;
        }

        private static string Summary(string game, bool valid, List<string> added)
        {
            var text = new StringBuilder();
            text.AppendLine("GTA V Archipelago logs - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            text.AppendLine("Windows: " + Environment.OSVersion.VersionString + (Environment.Is64BitOperatingSystem ? " (64-bit)" : string.Empty));
            text.AppendLine("Game folder: " + (string.IsNullOrEmpty(game) ? "(none)" : game) + (valid ? string.Empty : " (no GTA5.exe)"));
            if (valid)
            {
                text.AppendLine("Game version: " + (GameLocator.Version(game) ?? "unknown") + ", platform: " + GameLocator.Detect(game));
                var installer = new Installer(game, _ => { });
                text.AppendLine("ScriptHookV installed: " + installer.HasScriptHookV + ", source: " + (installer.ScriptHookVSource ?? "unknown"));
                text.AppendLine("Mods turned off for GTA Online: " + installer.ModsDisabled);
                text.AppendLine();
                text.AppendLine("Files next to GTA5.exe:");
                try
                {
                    foreach (FileInfo file in new DirectoryInfo(game).GetFiles().OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
                        text.AppendLine($"  {file.Name,-40} {file.Length,12:N0}  {file.LastWriteTime:yyyy-MM-dd HH:mm}");
                }
                catch (Exception ex)
                {
                    text.AppendLine("  (could not list: " + ex.Message + ")");
                }
                string mods = Path.Combine(game, Installer.ModsFolder);
                text.AppendLine();
                text.AppendLine("Files in " + Installer.ModsFolder + ":");
                try
                {
                    if (Directory.Exists(mods))
                        foreach (FileInfo file in new DirectoryInfo(mods).GetFiles().OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
                            text.AppendLine($"  {file.Name,-40} {file.Length,12:N0}  {file.LastWriteTime:yyyy-MM-dd HH:mm}");
                    else text.AppendLine("  (missing)");
                }
                catch (Exception ex)
                {
                    text.AppendLine("  (could not list: " + ex.Message + ")");
                }
            }
            text.AppendLine();
            text.AppendLine("Story mode saves (Documents\\Rockstar Games\\GTA V\\Profiles):");
            try
            {
                string profiles = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Rockstar Games", "GTA V", "Profiles");
                if (!Directory.Exists(profiles)) text.AppendLine("  (no Profiles folder)");
                else
                    foreach (string profile in Directory.GetDirectories(profiles))
                    {
                        text.AppendLine("  " + Path.GetFileName(profile));
                        foreach (FileInfo file in new DirectoryInfo(profile).GetFiles().OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
                            text.AppendLine($"    {file.Name,-30} {file.Length,12:N0}  {file.LastWriteTime:yyyy-MM-dd HH:mm}");
                    }
            }
            catch (Exception ex)
            {
                text.AppendLine("  (could not list: " + ex.Message + ")");
            }
            text.AppendLine();
            text.AppendLine("Collected files:");
            foreach (string name in added) text.AppendLine("  " + name);
            return text.ToString();
        }

        private static IEnumerable<string> SafeFiles(string folder, string pattern, SearchOption option)
        {
            try
            {
                if (!Directory.Exists(folder)) return Array.Empty<string>();
                return Directory.GetFiles(folder, pattern, option);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static string Relative(string root, string file)
        {
            string prefix = root.TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            string relative = file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? file.Substring(prefix.Length) : Path.GetFileName(file);
            return relative.Replace('\\', '/');
        }

        private static void Add(ZipArchive archive, string file, string name, List<string> added)
        {
            if (added.Contains(name, StringComparer.OrdinalIgnoreCase)) return;
            try
            {
                var info = new FileInfo(file);
                if (!info.Exists || info.Length > MaxFileSize) return;
                using (var source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (Stream target = archive.CreateEntry(name, CompressionLevel.Optimal).Open())
                    source.CopyTo(target);
                added.Add(name);
            }
            catch
            {
            }
        }

        private static void WriteText(ZipArchive archive, string name, string content)
        {
            using (var writer = new StreamWriter(archive.CreateEntry(name, CompressionLevel.Optimal).Open(), new UTF8Encoding(false)))
                writer.Write(content);
        }
    }
}
