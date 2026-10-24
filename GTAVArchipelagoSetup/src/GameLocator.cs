using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace GTAVArchipelagoSetup
{
    internal enum Platform
    {
        Unknown,
        Rockstar,
        Steam,
        Epic,
    }

    internal static class GameLocator
    {
        public const string ExpectedVersion = "1.0.3725";

        public static string Find()
        {
            foreach (string candidate in Candidates())
            {
                if (IsGameFolder(candidate)) return candidate;
            }
            return null;
        }

        public static bool IsGameFolder(string folder) =>
            !string.IsNullOrWhiteSpace(folder) && File.Exists(Path.Combine(folder, "GTA5.exe"));

        public static bool IsEnhanced(string folder) =>
            !string.IsNullOrWhiteSpace(folder) && File.Exists(Path.Combine(folder, "GTA5_Enhanced.exe"));

        public static string Version(string folder)
        {
            try
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(Path.Combine(folder, "GTA5.exe"));
                return $"{info.FileMajorPart}.{info.FileMinorPart}.{info.FileBuildPart}";
            }
            catch
            {
                return null;
            }
        }

        public static Platform Detect(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return Platform.Unknown;
            if (folder.IndexOf("steamapps", StringComparison.OrdinalIgnoreCase) >= 0 || File.Exists(Path.Combine(folder, "steam_api64.dll")))
                return Platform.Steam;
            if (File.Exists(Path.Combine(folder, "EOSSDK-Win64-Shipping.dll")) || folder.IndexOf("Epic Games", StringComparison.OrdinalIgnoreCase) >= 0)
                return Platform.Epic;
            return Platform.Rockstar;
        }

        private static IEnumerable<string> Candidates()
        {
            string rockstar = ReadRegistry(@"SOFTWARE\WOW6432Node\Rockstar Games\Grand Theft Auto V", "InstallFolder")
                ?? ReadRegistry(@"SOFTWARE\Rockstar Games\Grand Theft Auto V", "InstallFolder");
            if (rockstar != null) yield return rockstar;
            string steamGame = ReadRegistry(@"SOFTWARE\WOW6432Node\Rockstar Games\GTAV", "InstallFolderSteam");
            if (steamGame != null) yield return steamGame.EndsWith("GTAV", StringComparison.OrdinalIgnoreCase) ? Path.GetDirectoryName(steamGame) : steamGame;
            string epic = ReadRegistry(@"SOFTWARE\WOW6432Node\Rockstar Games\GTAV", "InstallFolderEpic");
            if (epic != null) yield return epic;
            foreach (string library in SteamLibraries())
                yield return Path.Combine(library, "steamapps", "common", "Grand Theft Auto V");
            foreach (string folder in EpicFolders())
                yield return folder;
            foreach (DriveInfo drive in SafeDrives())
            {
                yield return Path.Combine(drive.RootDirectory.FullName, "Program Files", "Rockstar Games", "Grand Theft Auto V");
                yield return Path.Combine(drive.RootDirectory.FullName, "Program Files (x86)", "Steam", "steamapps", "common", "Grand Theft Auto V");
                yield return Path.Combine(drive.RootDirectory.FullName, "SteamLibrary", "steamapps", "common", "Grand Theft Auto V");
                yield return Path.Combine(drive.RootDirectory.FullName, "Program Files", "Epic Games", "GTAV");
                yield return Path.Combine(drive.RootDirectory.FullName, "Games", "Grand Theft Auto V");
            }
        }

        private static IEnumerable<DriveInfo> SafeDrives()
        {
            try
            {
                return DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady).ToList();
            }
            catch
            {
                return Enumerable.Empty<DriveInfo>();
            }
        }

        private static IEnumerable<string> SteamLibraries()
        {
            var result = new List<string>();
            string steam = ReadRegistry(@"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath") ?? ReadRegistry(@"SOFTWARE\Valve\Steam", "InstallPath");
            if (steam == null) return result;
            result.Add(steam);
            string vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            try
            {
                if (File.Exists(vdf))
                {
                    foreach (Match match in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                        result.Add(match.Groups[1].Value.Replace("\\\\", "\\"));
                }
            }
            catch
            {
            }
            return result.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> EpicFolders()
        {
            var result = new List<string>();
            string manifests = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests");
            try
            {
                if (!Directory.Exists(manifests)) return result;
                foreach (string file in Directory.GetFiles(manifests, "*.item"))
                {
                    string text = File.ReadAllText(file);
                    if (text.IndexOf("Grand Theft Auto V", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    Match match = Regex.Match(text, "\"InstallLocation\"\\s*:\\s*\"([^\"]+)\"");
                    if (match.Success) result.Add(match.Groups[1].Value.Replace("\\\\", "\\"));
                }
            }
            catch
            {
            }
            return result;
        }

        private static string ReadRegistry(string key, string value)
        {
            foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (RegistryKey root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (RegistryKey sub = root.OpenSubKey(key))
                    {
                        if (sub?.GetValue(value) is string text && !string.IsNullOrWhiteSpace(text)) return text.TrimEnd('\\');
                    }
                }
                catch
                {
                }
            }
            return null;
        }
    }
}
