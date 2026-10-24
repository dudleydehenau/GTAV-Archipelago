using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using GTA;
using GTA.Math;
using GTA.Native;

namespace MissionLauncher.Core
{
    public sealed class MissionMarkers : Script
    {
        private const int RefreshInterval = 1000;
        private const int CompletionInterval = 5000;
        private const float GroupDistance = 3f;
        private const float DrawDistance = 60f;
        private const float HeightTolerance = 3f;
        private const int AutoStartDelay = 400;
        private static readonly string[] CharacterNames = { "Michael", "Franklin", "Trevor" };
        private static readonly int[] BlipColors = { 42, 43, 44 };
        private static readonly Color[] MarkerColors =
        {
            Color.FromArgb(150, 101, 180, 212),
            Color.FromArgb(150, 171, 237, 171),
            Color.FromArgb(150, 255, 163, 87),
        };

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<Group> _groups = new List<Group>();
        private readonly HashSet<string> _armed = new HashSet<string>();
        private bool _initialized;
        private bool _all;
        private bool _autoStart;
        private float _radius;
        private int _character = -1;
        private string _signature = string.Empty;
        private int _nextRefresh;
        private int _nextCompletion;
        private Group _inside;
        private int _enteredAt;
        private int _choice;

        public MissionMarkers()
        {
            Tick += OnTick;
            Aborted += (s, e) => ClearBlips();
        }

        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                if (!_initialized && !Initialize()) return;
                if (!CanShow())
                {
                    Reset();
                    return;
                }
                int character = CurrentCharacter();
                if (character < 0)
                {
                    Reset();
                    return;
                }
                if (character != _character || Game.GameTime >= _nextRefresh) Refresh(character);
                HandleProximity();
            }
            catch (Exception ex)
            {
                Log.Error("Mission marker error.", ex);
                Reset();
                _nextRefresh = Game.GameTime + 5000;
            }
        }

        private bool Initialize()
        {
            Launcher launcher = Launcher.Instance;
            if (launcher == null || !launcher.IsReady) return false;
            _initialized = true;
            string mode = launcher.Config.Markers ?? "All";
            if (string.Equals(mode, "Off", StringComparison.OrdinalIgnoreCase))
            {
                Log.Info("Mission markers disabled (Markers = Off).");
                Tick -= OnTick;
                return false;
            }
            _all = !string.Equals(mode, "Api", StringComparison.OrdinalIgnoreCase);
            _autoStart = launcher.Config.MarkerAutoStart;
            _radius = Math.Max(0.75f, launcher.Config.MarkerRadius);
            foreach (MarkerMission data in MarkerData.Load(launcher.DataDirectory))
            {
                ReplayMission mission = launcher.Catalog.All.FirstOrDefault(m => string.Equals(m.Code, data.Code, StringComparison.OrdinalIgnoreCase));
                if (mission == null || data.Points.Count == 0) continue;
                _entries.Add(new Entry(mission, data));
            }
            Log.Info($"Mission markers ready: {_entries.Count} missions with a start point, mode {(_all ? "All" : "Api")}.");
            return true;
        }

        private static bool CanShow()
        {
            if (MissionApi.IsReplayActive) return false;
            if (Function.Call<bool>(Hash.GET_MISSION_FLAG)) return false;
            if (!Function.Call<bool>(Hash.IS_PLAYER_PLAYING, Game.Player)) return false;
            if (!GTA.UI.Screen.IsFadedIn) return false;
            if (Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE)) return false;
            if (Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS)) return false;
            return !Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE);
        }

        private static int CurrentCharacter()
        {
            Ped ped = Game.Player.Character;
            if (ped == null || !ped.Exists()) return -1;
            int hash = ped.Model.Hash;
            if (hash == new Model(PedHash.Michael).Hash) return 0;
            if (hash == new Model(PedHash.Franklin).Hash) return 1;
            if (hash == new Model(PedHash.Trevor).Hash) return 2;
            return -1;
        }

        private MissionMarkerState StateOf(ReplayMission mission)
        {
            if (!_all || MissionApi.UseApiMarkers) return Launcher.Instance.Markers.Get(mission.Number);
            return mission.IsCompleted ? MissionMarkerState.Replay : MissionMarkerState.Available;
        }

        private void Refresh(int character)
        {
            _nextRefresh = Game.GameTime + RefreshInterval;
            Launcher launcher = Launcher.Instance;
            if (Game.GameTime >= _nextCompletion)
            {
                _nextCompletion = Game.GameTime + CompletionInterval;
                launcher.Catalog.RefreshCompletion();
            }
            string name = CharacterNames[character];
            var visible = new List<Tuple<Entry, MarkerPoint, MissionMarkerState>>();
            foreach (Entry entry in _entries)
            {
                if (!entry.Data.Points.TryGetValue(name, out MarkerPoint point)) continue;
                MissionMarkerState state = StateOf(entry.Mission);
                if (state == MissionMarkerState.Hidden) continue;
                visible.Add(Tuple.Create(entry, point, state));
            }
            var signature = new StringBuilder(character.ToString());
            foreach (var v in visible) signature.Append('|').Append(v.Item1.Mission.Number).Append(':').Append((int)v.Item3);
            bool blipsAlive = _groups.All(g => g.Blip != null && g.Blip.Exists());
            if (character == _character && signature.ToString() == _signature && blipsAlive) return;

            _character = character;
            _signature = signature.ToString();
            ClearBlips();
            foreach (var v in visible.OrderBy(v => v.Item3 == MissionMarkerState.Available ? 0 : 1).ThenBy(v => v.Item1.Mission.Number))
            {
                var position = new Vector3(v.Item2.X, v.Item2.Y, v.Item2.Z);
                Group group = _groups.FirstOrDefault(g => g.Position.DistanceTo2D(position) < GroupDistance && Math.Abs(g.Position.Z - position.Z) < HeightTolerance);
                if (group == null)
                {
                    group = new Group(position, v.Item2.Sprite, v.Item1.Data.Category);
                    _groups.Add(group);
                }
                group.Items.Add(new Item(v.Item1.Mission, v.Item3));
            }
            foreach (Group group in _groups) CreateBlip(group, character);
            _inside = null;
            Log.Debug($"Mission markers refreshed for {name}: {visible.Count} missions in {_groups.Count} places.");
        }

        private void CreateBlip(Group group, int character)
        {
            Blip blip = Blip.Create(group.Position);
            blip.Sprite = (BlipSprite)group.Sprite;
            blip.Color = (BlipColor)BlipColors[character];
            blip.IsShortRange = string.Equals(group.Category, "StrangersAndFreaks", StringComparison.OrdinalIgnoreCase);
            string name = group.Items[0].Mission.Title;
            if (group.Items.Count > 1) name += $" (+{group.Items.Count - 1})";
            blip.Name = name;
            if (group.Items.All(i => i.State == MissionMarkerState.Replay)) Function.Call(Hash.SHOW_TICK_ON_BLIP, blip, true);
            group.Blip = blip;
        }

        private void HandleProximity()
        {
            if (_groups.Count == 0) return;
            Ped player = Game.Player.Character;
            Vector3 position = player.Position;
            Group inside = null;
            foreach (Group group in _groups)
            {
                float distance = group.Position.DistanceTo2D(position);
                bool within = distance < _radius && Math.Abs(group.Position.Z - position.Z) < HeightTolerance;
                if (within) inside = group;
                else if (distance > _radius + 2f) _armed.Add(group.Key);
                if (distance < DrawDistance) DrawMarker(group);
            }

            if (inside == null)
            {
                _inside = null;
                return;
            }
            if (inside != _inside)
            {
                _inside = inside;
                _enteredAt = Game.GameTime;
                _choice = 0;
            }

            bool armed = _armed.Contains(inside.Key);
            if (_autoStart && armed && inside.Items.Count == 1 && inside.Items[0].State == MissionMarkerState.Available)
            {
                if (Game.GameTime - _enteredAt >= AutoStartDelay) Start(inside, inside.Items[0]);
                return;
            }
            ShowChooser(inside);
        }

        private void ShowChooser(Group group)
        {
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.Phone, true);
            if (group.Items.Count > 1)
            {
                if (JustPressed(Control.PhoneUp)) _choice = (_choice + group.Items.Count - 1) % group.Items.Count;
                if (JustPressed(Control.PhoneDown)) _choice = (_choice + 1) % group.Items.Count;
            }
            if (_choice >= group.Items.Count) _choice = 0;

            var text = new StringBuilder();
            for (int i = 0; i < group.Items.Count; i++)
            {
                Item item = group.Items[i];
                if (i > 0) text.Append("~n~");
                text.Append(i == _choice ? "~y~> " : "~s~   ");
                text.Append(item.Mission.Title);
                if (item.State == MissionMarkerState.Replay) text.Append(" ~c~(replay)");
                text.Append("~s~");
            }
            text.Append("~n~");
            if (group.Items.Count > 1) text.Append("~INPUT_CELLPHONE_UP~~INPUT_CELLPHONE_DOWN~ ").Append("Choose").Append("  ");
            text.Append("~INPUT_CONTEXT~ ").Append("Start");
            GTA.UI.Screen.ShowHelpTextThisFrame(text.ToString(), false);

            if (JustPressed(Control.Context)) Start(group, group.Items[_choice]);
        }

        private static bool JustPressed(Control control) =>
            Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, (int)control)
            || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, (int)control);

        private static float GroundHeight(Vector3 position)
        {
            var ground = new OutputArgument();
            if (!Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, position.X, position.Y, position.Z, ground, false, false)) return 0f;
            return ground.GetResult<float>();
        }

        private void Start(Group group, Item item)
        {
            _armed.Remove(group.Key);
            _inside = null;
            if (MissionApi.RequestReplay(item.Mission.Number, out string error))
            {
                Log.Info($"Mission started from its marker: {item.Mission.Id} {item.Mission.Title}.");
                return;
            }
            GTA.UI.Notification.PostTicker("~y~" + error, false);
        }

        private void DrawMarker(Group group)
        {
            if (!group.GroundResolved)
            {
                float ground = GroundHeight(group.Position + new Vector3(0f, 0f, 2f));
                group.Ground = ground > 0f && Math.Abs(ground - group.Position.Z) < 5f ? ground : group.Position.Z - 1f;
                group.GroundResolved = true;
            }
            Color color = MarkerColors[_character < 0 ? 0 : _character];
            World.DrawMarker(MarkerType.Cylinder, new Vector3(group.Position.X, group.Position.Y, group.Ground), Vector3.Zero, Vector3.Zero,
                new Vector3(_radius * 2f, _radius * 2f, 0.6f), color);
        }

        private void Reset()
        {
            if (_groups.Count > 0) ClearBlips();
            _character = -1;
            _signature = string.Empty;
            _inside = null;
        }

        private void ClearBlips()
        {
            foreach (Group group in _groups)
                if (group.Blip != null && group.Blip.Exists()) group.Blip.Delete();
            _groups.Clear();
        }

        private sealed class Entry
        {
            public Entry(ReplayMission mission, MarkerMission data)
            {
                Mission = mission;
                Data = data;
            }

            public ReplayMission Mission { get; }
            public MarkerMission Data { get; }
        }

        private sealed class Item
        {
            public Item(ReplayMission mission, MissionMarkerState state)
            {
                Mission = mission;
                State = state;
            }

            public ReplayMission Mission { get; }
            public MissionMarkerState State { get; }
        }

        private sealed class Group
        {
            public Group(Vector3 position, int sprite, string category)
            {
                Position = position;
                Sprite = sprite;
                Category = category;
                Key = $"{Math.Round(position.X)}:{Math.Round(position.Y)}";
            }

            public Vector3 Position { get; }
            public int Sprite { get; }
            public string Category { get; }
            public string Key { get; }
            public List<Item> Items { get; } = new List<Item>();
            public Blip Blip { get; set; }
            public bool GroundResolved { get; set; }
            public float Ground { get; set; }
        }
    }
}
