using System;
using System.IO;
using GTA;
using GTA.Native;

namespace GameEnvironment.Core
{
    public sealed class EnvironmentScript : Script
    {
        private const int ClassifyInterval = 2000;
        private const int PrologueWatchDuration = 30000;
        private const int AutoSaveDelay = 4000;

        private readonly SterileSetup _setup;
        private readonly InactiveNotice _notice = new InactiveNotice();
        private bool _disabled;
        private bool _wasBusy;
        private bool _needsApply;
        private bool _firstApply = true;
        private int _validationStart = -1;
        private int _nextClassify;
        private int _prologueWatchUntil;
        private int _autoSaveAt = -1;
        private int _lastSaveAt = -1;
        private const int LoadSaveDelay = 15000;
        private int _lastErrorAt = -1;
        private SaveKind? _lastKind;

        public EnvironmentScript()
        {
            if (Instance != null) return;
            Instance = this;

            string directory = Path.Combine(BaseDirectory, "GameEnvironment");
            Log.Init(directory);
            Config = EnvironmentConfig.Load(directory);
            Log.Verbose = Config.VerboseLog;

            Store = new StateStore(directory);
            Store.Load();
            Globals = new StoryGlobals(Config.Globals);
            Roster = new CharacterService(Config, Globals, Store);
            Economy = new Economy(Store);
            Garage = new Garage(Config, Store, Economy);
            Doors = new DoorKeeper(Config.OpenDoors, Globals);
            _setup = new SterileSetup(Config, Globals, Store, Roster);

            Roster.Switched += character => Economy.ScheduleWeaponCheck(2000);
            Garage.Destroyed += record => EnvironmentApi.RaiseVehicleDestroyed(record);

            Tick += OnTick;
            Aborted += OnAborted;
        }

        internal static EnvironmentScript Instance { get; private set; }

        internal EnvironmentConfig Config { get; }
        internal StoryGlobals Globals { get; }
        internal StateStore Store { get; }
        internal CharacterService Roster { get; }
        internal Economy Economy { get; }
        internal Garage Garage { get; }
        internal DoorKeeper Doors { get; }
        internal EnvironmentMode Mode { get; private set; } = EnvironmentMode.Starting;

        internal bool IsReady => !_disabled && Globals.Available;

        internal void RequestApply() => _needsApply = true;

        internal void ResetProgress(string owner)
        {
            Garage.DespawnAll();
            EnvironmentState state = Store.State;
            state.Vehicles.Clear();
            state.NextVehicleId = 1;
            foreach (PlayableCharacter character in Characters.All)
            {
                CharacterRecord record = state.For(character);
                record.Money = 0;
                record.Weapons.Clear();
                Economy.SetCash(character, 0);
            }
            Ped ped = Game.Player.Character;
            if (ped != null && ped.Exists()) ped.Weapons.RemoveAll();
            state.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            state.Owner = owner;
            Store.Save();
            Economy.ScheduleWeaponCheck(1500);
            Log.Info($"Progress reset for a new Archipelago game ({owner}).");
        }

        internal bool IsBusy => Globals.Available && ReplayGuard.IsBusy(Globals);
        internal bool IsLive => Mode == EnvironmentMode.Sterile && !IsBusy;

        private void OnTick(object sender, EventArgs e)
        {
            if (_disabled)
            {
                DrawNotice();
                return;
            }
            try
            {
                if (!Globals.Available && !TryValidate()) return;
                bool busy = ReplayGuard.IsBusy(Globals);
                Abilities.Tick(Config, busy);
                if (!busy) WheelFix.Tick();

                if (busy)
                {
                    Doors.Release("replay");
                    if (Config.ReplayCharacterFix) ReplayCast.Tick(Globals);
                    if (!_wasBusy)
                    {
                        _wasBusy = true;
                        Log.Info("Replay in progress, Environment paused (the mission handles the characters).");
                    }
                    return;
                }
                if (_wasBusy)
                {
                    _wasBusy = false;
                    _needsApply = true;
                    _nextClassify = 0;
                    Log.Info("Replay over, Environment resumed.");
                }

                if (Mode == EnvironmentMode.Starting || Mode == EnvironmentMode.Waiting || Game.GameTime >= _nextClassify) Classify();
                if (Mode != EnvironmentMode.Sterile)
                {
                    DrawNotice();
                    return;
                }

                if (_needsApply) Apply();
                WatchPrologue();
                Roster.Tick();
                Economy.Tick();
                Garage.Tick();
                if (Function.Call<bool>(Hash.GET_MISSION_FLAG)) Doors.Release("mission");
                else Doors.Tick();
                if (_autoSaveAt < 0 && Config.AutoSaveMinutes > 0 && _lastSaveAt >= 0 && Game.GameTime - _lastSaveAt >= Config.AutoSaveMinutes * 60000)
                    _autoSaveAt = Game.GameTime;
                if (_autoSaveAt >= 0 && Game.GameTime >= _autoSaveAt)
                {
                    if (_setup.AutoSave())
                    {
                        _autoSaveAt = -1;
                        _lastSaveAt = Game.GameTime;
                    }
                    else _autoSaveAt = Game.GameTime + 5000;
                }
            }
            catch (Exception ex)
            {
                int now = System.Environment.TickCount;
                if (_lastErrorAt < 0 || now - _lastErrorAt > 5000)
                {
                    _lastErrorAt = now;
                    Log.Error("Tick error.", ex);
                }
            }
        }

        private void DrawNotice()
        {
            if (Config.ShowInactiveNotice) _notice.Draw(Mode);
        }

        private bool TryValidate()
        {
            if (_validationStart < 0) _validationStart = System.Environment.TickCount;
            Globals.Validate();
            if (Globals.Available)
            {
                Log.Info("Game data: " + Globals.Status);
                Log.Info("MissionLauncher " + (ReplayGuard.IsLauncherPresent ? "found." : "not found, replays are detected from the game only."));
                return true;
            }
            if (Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE) || System.Environment.TickCount - _validationStart < 20000) return false;
            _disabled = true;
            Mode = EnvironmentMode.Disabled;
            Log.Warn("Game data: " + Globals.Status);
            GTA.UI.Notification.PostTicker("~r~GameEnvironment: " + "unsupported game version, disabled.", true);
            return false;
        }

        private void Classify()
        {
            _nextClassify = Game.GameTime + ClassifyInterval;
            SaveKind kind = Globals.Classify();
            if (kind != _lastKind)
            {
                _lastKind = kind;
                Log.Info($"Save state: {kind} ({Globals.Describe()}).");
            }
            switch (kind)
            {
                case SaveKind.NewGame:
                    if (!Config.NewGameSetup)
                    {
                        SetMode(EnvironmentMode.StorySave);
                        return;
                    }
                    Garage.DespawnAll();
                    PlayableCharacter start = _setup.Run();
                    _prologueWatchUntil = System.Environment.TickCount + PrologueWatchDuration;
                    _autoSaveAt = Game.GameTime + AutoSaveDelay;
                    _firstApply = false;
                    _needsApply = true;
                    _lastKind = SaveKind.Sterile;
                    SetMode(EnvironmentMode.Sterile);
                    EnvironmentApi.RaiseSterileGameCreated(start);
                    break;
                case SaveKind.Sterile:
                    if (Mode != EnvironmentMode.Sterile)
                    {
                        SetMode(EnvironmentMode.Sterile);
                        _needsApply = true;
                        if (Config.AutoSaveMinutes > 0 && _autoSaveAt < 0) _autoSaveAt = Game.GameTime + LoadSaveDelay;
                    }
                    if (Config.DisableRandomEvents) Function.Call(Hash.SET_RANDOM_EVENT_FLAG, false);
                    if (!_needsApply) ClearCommunications();
                    break;
                case SaveKind.Story:
                    SetMode(EnvironmentMode.StorySave);
                    break;
                default:
                    if (Mode != EnvironmentMode.Waiting) SetMode(EnvironmentMode.Waiting);
                    break;
            }
        }

        private void SetMode(EnvironmentMode mode)
        {
            if (Mode == mode) return;
            if (Mode == EnvironmentMode.Sterile)
            {
                Garage.DespawnAll();
                Doors.Release("mode change");
            }
            Log.Info($"Mode: {Mode} -> {mode}.");
            Mode = mode;
            if (mode == EnvironmentMode.StorySave)
                Log.Info("Story save detected: Environment stays inactive and never touches this save.");
        }

        private void Apply()
        {
            _needsApply = false;
            EnvironmentState state = Store.State;
            if (!state.Sterile)
            {
                Log.Warn("This save is sterile but state.json is not: adopting the save.");
                state.Sterile = true;
                state.CreatedAt ??= DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                PlayableCharacter? current = Characters.Current;
                if (current != null)
                {
                    state.StartCharacter = (int)current.Value;
                    state.LastCharacter = (int)current.Value;
                    state.For(current.Value).Money = Game.Player.Money;
                }
                Store.Save();
            }
            Globals.FlowHalted = true;
            if (!Function.Call<bool>(Hash.GET_MISSION_FLAG) && Globals.ReleaseMissionCandidate())
                Log.Info("Story mission slot released (mission type reset to none).");
            if (!Function.Call<bool>(Hash.GET_MISSION_FLAG) && Globals.EnableSelector())
                Log.Info("Character wheel re-enabled.");
            Roster.Apply();
            Economy.ApplyMoney();
            Economy.ScheduleWeaponCheck(1500);
            if (_firstApply)
            {
                _firstApply = false;
                Garage.ResetDeployments();
            }
            if (Config.DisableRandomEvents) Function.Call(Hash.SET_RANDOM_EVENT_FLAG, false);
            ClearCommunications();
            Log.Info("State applied. " + Globals.Describe());
            Log.Info(Globals.DescribeWheel());
            EnvironmentApi.RaiseStateApplied();
        }

        private void ClearCommunications()
        {
            if (!Config.BlockCommunications) return;
            int pending = Globals.PendingCommunications();
            if (pending == 0 && !Globals.HasCommunicationPriority()) return;
            if (Function.Call<bool>(Hash.IS_MOBILE_PHONE_CALL_ONGOING)) return;
            Globals.ClearCommunications();
            Log.Info($"Communication queue cleared ({pending} pending calls, texts or emails).");
        }

        private void WatchPrologue()
        {
            if (_prologueWatchUntil == 0 || System.Environment.TickCount > _prologueWatchUntil) return;
            if (!Globals.IsPrologueRunning) return;
            Log.Warn("prologue1 started again after the sterile setup.");
            Globals.FlowHalted = true;
            _setup.StopPrologue();
        }

        private void OnAborted(object sender, EventArgs e)
        {
            try
            {
                Store.Save();
                if (Mode == EnvironmentMode.Sterile)
                {
                    Garage.DespawnAll();
                    Doors.Release("script stopped");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error while stopping.", ex);
            }
        }
    }
}
