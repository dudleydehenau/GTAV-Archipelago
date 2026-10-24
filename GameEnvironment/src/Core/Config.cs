using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace GameEnvironment.Core
{
    internal sealed class EnvironmentConfig
    {
        public bool EnableMenu { get; set; }
        public string MenuKey { get; set; } = "F8";
        public bool VerboseLog { get; set; }
        public bool ShowInactiveNotice { get; set; } = true;
        public bool NewGameSetup { get; set; } = true;
        public string StartCharacter { get; set; } = "Random";
        public bool CharacterLocking { get; set; } = true;
        public bool AllowRelock { get; set; }
        public int StartMoney { get; set; }
        public int StartHour { get; set; } = 10;
        public bool DisableRandomEvents { get; set; } = true;
        public bool UnlockSpecialAbilities { get; set; } = true;
        public bool ReplayCharacterFix { get; set; } = true;
        public bool BlockCommunications { get; set; } = true;
        public bool AutoSaveAfterSetup { get; set; } = true;
        public int AutoSaveMinutes { get; set; } = 10;
        public float VehicleStoreDistance { get; set; } = 300f;
        public int DefaultRepairCost { get; set; } = 1000;
        public Dictionary<string, int> RepairCosts { get; set; }
        public List<SpawnPoint> Spawns { get; set; }
        public List<string> PrologueIpls { get; set; }
        public OpenDoorsConfig OpenDoors { get; set; } = new OpenDoorsConfig();
        public StoryGlobalsConfig Globals { get; set; } = new StoryGlobalsConfig();

        public static EnvironmentConfig Load(string directory)
        {
            string path = Path.Combine(directory, "config.json");
            EnvironmentConfig config;
            if (!File.Exists(path))
            {
                Log.Info("config.json not found, using defaults.");
                config = new EnvironmentConfig();
            }
            else
            {
                try
                {
                    config = new JavaScriptSerializer().Deserialize<EnvironmentConfig>(File.ReadAllText(path)) ?? new EnvironmentConfig();
                }
                catch (Exception ex)
                {
                    Log.Error("config.json is invalid, using defaults.", ex);
                    config = new EnvironmentConfig();
                }
            }
            config.Normalize();
            return config;
        }

        public SpawnPoint SpawnFor(PlayableCharacter character)
        {
            SpawnPoint spawn = Spawns.Find(s => string.Equals(s.Character, character.ToString(), StringComparison.OrdinalIgnoreCase));
            return spawn ?? DefaultSpawns()[(int)character];
        }

        public int RepairCostFor(GTA.VehicleClass vehicleClass)
        {
            return RepairCosts.TryGetValue(vehicleClass.ToString(), out int cost) ? Math.Max(0, cost) : Math.Max(0, DefaultRepairCost);
        }

        private void Normalize()
        {
            if (Globals == null) Globals = new StoryGlobalsConfig();
            if (Globals.CharacterIntroducedFlags == null || Globals.CharacterIntroducedFlags.Count != 3)
                Globals.CharacterIntroducedFlags = new List<int> { 115, 116, 117 };
            if (Globals.WheelDiagnostics == null) Globals.WheelDiagnostics = new List<int>();
            if (Globals.MissionStateResets == null) Globals.MissionStateResets = new List<int>();
            if (RepairCosts == null) RepairCosts = DefaultRepairCosts();
            RepairCosts = new Dictionary<string, int>(RepairCosts, StringComparer.OrdinalIgnoreCase);
            if (Spawns == null || Spawns.Count == 0) Spawns = DefaultSpawns();
            if (PrologueIpls == null || PrologueIpls.Count == 0) PrologueIpls = DefaultPrologueIpls();
            if (StartHour < 0 || StartHour > 23) StartHour = 10;
            if (VehicleStoreDistance < 50f) VehicleStoreDistance = 50f;
            if (StartMoney < 0) StartMoney = 0;
            if (OpenDoors == null) OpenDoors = new OpenDoorsConfig();
            if (OpenDoors.Groups == null) OpenDoors.Groups = OpenDoorsConfig.Defaults();
            else OpenDoors.MergeDefaults();
            if (OpenDoors.Radius < 20f) OpenDoors.Radius = 20f;
        }

        private static Dictionary<string, int> DefaultRepairCosts() => new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Super", 10000 },
            { "Planes", 10000 },
            { "Helicopters", 10000 },
            { "Military", 10000 },
        };

        private static List<SpawnPoint> DefaultSpawns() => new List<SpawnPoint>
        {
            new SpawnPoint { Character = "Michael", X = -852.4f, Y = 160.0f, Z = 65.6f, Heading = 90f },
            new SpawnPoint { Character = "Franklin", X = -14.0f, Y = -1453.0f, Z = 30.5f, Heading = 180f },
            new SpawnPoint { Character = "Trevor", X = 1981.6f, Y = 3816.5f, Z = 32.4f, Heading = 120f },
        };

        private static List<string> DefaultPrologueIpls() => new List<string>
        {
            "prologue01", "Prologue01c", "Prologue01d", "Prologue01e", "Prologue01f", "Prologue01g", "prologue01h",
            "prologue01i", "prologue01j", "prologue01k", "prologue01z", "prologue02", "prologue03", "prologue03b",
            "prologue03_grv_fun", "prologue04", "prologue04b", "prologue04_cover", "prologue05", "prologue05b",
            "prologue06", "prologue06b", "prologue06_int", "prologuerd", "prologuerdb", "prologue_DistantLights",
            "prologue_LODLights", "prologue_occl", "DES_ProTree_start", "DES_ProTree_start_lod",
        };
    }

    internal sealed class OpenDoorsConfig
    {
        public bool Enabled { get; set; } = true;
        public float Radius { get; set; } = 150f;
        public List<DoorGroup> Groups { get; set; }

        public void MergeDefaults()
        {
            Groups.RemoveAll(g => g == null);
            foreach (DoorGroup fallback in Defaults())
            {
                DoorGroup group = Groups.Find(g => string.Equals(g.Name, fallback.Name, StringComparison.OrdinalIgnoreCase));
                if (group == null)
                {
                    Groups.Add(fallback);
                    continue;
                }
                if (group.Doors == null) group.Doors = new List<string>();
                foreach (string door in fallback.Doors)
                    if (!group.Doors.Exists(d => string.Equals(d?.Trim(), door, StringComparison.OrdinalIgnoreCase))) group.Doors.Add(door);
                if (group.Interior == null && fallback.Interior != null) group.Interior = fallback.Interior;
                if (fallback.Buildings == null) continue;
                if (group.Buildings == null) group.Buildings = new Dictionary<string, int>();
                foreach (KeyValuePair<string, int> building in fallback.Buildings)
                    if (!group.Buildings.ContainsKey(building.Key)) group.Buildings[building.Key] = building.Value;
            }
        }

        public static List<DoorGroup> Defaults() => new List<DoorGroup>
        {
            new DoorGroup
            {
                Name = "Michael's house", X = -815f, Y = 175f, Z = 72f,
                Doors = new List<string>
                {
                    "AUTODOOR_MICHAEL_MANSION_GATE", "DOORHASH_M_MANSION_GA_SM", "DOORHASH_M_MANSION_F_L", "DOORHASH_M_MANSION_F_R",
                    "DOORHASH_M_MANSION_G1", "DOORHASH_M_MANSION_R_L1", "DOORHASH_M_MANSION_R_R1", "DOORHASH_M_MANSION_R_L2", "DOORHASH_M_MANSION_R_R2",
                },
            },
            new DoorGroup
            {
                Name = "Franklin's aunt's house", X = -14f, Y = -1441f, Z = 31f,
                Doors = new List<string> { "DOORHASH_F_HOUSE_SC_F", "DOORHASH_F_HOUSE_SC_B", "DOORHASH_F_HOUSE_SC_G" },
            },
            new DoorGroup
            {
                Name = "Franklin's villa", X = 8f, Y = 540f, Z = 176f,
                Doors = new List<string> { "DOORHASH_F_HOUSE_VH_F", "DOORHASH_F_HOUSE_VH_G" },
                Ipls = new List<string> { "v_franklinshouse" },
                Interior = new InteriorSets { X = 7.3f, Y = 531.3f, Z = 175.9f, Enable = new List<string> { "unlocked" }, Disable = new List<string> { "locked" } },
            },
            new DoorGroup
            {
                Name = "Trevor's trailer", X = 1973f, Y = 3815f, Z = 34f,
                Doors = new List<string> { "DOORHASH_T_TRAILER_CS", "DOORHASH_T_TRAILER_CS_G" },
            },
            new DoorGroup
            {
                Name = "Floyd's apartment", X = -1150f, Y = -1521f, Z = 10f,
                Doors = new List<string> { "DOORHASH_T_APARTMENT_VB" },
            },
            new DoorGroup
            {
                Name = "Lester's house", X = 1274f, Y = -1720f, Z = 55f,
                Doors = new List<string> { "DOORHASH_LESTER_F", "1646172266", "204467342" },
                Ipls = new List<string> { "v_lesters" },
            },
            new DoorGroup
            {
                Name = "Lester's factory", X = 717f, Y = -975f, Z = 25f,
                Doors = new List<string> { "DOORHASH_SWEATSHOP_L", "DOORHASH_SWEATSHOP_R", "-874851305", "-1480820165", "949391213" },
                Buildings = new Dictionary<string, int> { { "70", 1 }, { "71", 1 }, { "73", 1 } },
                Interior = new InteriorSets { X = 717.2994f, Y = -974.4271f, Z = 23.9142f, Name = "v_sweat", Unlock = true },
            },
            new DoorGroup
            {
                Name = "Vanilla Unicorn", X = 120f, Y = -1295f, Z = 29f,
                Doors = new List<string> { "DOORHASH_STRIPCLUB_F", "DOORHASH_STRIPCLUB_R" },
            },
            new DoorGroup
            {
                Name = "Simeon's dealership", X = -45f, Y = -1100f, Z = 27f,
                Doors = new List<string>
                {
                    "DOORHASH_DEALERSHIP_FRONT_L", "DOORHASH_DEALERSHIP_FRONT_R", "DOORHASH_DEALERSHIP_SIDE_L", "DOORHASH_DEALERSHIP_SIDE_R",
                    "DOORHASH_ARM2_SIMEON_OFFICE",
                },
            },
            new DoorGroup
            {
                Name = "Trevor Philips Industries", X = 1393f, Y = 3600f, Z = 35f,
                Doors = new List<string> { "DOORHASH_METHLAB_F_L", "DOORHASH_METHLAB_F_R", "DOORHASH_METHLAB_R" },
            },
        };
    }

    internal sealed class DoorGroup
    {
        public string Name { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Radius { get; set; }
        public List<string> Doors { get; set; }
        public List<string> Ipls { get; set; }
        public InteriorSets Interior { get; set; }
        public Dictionary<string, int> Buildings { get; set; }
    }

    internal sealed class InteriorSets
    {
        public string Name { get; set; }
        public bool Unlock { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public List<string> Enable { get; set; }
        public List<string> Disable { get; set; }
    }

    internal sealed class SpawnPoint
    {
        public string Character { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Heading { get; set; }
    }

    internal sealed class StoryGlobalsConfig
    {
        public int FlowState { get; set; } = 123996;
        public int FlowFlags { get; set; } = 124153;
        public int FlowFlagCount { get; set; } = 146;
        public int Strands { get; set; } = 123998;
        public int StrandCount { get; set; } = 32;
        public int MainStrand { get; set; } = 27;
        public List<int> CharacterIntroducedFlags { get; set; } = new List<int> { 115, 116, 117 };
        public int CharacterAvailable { get; set; } = 120161;
        public int StoryCompletion { get; set; } = 124326;
        public int StoryCompletionCount { get; set; } = 100;
        public int MissionTable { get; set; } = 93192;
        public int RepeatFlags { get; set; } = 80529;
        public int RunningMission { get; set; } = 80543;
        public int FlowBusy { get; set; } = 80552;
        public int Communications { get; set; } = 122599;
        public int MissionType { get; set; } = 44869;
        public int SelectorDisabled { get; set; } = 24395;
        public int MissionCandidate { get; set; } = 44831;
        public List<int> MissionStateResets { get; set; } = new List<int> { 44830, 44832, 65007, 65008 };
        public int SwitchBlocked { get; set; } = 80541;
        public int BuildingStates { get; set; } = 122399;
        public int BuildingDirty { get; set; } = 41608;
        public int BuildingDeferred { get; set; } = 41409;
        public int BuildingCount { get; set; } = 198;
        public List<int> WheelDiagnostics { get; set; } = new List<int> { 80552, 80541, 99713, 99926, 113951, 77342, 65001 };
    }
}
