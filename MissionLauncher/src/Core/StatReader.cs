using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using GTA;
using GTA.Native;

namespace MissionLauncher.Core
{
    internal sealed class StatResults
    {
        public MissionMedal Medal { get; set; } = MissionMedal.None;
        public int Percent { get; set; }
        public List<MissionObjective> Objectives { get; } = new List<MissionObjective>();
    }

    internal sealed class StatReader
    {
        private static readonly string[] ShotStats = { "sp0_shots", "sp1_shots", "sp2_shots" };
        private static readonly string[] HitStats = { "sp0_hits", "sp1_hits", "sp2_hits" };

        private static readonly HashSet<string> NotObjectives = new HashSet<string> { "MISHSTA_203", "MISHSTA_206" };
        private readonly GameGlobals _globals;
        private readonly Dictionary<int, string> _labels = new Dictionary<int, string>();

        public StatReader(GameGlobals globals)
        {
            _globals = globals;
            LoadLabels();
        }

        public void ReadShotBaseline(out int shots, out int hits)
        {
            shots = SumStats(ShotStats);
            hits = SumStats(HitStats);
        }

        public StatResults Read(int shotsAtStart, int hitsAtStart)
        {
            var results = new StatResults();
            int count = _globals.ActiveStatCount();
            int visible = 0;
            int passed = 0;
            int shots = SumStats(ShotStats) - shotsAtStart;
            int hits = SumStats(HitStats) - hitsAtStart;
            int accuracy = shots <= 0 ? 100 : Math.Min(100, (int)Math.Floor(100.0 * hits / shots));
            bool checkpointSkip = false;

            for (int i = 0; i < count; i++)
            {
                int slot = _globals.ActiveStatBase(i);
                int statId = GameGlobals.Read(slot);
                if (statId < 0 || statId >= _globals.StatDefinitionCount) continue;

                int definition = _globals.StatDefinitionBase(statId);
                int type = GameGlobals.Read(definition);
                int target = GameGlobals.Read(definition + 2);
                bool lowerIsBetter = GameGlobals.Read(definition + 3) != 0;
                bool hidden = GameGlobals.Read(definition + 7) != 0;
                int state = GameGlobals.Read(slot + 3);
                int value = GameGlobals.Read(slot + 1);
                float floatValue = GameGlobals.ReadFloat(slot + 2);
                if (value == 0 && floatValue != 0f) value = (int)Math.Floor(floatValue);
                if (type == 9) value = accuracy;
                if (type == 15) value = shots;
                if (type == 14) value = (int)Math.Floor(floatValue);

                if (hidden) continue;
                if (_labels.TryGetValue(statId, out string generic) && NotObjectives.Contains(generic)) continue;
                visible++;
                if (state == 2) checkpointSkip = true;

                bool ok = state == 0 && IsSuccess(type, value, target, lowerIsBetter);
                if (ok) passed++;

                string label = _labels.TryGetValue(statId, out string l) ? l : string.Empty;
                string text = label.Length > 0 ? GameText.Get(label) : $"Stat {statId}";
                results.Objectives.Add(new MissionObjective(statId, label, text, type, value, target, lowerIsBetter, state != 0, ok) { Position = visible });
            }

            if (visible > 0)
            {
                int percent = passed == visible ? 100 : Math.Min(100, (int)Math.Ceiling(50.0 * passed / visible + 50.0));
                if (checkpointSkip) percent = 50;
                results.Percent = percent;
                results.Medal = percent == 100 ? MissionMedal.Gold : percent > 50 ? MissionMedal.Silver : MissionMedal.Bronze;
            }
            return results;
        }

        private static bool IsSuccess(int type, int value, int target, bool lowerIsBetter)
        {
            if (value < 0 || value == int.MaxValue) return false;
            switch (type)
            {
                case 1:
                case 2:
                case 4:
                case 5:
                case 17:
                    if (value < 1) return false;
                    break;
            }
            return lowerIsBetter ? value < target : value >= target;
        }

        private static int SumStats(string[] names)
        {
            int total = 0;
            foreach (string name in names)
            {
                using (var result = new OutputArgument())
                {
                    if (Function.Call<bool>(Hash.STAT_GET_INT, Function.Call<int>(Hash.GET_HASH_KEY, name), result, -1))
                    {
                        total += result.GetResult<int>();
                    }
                }
            }
            return total;
        }

        private void LoadLabels()
        {
            try
            {
                using (Stream stream = typeof(StatReader).Assembly.GetManifestResourceStream("MissionLauncher.stat_labels.json"))
                {
                    if (stream == null) return;
                    using (var reader = new StreamReader(stream))
                    {
                        var raw = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(reader.ReadToEnd());
                        foreach (KeyValuePair<string, string> kv in raw)
                        {
                            if (int.TryParse(kv.Key, out int id)) _labels[id] = kv.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Could not read the objective labels.", ex);
            }
        }
    }
}
