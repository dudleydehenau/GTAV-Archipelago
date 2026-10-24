using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace GameArchipelago.Core
{
    internal sealed class ReceivedRecord
    {
        public long Item { get; set; }
        public long Location { get; set; }
        public int Player { get; set; }
        public string Sender { get; set; }
        public string Gift { get; set; }
        public string GiftName { get; set; }
        public string LocationName { get; set; }
        public string ReceivedAt { get; set; }
    }

    internal sealed class FeedEntry
    {
        public string Time { get; set; }
        public string Kind { get; set; }
        public string Text { get; set; }
    }

    internal sealed class TrapRecord
    {
        public string Id { get; set; }
        public string Sender { get; set; }
        public string ReceivedAt { get; set; }
        public string Status { get; set; }
    }

    internal sealed class SlotSave
    {
        public int Version { get; set; } = 1;
        public string Seed { get; set; }
        public string Slot { get; set; }
        public string Server { get; set; }
        public int Goal { get; set; }
        public int GoalMissions { get; set; }
        public string GoalMission { get; set; }
        public string GoalCollectible { get; set; }
        public long GoalLocation { get; set; }
        public string StartCharacter { get; set; }
        public List<string> Missions { get; set; } = new List<string>();
        public int ChecksPerMission { get; set; } = 1;
        public int GoldChecks { get; set; }
        public int ObjectiveChecks { get; set; }
        public Dictionary<string, int> CollectibleSteps { get; set; } = new Dictionary<string, int>();
        public int ChecksPerCollectible { get; set; } = 1;
        public bool RadarFromStart { get; set; } = true;
        public bool GuaranteedVehicles { get; set; }
        public bool MissionTraps { get; set; }
        public bool MissionWeapons { get; set; } = true;
        public bool PostGoalChaos { get; set; }
        public int EstimatedMinutes { get; set; }
        public int EstimatedGoalMinutes { get; set; }
        public int MoneyMultiplier { get; set; } = 100;
        public bool DeathLink { get; set; }
        public int DeathLinkMode { get; set; } = -1;
        public List<ReceivedRecord> Items { get; set; } = new List<ReceivedRecord>();
        public int GiftIndex { get; set; }
        public int TrapIndex { get; set; }
        public int NotifiedIndex { get; set; }
        public string GameId { get; set; }
        public List<long> Checked { get; set; } = new List<long>();
        public List<long> Pending { get; set; } = new List<long>();
        public List<long> ServerChecked { get; set; } = new List<long>();
        public List<string> Passed { get; set; } = new List<string>();
        public List<string> Gold { get; set; } = new List<string>();
        public List<string> Objectives { get; set; } = new List<string>();
        public List<string> Collected { get; set; } = new List<string>();
        public List<TrapRecord> TrapLog { get; set; } = new List<TrapRecord>();
        public List<string> Boats { get; set; } = new List<string>();
        public Dictionary<string, int> ObjectiveTotals { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string> ObjectiveNames { get; set; } = new Dictionary<string, string>();
        public List<FeedEntry> Feed { get; set; } = new List<FeedEntry>();
        public string StarterGameId { get; set; }
        public string StarterWeapon { get; set; }
        public int StarterRevision { get; set; }
        public bool GoalDone { get; set; }

        [ScriptIgnore]
        public string Key => MakeKey(Seed, Slot);

        public static string MakeKey(string seed, string slot)
        {
            var text = new StringBuilder();
            foreach (char c in (seed ?? "seed") + "_" + (slot ?? "slot"))
                text.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '-');
            return text.ToString();
        }

        public void Normalize()
        {
            Missions = Missions ?? new List<string>();
            CollectibleSteps = CollectibleSteps ?? new Dictionary<string, int>();
            CollectibleSteps = new Dictionary<string, int>(CollectibleSteps, StringComparer.OrdinalIgnoreCase);
            Items = Items ?? new List<ReceivedRecord>();
            Checked = Checked ?? new List<long>();
            Pending = Pending ?? new List<long>();
            ServerChecked = ServerChecked ?? new List<long>();
            Passed = Passed ?? new List<string>();
            Gold = Gold ?? new List<string>();
            Objectives = Objectives ?? new List<string>();
            Collected = Collected ?? new List<string>();
            TrapLog = TrapLog ?? new List<TrapRecord>();
            Boats = Boats ?? new List<string>();
            ObjectiveTotals = new Dictionary<string, int>(ObjectiveTotals ?? new Dictionary<string, int>(), StringComparer.OrdinalIgnoreCase);
            ObjectiveNames = new Dictionary<string, string>(ObjectiveNames ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
            Feed = Feed ?? new List<FeedEntry>();
            if (ChecksPerMission < 1) ChecksPerMission = 1;
            if (ChecksPerCollectible < 1) ChecksPerCollectible = 1;
            if (MoneyMultiplier <= 0) MoneyMultiplier = 100;
            GiftIndex = Math.Max(0, Math.Min(GiftIndex, Items.Count));
            TrapIndex = Math.Max(0, Math.Min(TrapIndex, Items.Count));
            NotifiedIndex = Math.Max(0, Math.Min(NotifiedIndex, Items.Count));
        }

        public bool IsDone(long location) => Checked.Contains(location) || ServerChecked.Contains(location);

        public void AddFeed(string kind, string text, int keep)
        {
            Feed.Add(new FeedEntry { Time = DateTime.Now.ToString("MM-dd HH:mm"), Kind = kind, Text = text });
            if (Feed.Count > keep) Feed.RemoveRange(0, Feed.Count - keep);
        }

        public void AddTrap(TrapRecord record, int keep)
        {
            TrapLog.Insert(0, record);
            if (TrapLog.Count > keep) TrapLog.RemoveRange(keep, TrapLog.Count - keep);
        }
    }

    internal sealed class SaveStore
    {
        private readonly string _directory;

        public SaveStore(string directory)
        {
            _directory = Path.Combine(directory, "saves");
        }

        public SlotSave Load(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            string path = Path.Combine(_directory, key + ".json");
            if (!File.Exists(path)) return null;
            try
            {
                SlotSave save = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<SlotSave>(File.ReadAllText(path));
                save?.Normalize();
                return save;
            }
            catch (Exception ex)
            {
                Log.Error($"Save {key} is invalid.", ex);
                return null;
            }
        }

        public void Save(SlotSave save)
        {
            if (save == null) return;
            try
            {
                Directory.CreateDirectory(_directory);
                string path = Path.Combine(_directory, save.Key + ".json");
                string temp = path + ".tmp";
                File.WriteAllText(temp, new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(save));
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
            }
            catch (Exception ex)
            {
                Log.Error("Could not write the Archipelago save.", ex);
            }
        }

        public IReadOnlyList<string> List()
        {
            try
            {
                if (!Directory.Exists(_directory)) return Array.Empty<string>();
                return Directory.GetFiles(_directory, "*.json").Select(Path.GetFileNameWithoutExtension).ToList();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}
