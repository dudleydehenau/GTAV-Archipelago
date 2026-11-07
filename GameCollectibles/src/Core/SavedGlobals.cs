using System;
using GTA;
using GTA.Native;

namespace GameCollectibles.Core
{
    internal sealed class SavedGlobalsProfile
    {
        public int Build { get; set; }
        public int Bridges { get; set; }
        public int Completion { get; set; }
        public int Jumps { get; set; }
    }

    internal sealed class SavedGlobals
    {
        private const int CompletionCount = 321;
        private const int CompletionStride = 12;
        private const int CompletionType = 6;

        private static readonly SavedGlobalsProfile[] Profiles =
        {
            new SavedGlobalsProfile { Build = 3889, Bridges = 125117, Completion = 125166, Jumps = 133940 },
            new SavedGlobalsProfile { Build = 3725, Bridges = 125056, Completion = 125105, Jumps = 133879 },
        };

        private SavedGlobalsProfile _profile;
        private int _nextTry;

        public bool Available => _profile != null;

        public string Status { get; private set; } = "not validated";

        public bool EnsureValid()
        {
            if (_profile != null) return true;
            if (Game.GameTime < _nextTry) return false;
            _nextTry = Game.GameTime + 5000;
            string first = null;
            foreach (SavedGlobalsProfile candidate in Ordered())
            {
                string error = Check(candidate);
                if (error == null)
                {
                    _profile = candidate;
                    Status = $"OK (game build {GameBuild.Describe()}, addresses {candidate.Build})";
                    Log.Info("Saved globals: " + Status + ".");
                    return true;
                }
                first = first ?? error;
            }
            if (Status != first)
            {
                Status = first;
                Log.Warn($"Saved globals not found for game build {GameBuild.Describe()}: {first}");
            }
            return false;
        }

        public int BridgeBits(int word) => Read(_profile.Bridges + word);

        public int JumpBits() => Read(_profile.Jumps);

        public bool IsCompleted(int entry) => entry >= 0 && entry < CompletionCount && Read(_profile.Completion + 1 + entry * CompletionStride + 5) != 0;

        private static SavedGlobalsProfile[] Ordered()
        {
            int build = GameBuild.Number;
            var list = new SavedGlobalsProfile[Profiles.Length];
            int n = 0;
            foreach (SavedGlobalsProfile p in Profiles) if (p.Build == build) list[n++] = p;
            foreach (SavedGlobalsProfile p in Profiles) if (p.Build != build) list[n++] = p;
            return list;
        }

        private static string Check(SavedGlobalsProfile candidate)
        {
            try
            {
                int count = Read(candidate.Completion);
                if (count != CompletionCount) return $"completion list size {count}/{CompletionCount} (addresses {candidate.Build})";
                int first = TypeOf(candidate, 0);
                int stunts = TypeOf(candidate, 289);
                int shops = TypeOf(candidate, 298);
                if (first != 1 || stunts != 11 || shops != 12)
                    return $"completion entries {first}/{stunts}/{shops}, expected 1/11/12 (addresses {candidate.Build})";
                return null;
            }
            catch (Exception ex)
            {
                return "read error: " + ex.Message;
            }
        }

        private static int TypeOf(SavedGlobalsProfile candidate, int entry) =>
            Read(candidate.Completion + 1 + entry * CompletionStride + CompletionType);

        private static int Read(int index) => GlobalVariable.Get(index).Read<int>();
    }
}
