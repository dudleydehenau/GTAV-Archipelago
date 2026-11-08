using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal sealed class SkillKeeper
    {
        public static readonly string[] Stats = { "STAMINA", "SHOOTING_ABILITY", "STRENGTH", "STEALTH_ABILITY", "FLYING_ABILITY", "WHEELIE_ABILITY", "LUNG_CAPACITY" };
        private static readonly int[][] StoryDefaults =
        {
            new[] { 47, 79, 21, 81, 31, 54, 22 },
            new[] { 51, 24, 49, 21, 19, 71, 46 },
            new[] { 23, 69, 79, 49, 82, 31, 28 },
        };
        private const int Interval = 1000;
        private const int SettleTime = 20000;

        private readonly StateStore _store;
        private int _next;
        private int _settledUntil;
        private int _model;

        public SkillKeeper(StateStore store)
        {
            _store = store;
        }

        public bool Ready { get; private set; }

        public bool Stable => Ready && Game.GameTime >= _settledUntil && !Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS);

        public void Apply()
        {
            bool captured = false;
            foreach (PlayableCharacter character in Characters.All)
            {
                CharacterRecord record = _store.State.For(character);
                if (record.SkillStart != null) continue;
                record.SkillStart = Capture(character);
                captured = true;
                Log.Info($"Skills of {character} at the start of this game: {Describe(record.SkillStart)}. Skill checks count the progress made from here.");
            }
            if (captured) _store.Save();
            Ready = true;
            _next = Game.GameTime + Interval;
            _settledUntil = Game.GameTime + SettleTime;
            _model = Game.Player.Character.Model.Hash;
        }

        public void Reset() => Ready = false;

        public void Tick()
        {
            if (!Ready) return;
            int model = Game.Player.Character.Model.Hash;
            if (model != _model || Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS))
            {
                _model = model;
                _settledUntil = Game.GameTime + SettleTime;
            }
            if (Game.GameTime < _next || Game.GameTime >= _settledUntil) return;
            _next = Game.GameTime + Interval;
            int absorbed = 0;
            foreach (PlayableCharacter character in Characters.All)
            {
                CharacterRecord record = _store.State.For(character);
                if (record.SkillStart == null) continue;
                foreach (string stat in Stats)
                {
                    int value = Get(character, stat);
                    if (record.SkillStart.TryGetValue(stat, out int start) && value <= start) continue;
                    record.SkillStart[stat] = value;
                    absorbed++;
                }
            }
            if (absorbed == 0) return;
            _store.Save();
            Log.Info($"Skills: the game set {absorbed} story value(s) after loading, they are part of the starting point, not progress.");
        }

        public int Progress(PlayableCharacter character, string stat)
        {
            if (!Ready) return -1;
            CharacterRecord record = _store.State.For(character);
            if (record.SkillStart == null || !record.SkillStart.TryGetValue(stat, out int start)) return -1;
            int value = Get(character, stat);
            if (value <= start) return 0;
            if (start >= 100) return 100;
            int progress = (value - start) * 100 / (100 - start);
            return progress > 100 ? 100 : progress;
        }

        private static Dictionary<string, int> Capture(PlayableCharacter character)
        {
            var skills = new Dictionary<string, int>();
            for (int i = 0; i < Stats.Length; i++)
            {
                int value = Get(character, Stats[i]);
                int story = StoryDefaults[(int)character][i];
                skills[Stats[i]] = value > story ? value : story;
            }
            return skills;
        }

        private static string Describe(Dictionary<string, int> skills)
        {
            var parts = new List<string>();
            foreach (string stat in Stats) parts.Add(stat.ToLowerInvariant() + " " + skills[stat]);
            return string.Join(", ", parts);
        }

        private static int Get(PlayableCharacter character, string stat)
        {
            int hash = Function.Call<int>(Hash.GET_HASH_KEY, $"SP{(int)character}_{stat}");
            var value = new OutputArgument();
            return Function.Call<bool>(Hash.STAT_GET_INT, hash, value, -1) ? value.GetResult<int>() : 0;
        }
    }
}
