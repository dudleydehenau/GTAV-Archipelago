using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace GTAVArchipelagoSetup
{
    internal sealed class InstallManifest
    {
        public string Version { get; set; } = "1";
        public List<string> GameFiles { get; set; } = new List<string>();
        public Dictionary<string, string> Installed { get; set; } = new Dictionary<string, string>();
    }

    internal sealed class Installer
    {
        public const string ModsFolder = "Archipelago";
        private const string ManifestName = "install.json";
        private static readonly string[] OurMods = { "MissionLauncher", "GameEnvironment", "GameTraps", "GameCollectibles", "GameArchipelago" };
        private static readonly string[] Skipped = { ".md", ".txt", ".pdb", ".xml", ".lnk" };
        private static readonly string[] LoaderFiles = { "dinput8.dll", "ScriptHookV.dll", "ScriptHookVDotNet.asi" };

        private readonly string _game;
        private readonly Action<string> _log;
        private static HttpClient _sharedHttp;

        public Installer(string game, Action<string> log)
        {
            _game = game;
            _log = log;
        }

        private static HttpClient Http
        {
            get
            {
                if (_sharedHttp != null) return _sharedHttp;
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("GTAVArchipelagoSetup/1.0");
                _sharedHttp = client;
                return client;
            }
        }

        public string ModsPath => Path.Combine(_game, ModsFolder);
        public string OfflinePath => Path.Combine(ModsPath, "_online_backup");

        public bool HasScriptHookV => File.Exists(Path.Combine(_game, "ScriptHookV.dll")) && File.Exists(Path.Combine(_game, "dinput8.dll"));

        public bool ModsDisabled => LoaderFiles.Any(f => File.Exists(Path.Combine(OfflinePath, f)));

        public async Task Install(IReadOnlyList<Component> components, bool keepSettings, Action<int> progress)
        {
            Directory.CreateDirectory(ModsPath);
            InstallManifest manifest = LoadManifest();
            MigrateScriptsFolder();
            string temp = Path.Combine(Path.GetTempPath(), "GTAVArchipelagoSetup-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                for (int i = 0; i < components.Count; i++)
                {
                    Component component = components[i];
                    progress(i * 100 / Math.Max(1, components.Count));
                    if (!component.IsConfigured)
                    {
                        _log($"{component.Name}: no download link set in sources.json, skipped.");
                        continue;
                    }
                    _log($"{component.Name}: looking for the latest release...");
                    Tuple<string, string> asset = await Resolve(component);
                    _log($"{component.Name}: downloading {asset.Item1}...");
                    string file = Path.Combine(temp, asset.Item1);
                    using (HttpResponseMessage response = await Http.GetAsync(asset.Item2))
                    {
                        response.EnsureSuccessStatusCode();
                        using (FileStream output = File.Create(file))
                            await response.Content.CopyToAsync(output);
                    }
                    int count = Deploy(component, file, keepSettings, manifest);
                    manifest.Installed[component.Name] = asset.Item1;
                    _log($"{component.Name}: {count} files installed.");
                }
                SetScriptsLocation(ModsFolder);
                LockConsole(true);
                SaveManifest(manifest);
                progress(100);
                _log("Done. ScriptHookVDotNet now loads the scripts from the \"" + ModsFolder + "\" folder.");
                if (!HasScriptHookV) _log("WARNING: ScriptHookV is missing. Install it with the ScriptHookV button (it cannot be downloaded automatically).");
            }
            finally
            {
                try
                {
                    Directory.Delete(temp, true);
                }
                catch
                {
                }
            }
        }

        private async Task<Tuple<string, string>> Resolve(Component component)
        {
            if (!string.IsNullOrWhiteSpace(component.Url))
            {
                string name = Path.GetFileName(new Uri(component.Url).LocalPath);
                return Tuple.Create(string.IsNullOrEmpty(name) ? component.Name + ".zip" : name, component.Url);
            }
            string json = await Http.GetStringAsync($"https://api.github.com/repos/{component.Repo}/releases?per_page=20");
            var releases = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<List<Dictionary<string, object>>>(json);
            var pattern = new Regex(string.IsNullOrEmpty(component.Asset) ? "\\.zip$" : component.Asset, RegexOptions.IgnoreCase);
            foreach (Dictionary<string, object> release in releases)
            {
                if (release.TryGetValue("draft", out object draft) && draft is bool d && d) continue;
                if (!component.Prerelease && release.TryGetValue("prerelease", out object pre) && pre is bool p && p) continue;
                if (!(release.TryGetValue("assets", out object assets) && assets is System.Collections.IEnumerable list) || assets is string) continue;
                foreach (object entry in list)
                {
                    if (!(entry is Dictionary<string, object> asset)) continue;
                    string name = asset["name"] as string;
                    if (name != null && pattern.IsMatch(name)) return Tuple.Create(name, (string)asset["browser_download_url"]);
                }
            }
            throw new InvalidOperationException($"No release file matching \"{component.Asset}\" in {component.Repo}.");
        }

        private int Deploy(Component component, string file, bool keepSettings, InstallManifest manifest)
        {
            string target = component.Target ?? "Mods";
            if (string.Equals(target, "World", StringComparison.OrdinalIgnoreCase))
            {
                InstallWorld(file);
                return 1;
            }
            bool game = string.Equals(target, "Game", StringComparison.OrdinalIgnoreCase);
            string destination = game ? _game : ModsPath;
            if (!file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
                return 1;
            }
            int count = 0;
            using (ZipArchive zip = ZipFile.OpenRead(file))
            {
                List<ZipArchiveEntry> entries = zip.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).ToList();
                string prefix = CommonPrefix(entries);
                foreach (ZipArchiveEntry entry in entries)
                {
                    string relative;
                    if (component.Files != null && component.Files.Count > 0)
                    {
                        if (!component.Files.Contains(entry.Name, StringComparer.OrdinalIgnoreCase)) continue;
                        relative = entry.Name;
                    }
                    else
                    {
                        relative = entry.FullName.Substring(prefix.Length).Replace('/', Path.DirectorySeparatorChar);
                        if (Skipped.Any(ext => relative.EndsWith(ext, StringComparison.OrdinalIgnoreCase))) continue;
                    }
                    string path = Path.Combine(destination, relative);
                    bool settings = entry.Name.Equals("config.json", StringComparison.OrdinalIgnoreCase)
                        || entry.Name.Equals("vehicles.json", StringComparison.OrdinalIgnoreCase)
                        || entry.Name.Equals("weapons.json", StringComparison.OrdinalIgnoreCase)
                        || entry.Name.Equals("ScriptHookVDotNet.ini", StringComparison.OrdinalIgnoreCase);
                    if (settings && keepSettings && File.Exists(path)) continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    entry.ExtractToFile(path, true);
                    count++;
                    if (game && !manifest.GameFiles.Contains(relative, StringComparer.OrdinalIgnoreCase)) manifest.GameFiles.Add(relative);
                }
            }
            return count;
        }

        private static string CommonPrefix(List<ZipArchiveEntry> entries)
        {
            if (entries.Count == 0) return string.Empty;
            string first = entries[0].FullName;
            int slash = first.IndexOf('/');
            if (slash < 0) return string.Empty;
            string folder = first.Substring(0, slash + 1);
            if (OurMods.Any(m => folder.Equals(m + "/", StringComparison.OrdinalIgnoreCase))) return string.Empty;
            return entries.All(e => e.FullName.StartsWith(folder, StringComparison.Ordinal)) ? folder : string.Empty;
        }

        private void InstallWorld(string file)
        {
            string worlds = Path.Combine(ModsPath, "apworld");
            Directory.CreateDirectory(worlds);
            File.Copy(file, Path.Combine(worlds, Path.GetFileName(file)), true);
            string archipelago = FindArchipelago();
            if (archipelago != null)
            {
                string custom = Path.Combine(archipelago, "custom_worlds");
                Directory.CreateDirectory(custom);
                File.Copy(file, Path.Combine(custom, Path.GetFileName(file)), true);
                _log($"APWorld copied to {custom}.");
            }
            else
            {
                _log($"Archipelago launcher not found: the APWorld was saved in {worlds}. Double-click it to install it.");
            }
        }

        public static string FindArchipelago()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Archipelago"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Archipelago"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Archipelago"),
            };
            return candidates.FirstOrDefault(c => File.Exists(Path.Combine(c, "ArchipelagoLauncher.exe")) || Directory.Exists(Path.Combine(c, "custom_worlds")));
        }

        private void MigrateScriptsFolder()
        {
            string scripts = Path.Combine(_game, "scripts");
            if (!Directory.Exists(scripts)) return;
            foreach (string mod in OurMods)
            {
                string dll = Path.Combine(scripts, mod + ".dll");
                string folder = Path.Combine(scripts, mod);
                if (File.Exists(dll))
                {
                    File.Copy(dll, Path.Combine(ModsPath, mod + ".dll"), true);
                    File.Delete(dll);
                    _log($"Moved {mod}.dll from scripts to {ModsFolder}.");
                }
                if (Directory.Exists(folder))
                {
                    CopyFolder(folder, Path.Combine(ModsPath, mod));
                    Directory.Delete(folder, true);
                    _log($"Moved the {mod} settings and saves from scripts to {ModsFolder}.");
                }
            }
            string lemon = Path.Combine(scripts, "LemonUI.SHVDN3.dll");
            if (File.Exists(lemon) && !File.Exists(Path.Combine(ModsPath, "LemonUI.SHVDN3.dll")))
                File.Copy(lemon, Path.Combine(ModsPath, "LemonUI.SHVDN3.dll"));
            if (Directory.EnumerateFileSystemEntries(scripts).Any())
                _log("Other scripts stay in the scripts folder: ScriptHookVDotNet no longer loads them while this setup is installed.");
        }

        private static void CopyFolder(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source))
            {
                string target = Path.Combine(destination, Path.GetFileName(file));
                if (!File.Exists(target)) File.Copy(file, target);
            }
            foreach (string folder in Directory.GetDirectories(source))
                CopyFolder(folder, Path.Combine(destination, Path.GetFileName(folder)));
        }

        public void SetScriptsLocation(string folder) => SetIniValue("ScriptsLocation", $"\"{folder}\"");

        private void SetIniValue(string key, string value)
        {
            string ini = Path.Combine(_game, "ScriptHookVDotNet.ini");
            var lines = File.Exists(ini) ? File.ReadAllLines(ini).ToList() : new List<string>();
            int index = lines.FindIndex(l =>
            {
                string line = l.TrimStart();
                return line.StartsWith(key, StringComparison.OrdinalIgnoreCase) && line.Substring(key.Length).TrimStart().StartsWith("=");
            });
            string text = $"{key}={value}";
            if (index >= 0) lines[index] = text;
            else lines.Add(text);
            File.WriteAllLines(ini, lines);
            _log($"ScriptHookVDotNet.ini: {text}");
        }

        public void LockConsole(bool locked)
        {
            SetIniValue("ConsoleKeyBinding", locked ? "None" : "F4");
            SetIniValue("ReloadKeyBinding", "None");
            string trainer = Path.Combine(_game, "NativeTrainer.asi");
            string parked = trainer + ".disabled";
            try
            {
                if (locked && File.Exists(trainer))
                {
                    if (File.Exists(parked)) File.Delete(parked);
                    File.Move(trainer, parked);
                    _log("NativeTrainer.asi disabled (renamed NativeTrainer.asi.disabled): its F4 menu is not allowed with Archipelago.");
                }
                else if (!locked && File.Exists(parked) && !File.Exists(trainer))
                {
                    File.Move(parked, trainer);
                    _log("NativeTrainer.asi restored.");
                }
            }
            catch (Exception ex)
            {
                _log("Could not move NativeTrainer.asi: " + ex.Message);
            }
        }

        public void InstallScriptHookV(string zipPath)
        {
            int count = 0;
            using (ZipArchive zip = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    if (!entry.Name.Equals("ScriptHookV.dll", StringComparison.OrdinalIgnoreCase)
                        && !entry.Name.Equals("dinput8.dll", StringComparison.OrdinalIgnoreCase)) continue;
                    entry.ExtractToFile(Path.Combine(_game, entry.Name), true);
                    count++;
                }
            }
            if (count < 2) throw new InvalidOperationException("This zip does not contain ScriptHookV.dll and dinput8.dll (take the zip from dev-c.com).");
            _log("ScriptHookV installed.");
        }

        public void DisableMods()
        {
            Directory.CreateDirectory(OfflinePath);
            foreach (string name in LoaderFiles)
            {
                string source = Path.Combine(_game, name);
                if (!File.Exists(source)) continue;
                string target = Path.Combine(OfflinePath, name);
                if (File.Exists(target)) File.Delete(target);
                File.Move(source, target);
            }
            _log("Mods disabled: ScriptHookV, its loader and ScriptHookVDotNet were moved aside. Turn BattlEye back on before GTA Online.");
        }

        public void EnableMods()
        {
            if (!Directory.Exists(OfflinePath)) return;
            foreach (string name in LoaderFiles)
            {
                string source = Path.Combine(OfflinePath, name);
                if (!File.Exists(source)) continue;
                string target = Path.Combine(_game, name);
                if (File.Exists(target)) File.Delete(target);
                File.Move(source, target);
            }
            _log("Mods enabled. Turn BattlEye off before playing.");
        }

        public void Uninstall(bool keepSaves)
        {
            InstallManifest manifest = LoadManifest();
            EnableMods();
            foreach (string relative in manifest.GameFiles)
            {
                string path = Path.Combine(_game, relative);
                if (relative.Equals("ScriptHookVDotNet.ini", StringComparison.OrdinalIgnoreCase)) continue;
                if (File.Exists(path)) File.Delete(path);
            }
            if (File.Exists(Path.Combine(_game, "ScriptHookVDotNet.ini")))
            {
                SetScriptsLocation("scripts");
                LockConsole(false);
            }
            if (Directory.Exists(ModsPath))
            {
                if (keepSaves)
                {
                    foreach (string file in Directory.GetFiles(ModsPath, "*.dll")) File.Delete(file);
                    string world = Path.Combine(ModsPath, "apworld");
                    if (Directory.Exists(world)) Directory.Delete(world, true);
                    _log($"Mods removed. Settings and saves kept in {ModsPath}.");
                }
                else
                {
                    Directory.Delete(ModsPath, true);
                    _log($"{ModsPath} deleted.");
                }
            }
            _log("Uninstalled. ScriptHookV itself was left in place.");
        }

        private InstallManifest LoadManifest()
        {
            string path = Path.Combine(ModsPath, ManifestName);
            try
            {
                if (File.Exists(path)) return new JavaScriptSerializer().Deserialize<InstallManifest>(File.ReadAllText(path)) ?? new InstallManifest();
            }
            catch
            {
            }
            return new InstallManifest();
        }

        private void SaveManifest(InstallManifest manifest)
        {
            File.WriteAllText(Path.Combine(ModsPath, ManifestName), new JavaScriptSerializer().Serialize(manifest));
        }
    }
}
