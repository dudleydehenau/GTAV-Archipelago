using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using GameTraps.Traps;
using GTA;

namespace GameTraps.Core
{
    public sealed class TrapRunner : Script
    {
        private readonly List<Trap> _traps = new List<Trap>();
        private readonly List<Queued> _queue = new List<Queued>();
        private readonly Announcer _announcer = new Announcer();
        private readonly string _queuePath;
        private readonly string _settingsPath;
        private const int RetryDelay = 1000;
        private readonly List<Trap> _active = new List<Trap>();
        private int _nextStartAt;
        private int _lastErrorAt = -1;
        private string _lastBlockReason;

        public TrapRunner()
        {
            if (Instance != null) return;
            Instance = this;

            string directory = Path.Combine(BaseDirectory, "GameTraps");
            Log.Init(directory);
            Config = TrapsConfig.Load(directory);
            Log.Verbose = Config.VerboseLog;
            _queuePath = Path.Combine(directory, "queue.json");
            _settingsPath = Path.Combine(directory, "menu.json");
            LoadSettings();

            _traps.Add(new ScooterBrother(Config));
            _traps.Add(new Ramp(Config));
            _traps.Add(new AnimalHunt(Config));
            _traps.Add(new AnimalTransform(Config, directory));
            _traps.Add(new FallingPlanes(Config));
            _traps.Add(new TurboTraffic(Config));
            _traps.Add(new AlienAbduction(Config));
            _traps.Add(new Irresistible(Config));
            _traps.Add(new SecondComing(Config));
            _traps.Add(new WantedStars(Config));
            _traps.Add(new Drunk(Config));
            _traps.Add(new FlatTires(Config));
            _traps.Add(new Disarmed(Config));
            _traps.Add(new LesterTax(Config));
            _traps.Add(new Paparazzi(Config));
            _traps.Add(new Clowns(Config));
            _traps.Add(new MoonGravity(Config));
            _traps.Add(new Blizzard(Config));
            _traps.Add(new Nightfall(Config));
            _traps.Add(new Ragdoll(Config));
            _traps.Add(new LiftOff(Config));
            _traps.Add(new PhantomCar(Config));
            _traps.Add(new PhantomWedge(Config));
            _traps.Add(new RedLight(Config));
            _traps.Add(new Speed(Config));
            _traps.Add(new TimeMachine(Config));
            _traps.Add(new Purge(Config));
            _traps.Add(new Terminator(Config));
            _traps.Add(new TheMatrix(Config));
            _traps.Add(new MadMax(Config));
            _traps.Add(new Jaws(Config));
            _traps.Add(new MacheteGranny(Config));
            _traps.Add(new HereWeGoAgain(Config));
            _traps.Add(new Bowling(Config));
            _traps.Add(new ViceCity(Config));
            _traps.Add(new BigSmoke(Config));
            _traps.Add(new BattleRoyale(Config));
            _traps.Add(new MarioKart(Config));
            _traps.Add(new TopDown(Config));
            _traps.Add(new UpsideDown(Config));
            _traps.Add(new Clones(Config));
            _traps.Add(new Kidnap(Config));
            _traps.Add(new ObjectRain(Config));
            _traps.Add(new GiantBalls(Config));
            _traps.Add(new FailedHeroes(Config));
            _traps.Add(new Mariachi(Config));
            _traps.Add(new FloatingCars(Config));
            _traps.Add(new Kamikaze(Config));
            _traps.Add(new FlashMob(Config));
            _traps.Add(new InitialD(Config));
            _traps.Add(new ForrestGump(Config));
            _traps.Add(new WalkingDead(Config));

            LoadQueue();
            Log.Info($"{_traps.Count} traps ready, {_queue.Count} waiting in the queue.");

            Tick += OnTick;
            Aborted += OnAborted;
        }

        internal static TrapRunner Instance { get; private set; }
        internal TrapsConfig Config { get; }
        internal IReadOnlyList<Trap> Traps => _traps;
        internal IReadOnlyList<string> ActiveIds => _active.Select(t => t.Id).ToList();
        internal IReadOnlyList<string> QueuedIds => _queue.Select(q => q.Id).ToList();
        internal string ActiveId => _active.Count > 0 ? _active[0].Id : null;
        internal int Pending => _queue.Count;
        internal bool IsReady { get; private set; }

        internal Trap Find(string id) => _traps.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));

        internal bool Enqueue(string id, out string error) => Enqueue(id, null, out error);

        internal bool Enqueue(string id, string source, out string error)
        {
            Trap trap = Find(id);
            if (trap == null)
            {
                error = $"Unknown trap '{id}'.";
                return false;
            }
            if (!trap.Enabled)
            {
                error = $"{trap.Title} is disabled in config.json.";
                return false;
            }
            _queue.Add(new Queued { Id = trap.Id, Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim() });
            SaveQueue();
            Log.Info($"Trap queued: {trap.Id} ({_queue.Count} waiting).");
            if (Config.AnnounceQueue) AnnounceQueue();
            error = null;
            return true;
        }

        internal void AnnounceQueue()
        {
            int count = _queue.Count;
            GTA.UI.Notification.PostTicker("~r~" + (count == 1
                ? "1 trap waiting..."
                : $"{count} traps waiting..."), false);
        }

        internal void ClearQueue()
        {
            _queue.Clear();
            SaveQueue();
            Log.Info("Trap queue cleared.");
        }

        private void OnTick(object sender, EventArgs e)
        {
            IsReady = true;
            Trap current = null;
            try
            {
                _announcer.Draw();
                ScreenEffects.Process();
                TrapTimer.Process();

                bool inMissionNow = Safety.InMission;
                if (_wasInMission != inMissionNow) _calmSince = -1;
                if (_wasInMission && !inMissionNow) _afterMission = true;
                _wasInMission = inMissionNow;
                if (_active.Count > 0 && Safety.MustInterrupt(out string reason))
                {
                    bool mission = reason == "mission" || reason == "replay";
                    foreach (Trap trap in _active.ToList())
                    {
                        if (mission && AllowedInMission(trap)) continue;
                        Requeue(trap);
                        Finish(trap, true, reason);
                    }
                    if (_active.Count == 0) return;
                }
                foreach (Trap trap in _active.ToList())
                {
                    current = trap;
                    if (!trap.Update()) Finish(trap, false, null);
                }
                current = null;

                foreach (Trap trap in _traps)
                    if (!_active.Contains(trap)) trap.Background();
                if (_queue.Count == 0 || Game.GameTime < _nextStartAt) return;
                if (_active.Count >= Math.Max(1, Config.MaxActive)) return;
                _inMission = Safety.InMission;
                if (_inMission && !Config.AllowInMissions) return;
                if (!Safety.CanStart(_inMission, out string blocked))
                {
                    _calmSince = -1;
                    if (blocked != _lastBlockReason)
                    {
                        _lastBlockReason = blocked;
                        Log.Info($"Traps waiting: {blocked}.");
                    }
                    return;
                }
                if (_calmSince < 0) _calmSince = Game.GameTime;
                int settle = _afterMission ? AfterMissionDelay : CalmDelay;
                if (Game.GameTime - _calmSince < settle) return;
                _afterMission = false;
                _lastBlockReason = null;
                current = StartNext();
            }
            catch (Exception ex)
            {
                int now = Environment.TickCount;
                if (_lastErrorAt < 0 || now - _lastErrorAt > 5000)
                {
                    _lastErrorAt = now;
                    Log.Error("Tick error" + (current != null ? $" in {current.Id}." : "."), ex);
                }
                if (current != null && _active.Contains(current)) Finish(current, true, "error");
            }
        }

        private Trap StartNext()
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                Queued next = _queue[i];
                Trap trap = Find(next.Id);
                if (trap == null || !trap.Enabled)
                {
                    _queue.RemoveAt(i);
                    SaveQueue();
                    return null;
                }
                if (_active.Contains(trap) || _active.Any(a => (a.Channels & trap.Channels) != TrapChannel.None)) continue;
                if (!CanStack(trap)) continue;
                if (_inMission && !AllowedInMission(trap)) continue;
                string reason;
                bool ready = Preconditions.Met(trap.Id, out reason) && trap.CanStart(out reason);
                if (!ready)
                {
                    if (next.WaitingSince < 0) next.WaitingSince = Game.GameTime;
                    if (Game.GameTime - next.WaitingSince < Config.MaxWait)
                    {
                        if (next.Reason != reason)
                        {
                            next.Reason = reason;
                            Log.Info($"Trap {trap.Id} waits: {reason}.");
                        }
                        continue;
                    }
                    _queue.RemoveAt(i);
                    SaveQueue();
                    Log.Info($"Trap {trap.Id} dropped after waiting {Config.MaxWait / 1000} s: {reason}.");
                    return null;
                }
                _queue.RemoveAt(i);
                SaveQueue();
                _active.Add(trap);
                _started[trap] = new Started { Source = next.Source, At = Game.GameTime };
                _nextStartAt = Game.GameTime + Math.Max(0, Config.GapBetweenTraps);
                Log.Info($"Trap started: {trap.Id} ({_active.Count} running).");
                if (Config.Announce)
                {
                    if (Config.RevealNames) _announcer.Show(trap.Title, next.Source == null ? trap.Subtitle : "Courtesy of " + next.Source);
                    else _announcer.Show("Trap!", next.Source == null ? "Brace yourself." : "Courtesy of " + next.Source);
                }
                TrapApi.RaiseStarted(trap.Id);
                trap.Start();
                return trap;
            }
            _nextStartAt = Game.GameTime + RetryDelay;
            return null;
        }

        private bool _inMission;
        private bool _wasInMission;
        private const int AfterMissionDelay = 10000;
        private const int CalmDelay = 1500;
        private bool _afterMission;
        private int _calmSince = -1;
        private const int RequeueWindow = 20000;
        private readonly Dictionary<Trap, Started> _started = new Dictionary<Trap, Started>();

        private sealed class Started
        {
            public string Source;
            public int At;
        }

        private void Requeue(Trap trap)
        {
            if (!_started.TryGetValue(trap, out Started started)) return;
            if (Game.GameTime - started.At > RequeueWindow) return;
            _queue.Insert(0, new Queued { Id = trap.Id, Source = started.Source });
            SaveQueue();
            Log.Info($"Trap {trap.Id} cut short by the mission: queued again.");
        }

        private static readonly HashSet<string> NeverInMissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { TrapIds.AnimalTransform };

        internal bool AllowedInMission(Trap trap) =>
            Config.AllowInMissions && !NeverInMissions.Contains(trap.Id)
            && Config.MissionTraps != null && Config.MissionTraps.Contains(trap.Id, StringComparer.OrdinalIgnoreCase);

        private bool CanStack(Trap trap)
        {
            if (_active.Count == 0) return true;
            if (!Config.StackTraps) return false;
            if (_active.Count >= Math.Max(1, Config.MaxActive)) return false;
            TrapWeight weight = TrapWeights.Of(trap.Id);
            if (weight == TrapWeight.Heavy) return false;
            if (_active.Any(a => TrapWeights.Of(a.Id) == TrapWeight.Heavy)) return false;
            if (weight == TrapWeight.Medium && _active.Any(a => TrapWeights.Of(a.Id) == TrapWeight.Medium)) return false;
            return true;
        }

        private void Finish(Trap trap, bool interrupted, string reason)
        {
            _active.Remove(trap);
            _started.Remove(trap);
            if (TrapWeights.Of(trap.Id) == TrapWeight.Heavy) _nextStartAt = Math.Max(_nextStartAt, Game.GameTime + Math.Max(Config.GapBetweenTraps, 2500));
            TrapTimer.Hide(trap.Id);
            try
            {
                trap.Stop(interrupted);
            }
            catch (Exception ex)
            {
                Log.Error($"Error while stopping {trap.Id}.", ex);
            }
            Log.Info(interrupted ? $"Trap interrupted: {trap.Id} ({reason})." : $"Trap finished: {trap.Id}.");
            TrapApi.RaiseFinished(trap.Id, interrupted);
        }

        private void OnAborted(object sender, EventArgs e)
        {
            foreach (Trap trap in _active.ToList())
            {
                try
                {
                    trap.Abort();
                    Log.Info($"Trap stopped with the scripts: {trap.Id}.");
                }
                catch (Exception ex)
                {
                    Log.Error("Error while stopping.", ex);
                }
            }
            _active.Clear();
        }

        internal void SetStreamSafe(bool value)
        {
            Config.StreamSafe = value;
            Log.Info($"Stream safe mode {(value ? "on" : "off")}.");
            try
            {
                File.WriteAllText(_settingsPath, new JavaScriptSerializer().Serialize(new MenuSettings { StreamSafe = value }));
            }
            catch (Exception ex)
            {
                Log.Error("Could not write menu.json.", ex);
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsPath)) return;
                MenuSettings saved = new JavaScriptSerializer().Deserialize<MenuSettings>(File.ReadAllText(_settingsPath));
                if (saved?.StreamSafe != null) Config.StreamSafe = saved.StreamSafe.Value;
            }
            catch (Exception ex)
            {
                Log.Error("menu.json is invalid.", ex);
            }
        }

        private void LoadQueue()
        {
            try
            {
                if (!File.Exists(_queuePath)) return;
                string json = File.ReadAllText(_queuePath);
                var serializer = new JavaScriptSerializer();
                if (json.TrimStart().StartsWith("[\""))
                {
                    List<string> ids = serializer.Deserialize<List<string>>(json);
                    if (ids != null) _queue.AddRange(ids.Where(id => Find(id) != null).Select(id => new Queued { Id = id }));
                    return;
                }
                List<SavedTrap> saved = serializer.Deserialize<List<SavedTrap>>(json);
                if (saved != null) _queue.AddRange(saved.Where(t => t != null && Find(t.Id) != null).Select(t => new Queued { Id = t.Id, Source = t.Source }));
            }
            catch (Exception ex)
            {
                Log.Error("queue.json is invalid, starting with an empty queue.", ex);
            }
        }

        private void SaveQueue()
        {
            try
            {
                string temp = _queuePath + ".tmp";
                File.WriteAllText(temp, new JavaScriptSerializer().Serialize(_queue.Select(q => new SavedTrap { Id = q.Id, Source = q.Source }).ToList()));
                if (File.Exists(_queuePath)) File.Delete(_queuePath);
                File.Move(temp, _queuePath);
            }
            catch (Exception ex)
            {
                Log.Error("Could not write queue.json.", ex);
            }
        }

        private sealed class Queued
        {
            public string Id;
            public string Source;
            public int WaitingSince = -1;
            public string Reason;
        }

        private sealed class SavedTrap
        {
            public string Id { get; set; }
            public string Source { get; set; }
        }

        private sealed class MenuSettings
        {
            public bool? StreamSafe { get; set; }
        }
    }
}
