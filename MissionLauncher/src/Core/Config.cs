using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace MissionLauncher.Core
{
    internal sealed class LauncherConfig
    {
        public bool EnableMenu { get; set; }
        public string MenuKey { get; set; } = "F7";
        public bool VerboseLog { get; set; }
        public bool OnlyCompletedMissions { get; set; }
        public string MenuOrder { get; set; } = "Story";
        public bool AutoConfirmRestorePoint { get; set; } = true;
        public bool RestorePointKeyFallback { get; set; } = true;
        public string Markers { get; set; } = "All";
        public bool MarkerAutoStart { get; set; } = true;
        public float MarkerRadius { get; set; } = 1.5f;

        public static LauncherConfig Load(string directory)
        {
            string path = Path.Combine(directory, "config.json");
            if (!File.Exists(path))
            {
                Log.Info("config.json not found, using defaults.");
                return new LauncherConfig();
            }
            try
            {
                LauncherConfig config = new JavaScriptSerializer().Deserialize<LauncherConfig>(File.ReadAllText(path)) ?? new LauncherConfig();
                return config;
            }
            catch (Exception ex)
            {
                Log.Error("config.json is invalid, using defaults.", ex);
                return new LauncherConfig();
            }
        }
    }

    internal sealed class GlobalsConfig
    {
        public int Build { get; set; }
        public int FailReason { get; set; }
        public int ReplayRequest { get; set; }
        public int ReplayFlags { get; set; }
        public int ReplayReturnPosition { get; set; }
        public int MissionTable { get; set; }
        public int StatDefinitions { get; set; }
        public int StatDefinitionCount { get; set; } = 913;
        public int ActiveStats { get; set; }
        public int ActiveStatCount { get; set; }
        public int ActiveStatMax { get; set; } = 16;
        public int MissionPassedFlag { get; set; }
        public int RampageRecords { get; set; }
        public int RampageCount { get; set; } = 5;
        public int StoryCompletion { get; set; }
        public int StoryCompletionCount { get; set; } = 100;
        public int StrangersCompletion { get; set; }
        public int StrangersCompletionCount { get; set; } = 63;

        private static readonly GlobalsConfig[] Profiles =
        {
            new GlobalsConfig
            {
                Build = 3889,
                FailReason = 80570,
                ReplayRequest = 80584,
                ReplayFlags = 80586,
                ReplayReturnPosition = 80589,
                MissionTable = 93249,
                StatDefinitions = 65288,
                ActiveStats = 77159,
                ActiveStatCount = 77158,
                MissionPassedFlag = 65057,
                RampageRecords = 117322,
                StoryCompletion = 124387,
                StrangersCompletion = 133546,
            },
            new GlobalsConfig
            {
                Build = 3725,
                FailReason = 80513,
                ReplayRequest = 80527,
                ReplayFlags = 80529,
                ReplayReturnPosition = 80532,
                MissionTable = 93192,
                StatDefinitions = 65235,
                ActiveStats = 77106,
                ActiveStatCount = 77105,
                MissionPassedFlag = 65004,
                RampageRecords = 117263,
                StoryCompletion = 124326,
                StrangersCompletion = 133485,
            },
        };

        public static GlobalsConfig[] Candidates(int build)
        {
            var list = new List<GlobalsConfig>(Profiles);
            list.Sort((a, b) => (b.Build == build).CompareTo(a.Build == build));
            return list.ToArray();
        }
    }
}
