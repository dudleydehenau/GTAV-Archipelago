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
    internal sealed class DownloadsBlockedException : Exception
    {
        public DownloadsBlockedException(IReadOnlyList<string> names, IReadOnlyList<string> links)
            : base("GitHub did not answer for " + string.Join(", ", names) + ".")
        {
            Names = names;
            Links = links;
        }

        public IReadOnlyList<string> Names { get; }

        public IReadOnlyList<string> Links { get; }
    }

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

        private static HttpClient _directHttp;
        private static bool _preferDirect;

        private static HttpClient Http => _sharedHttp ?? (_sharedHttp = CreateClient(true));

        private static HttpClient Direct => _directHttp ?? (_directHttp = CreateClient(false));

        private static HttpClient CreateClient(bool systemProxy)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var handler = new HttpClientHandler { UseProxy = systemProxy, AllowAutoRedirect = true };
            var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(systemProxy ? 5 : 2) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GTAVArchipelagoSetup/1.0");
            return client;
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
            var failedNames = new List<string>();
            var failedLinks = new List<string>();
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
                    Tuple<string, string> file = await Fetch(component, temp, failedNames, failedLinks);
                    if (file == null) continue;
                    int count = Deploy(component, file.Item2, keepSettings, manifest);
                    manifest.Installed[component.Name] = file.Item1;
                    _log($"{component.Name}: {count} files installed.");
                }
                if (failedNames.Count > 0)
                {
                    SetScriptsLocation(ModsFolder);
                    SaveManifest(manifest);
                    throw new DownloadsBlockedException(failedNames, failedLinks);
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

        private static readonly Regex ReleaseLink = new Regex(@"^https://github\.com/([^/]+/[^/]+)/releases/download/([^/]+)/([^/?#]+)", RegexOptions.IgnoreCase);
        private static bool _preferApi;

        private async Task<Tuple<string, string>> Fetch(Component component, string temp, List<string> failedNames, List<string> failedLinks)
        {
            Tuple<string, string> asset = null;
            string apiLink = null;
            string error = null;
            var routes = new List<Tuple<bool, bool>>
            {
                Tuple.Create(_preferDirect, _preferApi),
                Tuple.Create(_preferDirect, !_preferApi),
                Tuple.Create(!_preferDirect, _preferApi),
                Tuple.Create(!_preferDirect, !_preferApi),
            };
            for (int attempt = 0; attempt < routes.Count; attempt++)
            {
                bool direct = routes[attempt].Item1;
                bool api = routes[attempt].Item2;
                HttpClient client = direct ? Direct : Http;
                try
                {
                    if (asset == null)
                    {
                        _log($"{component.Name}: looking for the latest release...");
                        Tuple<string, string, string> found = await Resolve(component, client);
                        asset = Tuple.Create(found.Item1, found.Item2);
                        apiLink = found.Item3;
                    }
                    if (api && apiLink == null) apiLink = await ApiLink(asset.Item1, asset.Item2, client);
                    if (api && apiLink == null) continue;
                    string how = (api ? "through the GitHub API" : string.Empty) + (direct ? (api ? ", " : string.Empty) + "direct connection" : string.Empty);
                    _log($"{component.Name}: downloading {asset.Item1}{(how.Length > 0 ? $" ({how})" : string.Empty)}...");
                    string file = Path.Combine(temp, asset.Item1);
                    using (var request = new HttpRequestMessage(HttpMethod.Get, api ? apiLink : asset.Item2))
                    {
                        if (api) request.Headers.Accept.ParseAdd("application/octet-stream");
                        using (HttpResponseMessage response = await client.SendAsync(request))
                        {
                            response.EnsureSuccessStatusCode();
                            using (FileStream output = File.Create(file))
                                await response.Content.CopyToAsync(output);
                        }
                    }
                    if (api != _preferApi || direct != _preferDirect)
                    {
                        _preferApi = api;
                        _preferDirect = direct;
                        _log($"This route works ({(how.Length > 0 ? how : "normal link")}): the setup keeps using it.");
                    }
                    return Tuple.Create(asset.Item1, file);
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is WebException || ex is IOException)
                {
                    error = ex.GetBaseException().Message;
                    if (attempt < routes.Count - 1)
                    {
                        _log($"{component.Name}: GitHub did not answer ({error}), trying another way...");
                        if (attempt >= 1) await Task.Delay(attempt * 3000);
                    }
                }
            }
            string local = FindInDownloads(component, asset?.Item1);
            if (local != null)
            {
                _log($"{component.Name}: GitHub did not answer, using {Path.GetFileName(local)} from your Downloads folder.");
                string copy = Path.Combine(temp, Path.GetFileName(local));
                File.Copy(local, copy, true);
                return Tuple.Create(Path.GetFileName(local), copy);
            }
            _log($"{component.Name}: GitHub did not answer ({error}).");
            failedNames.Add(component.Name);
            string link = asset?.Item2 ?? (!string.IsNullOrWhiteSpace(component.Url) ? component.Url : $"https://github.com/{component.Repo}/releases/latest");
            if (!failedLinks.Contains(link)) failedLinks.Add(link);
            return null;
        }

        private static async Task<string> ApiLink(string name, string browserLink, HttpClient client)
        {
            Match match = ReleaseLink.Match(browserLink ?? string.Empty);
            if (!match.Success) return null;
            string json = await client.GetStringAsync($"https://api.github.com/repos/{match.Groups[1].Value}/releases/tags/{match.Groups[2].Value}");
            var release = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<Dictionary<string, object>>(json);
            if (!(release.TryGetValue("assets", out object assets) && assets is System.Collections.IEnumerable list) || assets is string) return null;
            foreach (object entry in list)
                if (entry is Dictionary<string, object> asset && string.Equals(asset["name"] as string, name, StringComparison.OrdinalIgnoreCase))
                    return asset["url"] as string;
            return null;
        }

        private static string FindInDownloads(Component component, string exactName)
        {
            string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (!Directory.Exists(downloads)) return null;
            if (!string.IsNullOrEmpty(exactName))
            {
                string exact = Path.Combine(downloads, exactName);
                if (File.Exists(exact)) return exact;
            }
            if (!string.IsNullOrWhiteSpace(component.Url))
            {
                string named = Path.Combine(downloads, Path.GetFileName(new Uri(component.Url).LocalPath));
                if (File.Exists(named)) return named;
            }
            var pattern = new Regex(string.IsNullOrEmpty(component.Asset) ? "^" + Regex.Escape(component.Name) + ".*\\.zip$" : component.Asset, RegexOptions.IgnoreCase);
            try
            {
                return new DirectoryInfo(downloads).GetFiles()
                    .Where(f => pattern.IsMatch(f.Name))
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .Select(f => f.FullName)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private async Task<Tuple<string, string, string>> Resolve(Component component, HttpClient client)
        {
            if (!string.IsNullOrWhiteSpace(component.Url))
            {
                string name = Path.GetFileName(new Uri(component.Url).LocalPath);
                return Tuple.Create(string.IsNullOrEmpty(name) ? component.Name + ".zip" : name, component.Url, (string)null);
            }
            string json = await client.GetStringAsync($"https://api.github.com/repos/{component.Repo}/releases?per_page=20");
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
                    if (name != null && pattern.IsMatch(name)) return Tuple.Create(name, (string)asset["browser_download_url"], asset["url"] as string);
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

        public string InstallScriptHookV(string zipPath)
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
            if (count < 2) throw new InvalidOperationException("This zip does not contain ScriptHookV.dll and dinput8.dll. Take the zip from the ScriptHookV page on dev-c.com.");
            string name = Path.GetFileName(zipPath);
            Directory.CreateDirectory(ModsPath);
            InstallManifest manifest = LoadManifest();
            manifest.Installed["ScriptHookV"] = name;
            SaveManifest(manifest);
            _log($"ScriptHookV installed from {name}.");
            return name;
        }

        public string ScriptHookVSource
        {
            get
            {
                InstallManifest manifest = LoadManifest();
                return manifest.Installed.TryGetValue("ScriptHookV", out string name) ? name : null;
            }
        }

        public static IReadOnlyList<int> BuildsIn(string fileName)
        {
            var builds = new List<int>();
            if (string.IsNullOrEmpty(fileName)) return builds;
            foreach (Match match in Regex.Matches(fileName, @"(?<!\d)(\d{3,4})\.\d+"))
                if (int.TryParse(match.Groups[1].Value, out int build)) builds.Add(build);
            return builds;
        }

        public static string FindScriptHookVZip()
        {
            string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            try
            {
                if (!Directory.Exists(downloads)) return null;
                return new DirectoryInfo(downloads).GetFiles("ScriptHookV*.zip")
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .Select(f => f.FullName)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
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
