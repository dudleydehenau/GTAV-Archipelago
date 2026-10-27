using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace GameArchipelago.Core
{
    internal sealed class ClientConfig
    {
        private string _path;

        public bool EnableMenu { get; set; } = true;
        public string MenuKey { get; set; } = "F6";
        public bool VerboseLog { get; set; }
        public bool AutoConnect { get; set; } = true;
        public bool ShowReceived { get; set; } = true;
        public bool ShowSent { get; set; } = true;
        public bool ShowChat { get; set; } = true;
        public bool ShowHints { get; set; } = true;
        public int NotificationDelay { get; set; } = 900;
        public int LogSize { get; set; } = 80;
        public bool OnlineGuard { get; set; } = true;
        public List<string> StarterWeapons { get; set; } = new List<string>
        {
            "WEAPON_PISTOL50", "WEAPON_COMBATPISTOL", "WEAPON_APPISTOL", "WEAPON_HEAVYPISTOL", "WEAPON_MICROSMG",
            "WEAPON_SMG", "WEAPON_ASSAULTSMG", "WEAPON_MACHINEPISTOL", "WEAPON_MINISMG", "WEAPON_COMBATPDW",
        };
        public int StarterAmmo { get; set; } = 1000;
        public List<string> MissionKit { get; set; } = new List<string>
        {
            "WEAPON_PISTOL:150", "WEAPON_SMG:360", "WEAPON_CARBINERIFLE:420", "WEAPON_PUMPSHOTGUN:48",
            "WEAPON_SNIPERRIFLE:40", "WEAPON_GRENADE:5",
        };
        public Dictionary<string, List<string>> MissionExtras { get; set; } = new Dictionary<string, List<string>>
        {
            { "ASS1", new List<string> { "WEAPON_STICKYBOMB:5" } },
            { "ASS2", new List<string> { "WEAPON_STICKYBOMB:3" } },
            { "ASS5", new List<string> { "WEAPON_STICKYBOMB:3" } },
        };
        public int FeedSize { get; set; } = 1500;
        public int TrapQueueLimit { get; set; } = 10;
        public bool TrapsAfterGoal { get; set; }

        public static ClientConfig Load(string directory)
        {
            string path = Path.Combine(directory, "config.json");
            ClientConfig config = null;
            if (File.Exists(path))
            {
                try
                {
                    config = new JavaScriptSerializer().Deserialize<ClientConfig>(File.ReadAllText(path));
                }
                catch (Exception ex)
                {
                    Log.Error("config.json is invalid, using defaults.", ex);
                }
            }
            else
            {
                Log.Info("config.json not found, using defaults.");
            }
            config = config ?? new ClientConfig();
            if (config.NotificationDelay < 200) config.NotificationDelay = 200;
            if (config.LogSize < 10) config.LogSize = 10;
            if (config.StarterWeapons == null || config.StarterWeapons.Count == 0) config.StarterWeapons = new List<string> { "WEAPON_PISTOL50" };
            if (config.StarterAmmo < 1) config.StarterAmmo = 1000;
            if (config.MissionKit == null) config.MissionKit = new List<string>();
            if (config.MissionExtras == null) config.MissionExtras = new Dictionary<string, List<string>>();
            if (config.FeedSize < 100) config.FeedSize = 100;
            config._path = path;
            return config;
        }

        public void Save()
        {
            if (_path == null) return;
            try
            {
                File.WriteAllText(_path, new JavaScriptSerializer().Serialize(this));
            }
            catch (Exception ex)
            {
                Log.Error("Could not write config.json.", ex);
            }
        }
    }

    internal sealed class ConnectionSettings
    {
        private string _path;

        public string Server { get; set; } = "archipelago.gg:38281";
        public string Slot { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string LastSave { get; set; }

        [ScriptIgnore]
        public bool IsComplete => !string.IsNullOrWhiteSpace(Server) && !string.IsNullOrWhiteSpace(Slot);

        public static ConnectionSettings Load(string directory)
        {
            string path = Path.Combine(directory, "connection.json");
            ConnectionSettings settings = null;
            if (File.Exists(path))
            {
                try
                {
                    settings = new JavaScriptSerializer().Deserialize<ConnectionSettings>(File.ReadAllText(path));
                }
                catch (Exception ex)
                {
                    Log.Error("connection.json is invalid.", ex);
                }
            }
            settings = settings ?? new ConnectionSettings();
            settings._path = path;
            return settings;
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(_path, new JavaScriptSerializer().Serialize(this));
            }
            catch (Exception ex)
            {
                Log.Error("Could not write connection.json.", ex);
            }
        }
    }
}
