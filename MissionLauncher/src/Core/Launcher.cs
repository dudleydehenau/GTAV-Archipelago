using System;
using System.IO;
using GTA;
using GTA.Native;

namespace MissionLauncher.Core
{
    public sealed class Launcher : Script
    {
        internal static Launcher Instance { get; private set; }

        private readonly GameGlobals _globals;
        private readonly StatReader _stats;
        private readonly ReplayObserver _observer;
        private readonly RestorePointConfirmer _confirmer;
        private bool _initialized;
        private bool _failed;

        internal LauncherConfig Config { get; }
        internal ReplayCatalog Catalog { get; } = new ReplayCatalog();
        internal ReplayObserver Observer => _observer;
        internal MarkerStore Markers { get; }
        internal string DataDirectory { get; }
        internal bool IsReady => _initialized && !_failed && Catalog.Built;

        public Launcher()
        {
            if (Instance != null) return;
            Instance = this;

            string directory = Path.Combine(BaseDirectory, "MissionLauncher");
            DataDirectory = directory;
            Log.Init(directory);
            Config = LauncherConfig.Load(directory);
            Log.Verbose = Config.VerboseLog;

            _globals = new GameGlobals();
            _stats = new StatReader(_globals);
            _observer = new ReplayObserver(_globals, _stats, Catalog, directory);
            _confirmer = new RestorePointConfirmer(Config.RestorePointKeyFallback);
            Markers = new MarkerStore(directory);

            Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (_failed) return;

            if (!_initialized)
            {
                _initialized = true;
                _globals.Validate();
                Log.Info("Game data: " + _globals.Status);
                if (!_globals.Available)
                {
                    _failed = true;
                    GTA.UI.Notification.PostTicker("~r~MissionLauncher: " + "unsupported game version, disabled.", true);
                    return;
                }
                try
                {
                    Catalog.Build(_globals);
                }
                catch (Exception ex)
                {
                    _failed = true;
                    Log.Error("Could not build the replay catalog.", ex);
                    return;
                }
            }

            try
            {
                if (Config.AutoConfirmRestorePoint) _confirmer.Tick();
                _observer.Tick();
            }
            catch (Exception ex)
            {
                Log.Error("Replay tracking error.", ex);
            }
        }

        internal bool RequestReplay(int number, out string error)
        {
            error = null;
            if (!IsReady)
            {
                error = "MissionLauncher is not ready yet.";
                return false;
            }
            ReplayMission mission = Catalog.Find(number);
            if (mission == null)
            {
                error = $"Unknown mission {number}.";
                return false;
            }
            string locked = MissionApi.LockReason(mission);
            if (locked != null)
            {
                error = locked;
                return false;
            }
            if (_observer.SessionActive || _globals.IsReplayRequested() || _globals.IsReplayControllerRunning())
            {
                error = "A replay is already running.";
                return false;
            }
            if (Function.Call<bool>(Hash.GET_MISSION_FLAG))
            {
                error = "A mission is already running.";
                return false;
            }
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead)
            {
                error = "The player must be alive.";
                return false;
            }

            Catalog.RefreshCompletion();
            if (Catalog.CompletionAvailable && !mission.IsCompleted)
                Log.Warn($"{mission.Id} {mission.Title} has not been completed in this save yet (the game's own replay menu only lists completed missions).");

            _globals.RequestReplay((int)mission.Category, mission.GameIndex, player.Position);
            if (Config.AutoConfirmRestorePoint) _confirmer.Arm();
            Log.Info($"Replay requested: {mission.Id} {mission.Title} ({mission.Code}).");
            return true;
        }
    }
}
