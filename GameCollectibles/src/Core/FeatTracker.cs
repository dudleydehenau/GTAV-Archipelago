using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameCollectibles.Core
{
    internal sealed class FeatTracker
    {
        private const int BridgeInterval = 1000;
        private const int LandmarkInterval = 500;
        private const int SkillInterval = 3000;
        private const int EscapeInterval = 250;
        private const int JumpWatch = 4000;
        private const float JumpMatchDistance = 60f;
        private const int GreyWindow = 2500;

        private readonly CollectibleRunner _core;
        private readonly SavedGlobals _globals = new SavedGlobals();
        private readonly HashSet<string> _reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<int> _knife = new HashSet<int>(FeatData.KnifeFlightBits);
        private readonly Dictionary<string, Blip> _blips = new Dictionary<string, Blip>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _skillHashes = new Dictionary<string, int>();
        private HashSet<FeatKind> _disabled = new HashSet<FeatKind>();
        private HashSet<string> _hints;
        private int _nextBridge;
        private int _nextLandmark;
        private int _nextSkill;
        private int _nextEscape;
        private int _nextBlips;
        private bool _jumpActive;
        private Vector3 _jumpStart;
        private int _jumpTotal;
        private int _jumpUnique;
        private int _jumpWatchUntil;
        private int _peakWanted;
        private int _greyAt = -100000;

        public FeatTracker(CollectibleRunner core)
        {
            _core = core;
            All = Build();
        }

        public IReadOnlyList<FeatInfoSource> All { get; }

        public string GlobalsStatus => _globals.Status;

        public IReadOnlyCollection<FeatKind> DisabledKinds => _disabled;

        public bool IsEnabled(FeatKind kind)
        {
            if (_disabled.Contains(kind)) return false;
            CollectiblesConfig c = _core.Config;
            switch (kind)
            {
                case FeatKind.StuntJump: return c.StuntJumps;
                case FeatKind.UnderBridge:
                case FeatKind.KnifeFlight: return c.Bridges;
                case FeatKind.Landmark: return c.Landmarks;
                case FeatKind.Skill: return c.Skills;
                case FeatKind.Activity: return c.Activities;
                default: return c.Escapes;
            }
        }

        public void SetDisabled(IEnumerable<FeatKind> kinds)
        {
            var next = new HashSet<FeatKind>(kinds ?? Enumerable.Empty<FeatKind>());
            if (next.SetEquals(_disabled)) return;
            _disabled = next;
            _nextBlips = 0;
            Log.Info("Disabled feats: " + (_disabled.Count == 0 ? "none" : string.Join(", ", _disabled)) + ".");
        }

        public IEnumerable<string> Hints => _hints;

        public void SetHints(IEnumerable<string> ids)
        {
            _hints = ids == null ? null : new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
            _nextBlips = 0;
        }

        public void Tick(int now)
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists()) return;
            if (IsEnabled(FeatKind.StuntJump)) TrackJumps(now, player);
            if (now >= _nextEscape)
            {
                _nextEscape = now + EscapeInterval;
                if (IsEnabled(FeatKind.Escape)) TrackEscape(now, player);
            }
            if (now >= _nextLandmark)
            {
                _nextLandmark = now + LandmarkInterval;
                if (IsEnabled(FeatKind.Landmark) && !player.IsDead) TrackLandmarks(player);
            }
            if (now >= _nextBridge)
            {
                _nextBridge = now + BridgeInterval;
                if (IsEnabled(FeatKind.UnderBridge) || IsEnabled(FeatKind.KnifeFlight)) TrackBridges();
            }
            if (now >= _nextSkill)
            {
                _nextSkill = now + SkillInterval;
                if (IsEnabled(FeatKind.Skill)) TrackSkills();
                if (IsEnabled(FeatKind.Activity)) TrackActivities();
            }
            if (now >= _nextBlips)
            {
                _nextBlips = now + 2000;
                UpdateBlips();
            }
        }

        public void Clear()
        {
            foreach (Blip blip in _blips.Values)
                if (blip != null && blip.Exists()) blip.Delete();
            _blips.Clear();
        }

        public void Forget(string id) => _reported.Remove(id);

        private void TrackJumps(int now, Ped player)
        {
            bool active = Function.Call<bool>(Hash.IS_STUNT_JUMP_IN_PROGRESS);
            if (active && !_jumpActive)
            {
                _jumpActive = true;
                _jumpStart = player.IsInVehicle() ? player.CurrentVehicle.Position : player.Position;
                _jumpTotal = Function.Call<int>(Hash.GET_TOTAL_SUCCESSFUL_STUNT_JUMPS);
                _jumpUnique = Function.Call<int>(Hash.GET_NUM_SUCCESSFUL_STUNT_JUMPS);
                _jumpWatchUntil = int.MaxValue;
                Log.Debug($"Stunt jump started at {_jumpStart}.");
            }
            else if (!active && _jumpActive)
            {
                _jumpActive = false;
                _jumpWatchUntil = now + JumpWatch;
            }
            if (now > _jumpWatchUntil || _jumpWatchUntil == 0) return;
            int total = Function.Call<int>(Hash.GET_TOTAL_SUCCESSFUL_STUNT_JUMPS);
            int unique = Function.Call<int>(Hash.GET_NUM_SUCCESSFUL_STUNT_JUMPS);
            if (total <= _jumpTotal && unique <= _jumpUnique) return;
            _jumpWatchUntil = 0;
            int best = -1;
            float bestDistance = JumpMatchDistance;
            for (int i = 0; i < FeatData.StuntJumps.Length; i++)
            {
                Vector3 start = FeatData.StuntJumps[i];
                float distance = new Vector2(start.X - _jumpStart.X, start.Y - _jumpStart.Y).Length();
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            if (best < 0)
            {
                Log.Warn($"Stunt jump completed at {_jumpStart}, but it matches none of the 50 jumps.");
                return;
            }
            Complete($"StuntJump_{best + 1:00}", FeatKind.StuntJump, $"Stunt Jump #{best + 1}");
        }

        private void TrackEscape(int now, Ped ped)
        {
            if (ped.IsDead)
            {
                _peakWanted = 0;
                return;
            }
            int wanted = Game.Player.Wanted.WantedLevel;
            if (wanted > 0)
            {
                if (wanted > _peakWanted) _peakWanted = wanted;
                if (Function.Call<bool>(Hash.ARE_PLAYER_STARS_GREYED_OUT, Game.Player)) _greyAt = now;
                return;
            }
            if (_peakWanted == 0) return;
            int peak = _peakWanted;
            _peakWanted = 0;
            if (now - _greyAt > GreyWindow) return;
            if (Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player, true)) return;
            if (Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE)) return;
            for (int stars = 1; stars <= Math.Min(5, peak); stars++)
                Complete($"Escape_{stars}", FeatKind.Escape, $"Escaped a {stars}-star wanted level");
        }

        private void TrackLandmarks(Ped player)
        {
            Vector3 here = player.Position;
            foreach (Landmark landmark in FeatData.Landmarks)
            {
                if (IsDone(landmark.Id)) continue;
                float dx = landmark.Position.X - here.X;
                float dy = landmark.Position.Y - here.Y;
                if (dx * dx + dy * dy > landmark.Radius * landmark.Radius) continue;
                if (here.Z < landmark.MinHeight) continue;
                Complete(landmark.Id, FeatKind.Landmark, landmark.Name);
            }
        }

        private void TrackBridges()
        {
            if (!_globals.EnsureValid()) return;
            for (int word = 0; word < 3; word++)
            {
                int bits = _globals.BridgeBits(word);
                if (bits == 0) continue;
                for (int b = 0; b < 32; b++)
                {
                    if ((bits & (1 << b)) == 0) continue;
                    int index = word * 32 + b;
                    if (index >= FeatData.BridgePoints.Length) continue;
                    bool knife = _knife.Contains(index);
                    FeatKind kind = knife ? FeatKind.KnifeFlight : FeatKind.UnderBridge;
                    if (!IsEnabled(kind)) continue;
                    string id = knife ? $"KnifeFlight_{index:00}" : $"UnderBridge_{index:00}";
                    Complete(id, kind, knife ? "Knife Flight" : "Under the Bridge");
                }
            }
        }

        private void TrackActivities()
        {
            if (!_globals.EnsureValid()) return;
            foreach (KeyValuePair<int, string> activity in FeatData.Activities)
                if (_globals.IsCompleted(activity.Key))
                    Complete($"Activity_{activity.Key}", FeatKind.Activity, activity.Value);
        }

        private void TrackSkills()
        {
            for (int c = 0; c < FeatData.Characters.Length; c++)
            {
                foreach (Skill skill in FeatData.Skills)
                {
                    int value = ReadStat($"SP{c}_{skill.Stat}");
                    if (value < 10 || value > 100) continue;
                    for (int level = 10; level <= value; level += 10)
                        Complete($"Skill_{FeatData.Characters[c]}_{skill.Key}_{level}", FeatKind.Skill, $"{FeatData.Characters[c]}: {skill.Name} {level}%");
                }
            }
        }

        private int ReadStat(string name)
        {
            if (!_skillHashes.TryGetValue(name, out int hash))
            {
                hash = Function.Call<int>(Hash.GET_HASH_KEY, name);
                _skillHashes[name] = hash;
            }
            using (var result = new OutputArgument())
            {
                if (!Function.Call<bool>(Hash.STAT_GET_INT, hash, result, -1)) return -1;
                return result.GetResult<int>();
            }
        }

        private bool IsDone(string id) => _core.Store.Contains(id) || _reported.Contains(id);

        private void Complete(string id, FeatKind kind, string text)
        {
            if (IsDone(id)) return;
            _reported.Add(id);
            _core.Store.Add(id);
            RemoveBlip(id);
            Log.Info($"Feat done: {id}.");
            if (kind == FeatKind.Landmark || kind == FeatKind.StuntJump || kind == FeatKind.Escape || kind == FeatKind.Activity)
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "CHECKPOINT_PERFECT", "HUD_MINI_GAME_SOUNDSET", true);
            if (_core.Config.Notify) GTA.UI.Notification.PostTicker($"~p~{GameText.Feat(kind)}~s~ {text}", false);
            try
            {
                CollectiblesApi.RaiseCollected(id);
            }
            catch (Exception ex)
            {
                Log.Error("A Collected event handler failed.", ex);
            }
        }

        private void UpdateBlips()
        {
            bool show = _core.Config.LandmarkBlips && IsEnabled(FeatKind.Landmark);
            foreach (Landmark landmark in FeatData.Landmarks)
            {
                bool want = show && !IsDone(landmark.Id) && (_hints == null || _hints.Contains(landmark.Id));
                _blips.TryGetValue(landmark.Id, out Blip blip);
                bool has = blip != null && blip.Exists();
                if (want && !has)
                {
                    blip = Blip.Create(landmark.Position);
                    blip.Sprite = BlipSprite.Standard;
                    blip.Color = BlipColor.Purple;
                    blip.Scale = 0.75f;
                    blip.IsShortRange = false;
                    blip.Name = "Landmark: " + landmark.Name;
                    _blips[landmark.Id] = blip;
                }
                else if (!want && has) RemoveBlip(landmark.Id);
            }
        }

        private void RemoveBlip(string id)
        {
            if (_blips.TryGetValue(id, out Blip blip) && blip != null && blip.Exists()) blip.Delete();
            _blips.Remove(id);
        }

        private static List<FeatInfoSource> Build()
        {
            var list = new List<FeatInfoSource>();
            for (int i = 0; i < FeatData.StuntJumps.Length; i++)
                list.Add(new FeatInfoSource($"StuntJump_{i + 1:00}", FeatKind.StuntJump, $"Stunt Jump #{i + 1}", FeatData.StuntJumps[i]));
            var knife = new HashSet<int>(FeatData.KnifeFlightBits);
            for (int i = 0; i < FeatData.BridgePoints.Length; i++)
            {
                if (i == 65 || i == 66) continue;
                bool isKnife = knife.Contains(i);
                list.Add(new FeatInfoSource(isKnife ? $"KnifeFlight_{i:00}" : $"UnderBridge_{i:00}", isKnife ? FeatKind.KnifeFlight : FeatKind.UnderBridge,
                    isKnife ? "Knife Flight" : "Under the Bridge", FeatData.BridgePoints[i]));
            }
            foreach (Landmark landmark in FeatData.Landmarks)
                list.Add(new FeatInfoSource(landmark.Id, FeatKind.Landmark, landmark.Name, landmark.Position));
            foreach (string character in FeatData.Characters)
                foreach (Skill skill in FeatData.Skills)
                    for (int level = 10; level <= 100; level += 10)
                        list.Add(new FeatInfoSource($"Skill_{character}_{skill.Key}_{level}", FeatKind.Skill, $"{character}: {skill.Name} {level}%", Vector3.Zero));
            for (int stars = 1; stars <= 5; stars++)
                list.Add(new FeatInfoSource($"Escape_{stars}", FeatKind.Escape, $"Escape a {stars}-star wanted level", Vector3.Zero));
            foreach (KeyValuePair<int, string> activity in FeatData.Activities)
                list.Add(new FeatInfoSource($"Activity_{activity.Key}", FeatKind.Activity, activity.Value, Vector3.Zero));
            return list;
        }
    }

    internal sealed class FeatInfoSource
    {
        public FeatInfoSource(string id, FeatKind kind, string name, Vector3 position)
        {
            Id = id;
            Kind = kind;
            Name = name;
            Position = position;
        }

        public string Id { get; }
        public FeatKind Kind { get; }
        public string Name { get; }
        public Vector3 Position { get; }
    }
}
