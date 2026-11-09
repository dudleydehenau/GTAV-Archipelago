using System;
using System.Collections.Generic;
using System.IO;
using GTA;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal sealed class SaveCleaner
    {
        private const int Delay = 3000;
        private const int Fallback = 60000;
        private readonly Dictionary<string, DateTime> _baseline;
        private readonly FileSystemWatcher _watcher;
        private volatile bool _changed;
        private int _next;

        public SaveCleaner()
        {
            _baseline = Snapshot();
            try
            {
                if (Directory.Exists(ProfilesFolder))
                {
                    _watcher = new FileSystemWatcher(ProfilesFolder, "SGTA5*")
                    {
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                    };
                    _watcher.Created += (s, e) => _changed = true;
                    _watcher.Changed += (s, e) => _changed = true;
                    _watcher.Renamed += (s, e) => _changed = true;
                    _watcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception ex)
            {
                _watcher = null;
                Log.Warn("Could not watch the save folder, it is checked every minute instead: " + ex.Message);
            }
        }

        public static string ProfilesFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Rockstar Games", "GTA V", "Profiles");

        public void Tick(bool active)
        {
            if (!active || Game.GameTime < _next) return;
            if (_watcher != null && !_changed)
            {
                _next = Game.GameTime + Delay;
                return;
            }
            if (Function.Call<bool>(Hash.IS_AUTO_SAVE_IN_PROGRESS))
            {
                _next = Game.GameTime + Delay;
                return;
            }
            _changed = false;
            _next = Game.GameTime + (_watcher != null ? Delay : Fallback);
            Remove("written during this session");
        }

        public void Remove(string reason)
        {
            foreach (KeyValuePair<string, DateTime> file in Snapshot())
            {
                if (_baseline.TryGetValue(file.Key, out DateTime written) && written == file.Value) continue;
                try
                {
                    File.Delete(file.Key);
                    _baseline.Remove(file.Key);
                    Log.Info($"Save removed ({reason}): {Path.GetFileName(Path.GetDirectoryName(file.Key))}\\{Path.GetFileName(file.Key)}.");
                }
                catch (Exception ex)
                {
                    Log.Warn($"Could not remove {file.Key}: {ex.Message}");
                }
            }
        }

        private static Dictionary<string, DateTime> Snapshot()
        {
            var files = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string root = ProfilesFolder;
                if (!Directory.Exists(root)) return files;
                foreach (string profile in Directory.GetDirectories(root))
                    foreach (string file in Directory.GetFiles(profile, "SGTA5*"))
                        files[file] = File.GetLastWriteTimeUtc(file);
            }
            catch (Exception ex)
            {
                Log.Warn("Could not list the save files: " + ex.Message);
            }
            return files;
        }
    }
}
