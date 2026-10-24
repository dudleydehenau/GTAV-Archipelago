using System;
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
        public GlobalsConfig Globals { get; set; } = new GlobalsConfig();

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
                if (config.Globals == null) config.Globals = new GlobalsConfig();
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
        public int FailReason { get; set; } = 80513;
        public int ReplayRequest { get; set; } = 80527;
        public int ReplayFlags { get; set; } = 80529;
        public int ReplayReturnPosition { get; set; } = 80532;
        public int MissionTable { get; set; } = 93192;
        public int StatDefinitions { get; set; } = 65235;
        public int StatDefinitionCount { get; set; } = 913;
        public int ActiveStats { get; set; } = 77106;
        public int ActiveStatCount { get; set; } = 77105;
        public int ActiveStatMax { get; set; } = 16;
        public int MissionPassedFlag { get; set; } = 65004;
        public int RampageRecords { get; set; } = 117263;
        public int RampageCount { get; set; } = 5;
        public int StoryCompletion { get; set; } = 124326;
        public int StoryCompletionCount { get; set; } = 100;
        public int StrangersCompletion { get; set; } = 133485;
        public int StrangersCompletionCount { get; set; } = 63;
    }
}
