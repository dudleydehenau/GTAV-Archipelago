using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal sealed class SterileSetup
    {
        private const int ForceCleanupSinglePlayer = 2;
        private readonly EnvironmentConfig _config;
        private readonly StoryGlobals _globals;
        private readonly StateStore _store;
        private readonly CharacterService _characters;
        private readonly Random _random = new Random();

        public SterileSetup(EnvironmentConfig config, StoryGlobals globals, StateStore store, CharacterService characters)
        {
            _config = config;
            _globals = globals;
            _store = store;
            _characters = characters;
        }

        public PlayableCharacter Run()
        {
            Log.Info("New game detected, building the sterile start. " + _globals.Describe());
            _globals.FlowHalted = true;
            if (!GTA.UI.Screen.IsFadedOut) GTA.UI.Screen.FadeOut(0);
            StopPrologue();
            CleanWorld();

            PlayableCharacter start = PickStart();
            _store.Reset();
            EnvironmentState state = _store.State;
            state.Sterile = true;
            state.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            state.StartCharacter = (int)start;
            state.LastCharacter = (int)start;
            state.PendingStartCharacter = -1;
            foreach (PlayableCharacter character in Characters.All)
            {
                CharacterRecord record = state.For(character);
                record.Unlocked = !_config.CharacterLocking || character == start;
                record.Money = _config.StartMoney;
            }
            _store.Save();
            Log.Info($"Start character: {start}.");

            Become(start);
            _characters.Apply();
            PlaceAt(_config.SpawnFor(start));
            RestorePlayer();
            Economy.SetCash(PlayableCharacter.Michael, _config.StartMoney);
            Economy.SetCash(PlayableCharacter.Franklin, _config.StartMoney);
            Economy.SetCash(PlayableCharacter.Trevor, _config.StartMoney);

            Function.Call(Hash.SHUTDOWN_LOADING_SCREEN);
            Script.Wait(500);
            GTA.UI.Screen.FadeIn(1500);
            Log.Info("Sterile start ready. " + _globals.Describe());
            return start;
        }

        public bool AutoSave()
        {
            if (!_config.AutoSaveAfterSetup) return true;
            if (Function.Call<bool>(Hash.IS_AUTO_SAVE_IN_PROGRESS)) return false;
            if (Function.Call<bool>(Hash.GET_MISSION_FLAG) || Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE)) return false;
            if (!Function.Call<bool>(Hash.IS_SCREEN_FADED_IN) || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE)) return false;
            if (ReplayGuard.IsBusy(_globals)) return false;
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead || !Game.Player.CanControlCharacter) return false;
            Function.Call(Hash.DO_AUTO_SAVE);
            Log.Info("Autosave requested.");
            return true;
        }

        public void StopPrologue()
        {
            int hash = _globals.PrologueScriptHash;
            if (StoryGlobals.IsScriptRunning(hash))
            {
                Log.Info("prologue1 is running, sending it a cleanup.");
                Function.Call(Hash.FORCE_CLEANUP_FOR_ALL_THREADS_WITH_THIS_NAME, "prologue1", ForceCleanupSinglePlayer);
                int deadline = System.Environment.TickCount + 10000;
                while (StoryGlobals.IsScriptRunning(hash) && System.Environment.TickCount < deadline) Script.Wait(0);
                if (StoryGlobals.IsScriptRunning(hash)) Log.Warn("prologue1 is still running after 10 s.");
                else Log.Info("prologue1 stopped.");
            }
            if (_globals.PrologueIndex >= 0 && _globals.RunningMission == _globals.PrologueIndex)
            {
                _globals.RunningMission = -1;
                Log.Info("Running mission reset.");
            }
            Function.Call(Hash.SET_MISSION_FLAG, false);
            if (_globals.ReleaseMissionCandidate()) Log.Info("Story mission slot released (mission type reset to none).");
            if (_globals.EnableSelector()) Log.Info("Character wheel re-enabled (the prologue had disabled it).");
        }

        private PlayableCharacter PickStart()
        {
            int pending = _store.State.PendingStartCharacter;
            if (Characters.IsValid(pending)) return (PlayableCharacter)pending;
            if (Characters.TryParse(_config.StartCharacter, out PlayableCharacter configured)) return configured;
            return (PlayableCharacter)_random.Next(3);
        }

        private void CleanWorld()
        {
            foreach (string ipl in _config.PrologueIpls)
                if (Function.Call<bool>(Hash.IS_IPL_ACTIVE, ipl)) Function.Call(Hash.REMOVE_IPL, ipl);
            Function.Call(Hash.SET_MINIMAP_IN_PROLOGUE, false);
            Function.Call(Hash.SET_ALLOW_STREAM_PROLOGUE_NODES, false);
            Function.Call(Hash.UNLOCK_MINIMAP_ANGLE);
            Function.Call(Hash.RENDER_SCRIPT_CAMS, false, false, 0, true, false, 0);
            Function.Call(Hash.DESTROY_ALL_CAMS, false);
            Function.Call(Hash.CLEAR_ROOM_FOR_GAME_VIEWPORT);
            Function.Call(Hash.SET_FRONTEND_RADIO_ACTIVE, true);
            Function.Call(Hash.SET_USER_RADIO_CONTROL_ENABLED, true);
            Function.Call(Hash.SET_AUDIO_FLAG, "PoliceScannerDisabled", false);
            Function.Call(Hash.SET_AUDIO_FLAG, "DisableReplayScriptStreamRecording", false);
            Function.Call(Hash.CLEAR_WEATHER_TYPE_PERSIST);
            Function.Call(Hash.SET_WEATHER_TYPE_NOW, "EXTRASUNNY");
            if (Function.Call<int>(Hash.GET_CLOCK_YEAR) < 2013) Function.Call(Hash.SET_CLOCK_DATE, 17, 8, 2013);
            Function.Call(Hash.SET_CLOCK_TIME, _config.StartHour, 0, 0);
        }

        private static void Become(PlayableCharacter character)
        {
            Model model = Characters.ModelOf(character);
            if (Game.Player.Character.Model.Hash != model.Hash)
            {
                if (!Game.Player.ChangeModel(model)) Log.Warn($"Could not switch the player model to {character}.");
                Script.Wait(0);
            }
            Game.Player.Character.Style.SetDefaultClothes();
        }

        private static void PlaceAt(SpawnPoint spawn)
        {
            var target = new Vector3(spawn.X, spawn.Y, spawn.Z);
            Ped ped = Game.Player.Character;
            ped.IsPositionFrozen = true;
            ped.Position = target;
            ped.Heading = spawn.Heading;

            Function.Call(Hash.NEW_LOAD_SCENE_START_SPHERE, target.X, target.Y, target.Z, 80f, 0);
            int deadline = System.Environment.TickCount + 15000;
            while (!Function.Call<bool>(Hash.IS_NEW_LOAD_SCENE_LOADED) && System.Environment.TickCount < deadline) Script.Wait(0);
            Function.Call(Hash.NEW_LOAD_SCENE_STOP);

            Function.Call(Hash.REQUEST_COLLISION_AT_COORD, target.X, target.Y, target.Z);
            deadline = System.Environment.TickCount + 10000;
            while (!Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, ped) && System.Environment.TickCount < deadline)
            {
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, target.X, target.Y, target.Z);
                Script.Wait(0);
            }

            var groundArgument = new OutputArgument();
            float ground = Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, target.X, target.Y, target.Z + 25f, groundArgument, false, false)
                ? groundArgument.GetResult<float>()
                : 0f;
            if (ground > 0f) target.Z = ground;
            ped.Position = target;
            ped.Heading = spawn.Heading;
            ped.IsPositionFrozen = false;
            Function.Call(Hash.SET_GAMEPLAY_CAM_RELATIVE_HEADING, 0f);
            Function.Call(Hash.SET_GAMEPLAY_CAM_RELATIVE_PITCH, 0f, 1f);
        }

        private static void RestorePlayer()
        {
            Ped ped = Game.Player.Character;
            ped.IsInvincible = false;
            ped.IsVisible = true;
            ped.Health = ped.MaxHealth;
            ped.Armor = 0;
            ped.Weapons.RemoveAll();
            Game.Player.Wanted.SetWantedLevel(0, false);
            Game.Player.Wanted.ApplyWantedLevelChangeNow(false);
            Function.Call(Hash.SET_PLAYER_CONTROL, Game.Player, true, 0);
            Function.Call(Hash.DISPLAY_HUD, true);
            Function.Call(Hash.DISPLAY_RADAR, true);
        }
    }
}
