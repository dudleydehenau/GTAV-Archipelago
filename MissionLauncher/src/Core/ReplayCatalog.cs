using System;
using System.Collections.Generic;
using System.Linq;
using GTA.Native;

namespace MissionLauncher.Core
{
    internal sealed class ReplayCatalog
    {
        private static readonly string[][] StrangersAndFreaks =
        {
            new[] { "Abigail1", "ABI1" }, new[] { "Abigail2", "ABI2" },
            new[] { "Barry1", "BA1" }, new[] { "Barry2", "BA2" }, new[] { "Barry3", "BA3" }, new[] { "Barry3A", "BA3A" },
            new[] { "Barry3C", "BA3C" }, new[] { "Barry4", "BA4" },
            new[] { "Dreyfuss1", "DRE1" },
            new[] { "Epsilon1", "EPS1" }, new[] { "Epsilon2", "EPS2" }, new[] { "Epsilon3", "EPS3" }, new[] { "Epsilon4", "EPS4" },
            new[] { "Epsilon5", "EPS5" }, new[] { "Epsilon6", "EPS6" }, new[] { "Epsilon7", "EPS7" }, new[] { "Epsilon8", "EPS8" },
            new[] { "Extreme1", "EXT1" }, new[] { "Extreme2", "EXT2" }, new[] { "Extreme3", "EXT3" }, new[] { "Extreme4", "EXT4" },
            new[] { "Fanatic1", "FAN1" }, new[] { "Fanatic2", "FAN2" }, new[] { "Fanatic3", "FAN3" },
            new[] { "Hao1", "HAO1" },
            new[] { "Hunting1", "HUN1" }, new[] { "Hunting2", "HUN2" },
            new[] { "Josh1", "JOS1" }, new[] { "Josh2", "JOS2" }, new[] { "Josh3", "JOS3" }, new[] { "Josh4", "JOS4" },
            new[] { "Maude1", "MAU1" },
            new[] { "Minute1", "MIN1" }, new[] { "Minute2", "MIN2" }, new[] { "Minute3", "MIN3" },
            new[] { "MrsPhilips1", "MRS1" }, new[] { "MrsPhilips2", "MRS2" },
            new[] { "Nigel1", "NI1" }, new[] { "Nigel1A", "NI1A" }, new[] { "Nigel1B", "NI1B" }, new[] { "Nigel1C", "NI1C" },
            new[] { "Nigel1D", "NI1D" }, new[] { "Nigel2", "NI2" }, new[] { "Nigel3", "NI3" },
            new[] { "Omega1", "OME1" }, new[] { "Omega2", "OME2" },
            new[] { "Paparazzo1", "PA1" }, new[] { "Paparazzo2", "PA2" }, new[] { "Paparazzo3", "PA3" }, new[] { "Paparazzo3A", "PA3A" },
            new[] { "Paparazzo3B", "PA3B" }, new[] { "Paparazzo4", "PA4" },
            new[] { "Rampage1", "RAM1" }, new[] { "Rampage2", "RAM2" }, new[] { "Rampage3", "RAM3" }, new[] { "Rampage4", "RAM4" },
            new[] { "Rampage5", "RAM5" },
            new[] { "TheLastOne", "SAS1" },
            new[] { "Tonya1", "TON1" }, new[] { "Tonya2", "TON2" }, new[] { "Tonya3", "TON3" }, new[] { "Tonya4", "TON4" },
            new[] { "Tonya5", "TON5" },
        };

        private static readonly string[] StoryOrder =
        {
            "PRO1", "ARM1", "ARM2", "ARM3", "FRA0", "FAM1", "FAM3", "FAM2", "LS1",
            "JH1", "JHP1A", "JHP1B", "JHP2A", "JH2", "JH2A", "JH2B",
            "LM1", "TRV1", "CHN1", "TRV2", "CHN2", "TRV3", "FAM4",
            "FIB1", "FIB2", "FRA1",
            "DH1", "DHP1", "DHP2B", "DH2A", "DH2B",
            "FIB4I", "FB4P1", "FB4P2", "FB4P3", "FB4P4", "FB4P5", "FIB4",
            "CAR1", "CAR2", "SOL1", "MAR1", "CAR3", "CAR4",
            "FAM5", "FIB3", "FIB5", "TRV4", "FH1", "MIC1", "MIC2", "SOL2", "AH1",
            "EXL1", "EXL2", "EXL3",
            "RH1", "RHP1", "RH2",
            "FAM6", "AH2", "AHP1", "AH3a", "AH3b",
            "MIC3", "FRA2", "MIC4", "SOL3",
            "FH2I", "FHPRA", "FHPRB", "FHPC1", "FHPC2", "FHPC3", "FHPD", "FH2A", "FH2B",
            "FINI", "FINA", "FINB", "FINC1", "FINC2", "FINC",
            "ASS1", "ASS2", "ASS3", "ASS4", "ASS5",
            "MEA1", "MEJ1", "MET1",
            "DRF1", "DRF2", "DRF3", "DRF4", "DRF5",
        };

        private static readonly bool[] StrangersReplayable =
        {
            false, false, true, true, false, true, true, false, true, false,
            false, false, true, false, true, false, true, true, true, true,
            true, true, true, true, true, true, true, false, true, true,
            true, false, true, true, true, false, false, false, true, true,
            true, true, true, true, false, false, true, true, false, true,
            true, true, true, true, true, true, true, true, true, true,
            true, true, true,
        };

        private static readonly HashSet<string> FallbackNotReplayable = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "FIB4I", "FB4P3", "FH2I", "FHPD", "FINI", "FINC",
            "MEA1", "MEJ1",
            "DRF1", "DRF2", "DRF3", "DRF4", "DRF5",
        };

        private static readonly Dictionary<string, string> Variants = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "JH2A", " - Smart" },
            { "JH2B", " - Loud" },
            { "AH3a", " - Fire Crew" },
            { "AH3b", " - Roof Entry" },
            { "DH2A", " - Offshore" },
            { "DH2B", " - Freighter" },
            { "FH2A", " - Subtle" },
            { "FH2B", " - Obvious" },
            { "FHPC1", " - Pillbox Hill" },
            { "FHPC2", " - Rockford Hills" },
            { "FHPC3", " - Mission Row" },
            { "FINA", " (Ending A - Kill Trevor)" },
            { "FINB", " (Ending B - Kill Michael)" },
            { "FINC1", " (Ending C - Save everyone)" },
            { "FINC2", " (Ending C - Save everyone)" },
        };

        private GameGlobals _globals;

        public IReadOnlyList<ReplayMission> Story { get; private set; } = Array.Empty<ReplayMission>();
        public IReadOnlyList<ReplayMission> Strangers { get; private set; } = Array.Empty<ReplayMission>();
        public IReadOnlyList<ReplayMission> All { get; private set; } = Array.Empty<ReplayMission>();
        public bool Built { get; private set; }

        public void Build(GameGlobals globals)
        {
            _globals = globals;
            int count = globals.MissionCount();
            int flagged = 0;
            for (int i = 0; i < count; i++) if (globals.IsStoryReplayable(i)) flagged++;
            bool useFlags = flagged >= 50 && flagged < count;
            if (!useFlags) Log.Warn($"Replay flags look wrong ({flagged}/{count}), using the built-in list instead.");

            var found = new List<Tuple<int, string, string>>();
            var skipped = new List<string>();
            for (int i = 0; i < count; i++)
            {
                string code = globals.MissionCode(i);
                if (string.IsNullOrEmpty(code)) continue;
                if (useFlags ? !globals.IsStoryReplayable(i) : FallbackNotReplayable.Contains(code))
                {
                    skipped.Add(code);
                    continue;
                }
                string label = "M_" + code;
                if (!GameText.Exists(label) && GameText.Exists(label + "A")) label += "A";
                bool known = Array.FindIndex(StoryOrder, c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase)) >= 0;
                if (!GameText.Exists(label) && !known) continue;
                found.Add(Tuple.Create(i, code, label));
            }

            var story = new List<ReplayMission>();
            int number = 1;
            foreach (Tuple<int, string, string> entry in found
                .OrderBy(e => OrderOf(e.Item2))
                .ThenBy(e => e.Item1))
            {
                story.Add(new ReplayMission(ReplayCategory.Story, number++, entry.Item1, entry.Item2, entry.Item3, string.Empty, globals.MissionScriptHash(entry.Item1)));
            }

            var strangers = new List<ReplayMission>();
            int rcNumber = 101;
            for (int i = 0; i < StrangersAndFreaks.Length; i++)
            {
                if (!StrangersReplayable[i]) continue;
                string script = StrangersAndFreaks[i][0];
                string label = "RC_" + StrangersAndFreaks[i][1];
                int hash = Function.Call<int>(Hash.GET_HASH_KEY, script.ToLowerInvariant());
                strangers.Add(new ReplayMission(ReplayCategory.StrangersAndFreaks, rcNumber++, i, label, label, script, hash));
            }

            Log.Info($"Not replayable according to the game ({(useFlags ? "mission flags" : "built-in list")}): {string.Join(", ", skipped)}.");
            Story = story.AsReadOnly();
            Strangers = strangers.AsReadOnly();
            All = story.Concat(strangers).ToList().AsReadOnly();
            RefreshCompletion();
            RefreshTitles();
            Built = true;
            Log.Info($"Replay catalog: {story.Count} story missions ({story.Count(m => m.IsCompleted)} completed), {strangers.Count} Strangers and Freaks missions ({strangers.Count(m => m.IsCompleted)} completed).");
            foreach (ReplayMission m in All) Log.Debug($"  {m.Id,-3} {m.Code,-8} {(m.IsCompleted ? m.CompletionOrder.ToString() : "-"),-4} {m.Title}");
        }

        public bool CompletionAvailable => _globals != null && _globals.CompletionAvailable;

        public void RefreshCompletion()
        {
            if (_globals == null) return;
            foreach (ReplayMission m in All) m.CompletionOrder = _globals.CompletionOrder((int)m.Category, m.GameIndex);
        }

        public IReadOnlyList<ReplayMission> Visible(ReplayCategory category, bool onlyCompleted, bool completionOrder)
        {
            IEnumerable<ReplayMission> list = category == ReplayCategory.Story ? Story : Strangers;
            if (!CompletionAvailable) return list.ToList().AsReadOnly();
            if (onlyCompleted) list = list.Where(m => m.IsCompleted);
            if (completionOrder) list = list.OrderBy(m => m.IsCompleted ? 0 : 1).ThenBy(m => m.CompletionOrder).ThenBy(m => m.Number);
            return list.ToList().AsReadOnly();
        }

        public void RefreshTitles()
        {
            foreach (ReplayMission m in All)
            {
                string title = GameText.Get(m.TitleLabel, m.Code);
                if (Variants.TryGetValue(m.Code, out string variant)) title += variant;
                m.Title = title;
            }
            foreach (IGrouping<string, ReplayMission> group in All.GroupBy(m => m.Title, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            {
                foreach (ReplayMission m in group) m.Title += $" ({m.Code})";
            }
        }

        private static int OrderOf(string code)
        {
            int index = Array.FindIndex(StoryOrder, c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
            return index < 0 ? int.MaxValue : index;
        }

        public ReplayMission Find(int number) => All.FirstOrDefault(m => m.Number == number);

        public ReplayMission Find(ReplayCategory category, int gameIndex) =>
            All.FirstOrDefault(m => m.Category == category && m.GameIndex == gameIndex);
    }
}
