using System;
using GTA;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal sealed class CharacterService
    {
        private const int SyncInterval = 250;
        private readonly EnvironmentConfig _config;
        private readonly StoryGlobals _globals;
        private readonly StateStore _store;
        private const int FlowBusyDelay = 5000;
        private const int FlowBusyInterval = 2000;
        private int _nextSync;
        private int _flowBusyCheckAt;
        private int _nextAutoSwitch;

        public CharacterService(EnvironmentConfig config, StoryGlobals globals, StateStore store)
        {
            _config = config;
            _globals = globals;
            _store = store;
        }

        public event Action<PlayableCharacter> Switched;

        public bool IsUnlocked(PlayableCharacter character) => !_config.CharacterLocking || _store.State.For(character).Unlocked;

        public void Apply()
        {
            foreach (PlayableCharacter character in Characters.All)
                _globals.SetCharacterIntroduced(character);
            if (_config.CharacterLocking) _globals.EnsureMainStrandActive();
            EnsureOneUnlocked();
            Sync(true);
            _nextSync = Game.GameTime + SyncInterval;
            _flowBusyCheckAt = Game.GameTime + FlowBusyDelay;
        }

        public void Tick()
        {
            if (Game.GameTime < _nextSync) return;
            _nextSync = Game.GameTime + SyncInterval;
            if (Game.GameTime >= _flowBusyCheckAt && !Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE) && Function.Call<bool>(Hash.IS_SCREEN_FADED_IN))
            {
                _flowBusyCheckAt = Game.GameTime + FlowBusyInterval;
                if (_globals.FlowHalted && _globals.ClearFlowBusy()) Log.Info("Story flow busy flag cleared (character wheel unblocked).");
            }
            Sync(false);
            TrackCurrent();
            AutoSwitch();
        }

        private void AutoSwitch()
        {
            if (!_config.CharacterLocking || Game.GameTime < _nextAutoSwitch) return;
            PlayableCharacter? current = Characters.Current;
            if (current == null || IsUnlocked(current.Value)) return;
            _nextAutoSwitch = Game.GameTime + 3000;
            PlayableCharacter? target = null;
            int start = _store.State.StartCharacter;
            if (Characters.IsValid(start) && IsUnlocked((PlayableCharacter)start)) target = (PlayableCharacter)start;
            foreach (PlayableCharacter character in Characters.All)
                if (target == null && IsUnlocked(character)) target = character;
            if (target == null) return;
            if (SwitchTo(target.Value, out string error))
                GTA.UI.Notification.PostTicker($"~y~{current.Value}~s~ is locked: you now play ~g~{target.Value}~s~.", true);
            else
                Log.Debug($"Automatic switch to {target.Value} delayed: {error}");
        }

        public bool Unlock(PlayableCharacter character, bool live)
        {
            CharacterRecord record = _store.State.For(character);
            if (record.Unlocked) return false;
            record.Unlocked = true;
            _store.Save();
            Log.Info($"Character unlocked: {character}.");
            if (live)
            {
                _globals.SetCharacterIntroduced(character);
                _globals.SetCharacterAvailable(character, true);
                EnvironmentScript.Instance?.RequestApply();
                GTA.UI.Notification.PostTicker($"~g~{character}~s~ is now available.", true);
            }
            return true;
        }

        public bool Lock(PlayableCharacter character, bool live, out string error)
        {
            error = null;
            if (!_config.CharacterLocking)
            {
                error = "Character locking is disabled in config.json.";
                return false;
            }
            if (!_config.AllowRelock)
            {
                error = "Characters cannot be locked again (AllowRelock = false).";
                return false;
            }
            CharacterRecord record = _store.State.For(character);
            if (!record.Unlocked) return true;
            if (Characters.Current == character)
            {
                error = "The current character cannot be locked.";
                return false;
            }
            if (Characters.Where(c => c != character && _store.State.For(c).Unlocked).Count == 0)
            {
                error = "At least one character must stay unlocked.";
                return false;
            }
            record.Unlocked = false;
            _store.Save();
            if (live) _globals.SetCharacterAvailable(character, false);
            Log.Info($"Character locked: {character}.");
            return true;
        }

        public bool Force(PlayableCharacter character, bool unlocked, bool live)
        {
            if (!_config.CharacterLocking) return false;
            CharacterRecord record = _store.State.For(character);
            if (record.Unlocked == unlocked) return false;
            record.Unlocked = unlocked;
            _store.Save();
            if (live)
            {
                if (unlocked) _globals.SetCharacterIntroduced(character);
                _globals.SetCharacterAvailable(character, unlocked || Characters.Current == character);
                EnvironmentScript.Instance?.RequestApply();
            }
            Log.Info($"Character {(unlocked ? "unlocked" : "locked")} by the API: {character}.");
            return true;
        }

        public bool SwitchTo(PlayableCharacter character, out string error)
        {
            error = null;
            PlayableCharacter? current = Characters.Current;
            if (current == character) return true;
            Ped ped = Game.Player.Character;
            if (ped == null || !ped.Exists() || ped.IsDead)
            {
                error = "The player is not ready.";
                return false;
            }
            if (Function.Call<bool>(Hash.GET_MISSION_FLAG) || Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE) || !Function.Call<bool>(Hash.IS_SCREEN_FADED_IN))
            {
                error = "Not now.";
                return false;
            }
            Vehicle vehicle = ped.IsInVehicle() ? ped.CurrentVehicle : null;
            VehicleSeat seat = vehicle != null ? ped.SeatIndex : VehicleSeat.None;
            Model model = Characters.ModelOf(character);
            if (!model.Request(3000))
            {
                error = "The model could not be loaded.";
                return false;
            }
            if (!Game.Player.ChangeModel(model))
            {
                error = "The model could not be changed.";
                return false;
            }
            model.MarkAsNoLongerNeeded();
            Game.Player.Character.Style.SetDefaultClothes();
            if (vehicle != null && vehicle.Exists() && !Game.Player.Character.IsInVehicle())
                Game.Player.Character.SetIntoVehicle(vehicle, seat);
            _globals.SetCharacterAvailable(character, true);
            Log.Info($"Switched to {character}.");
            TrackCurrent();
            return true;
        }

        private void EnsureOneUnlocked()
        {
            if (!_config.CharacterLocking) return;
            if (Characters.Where(c => _store.State.For(c).Unlocked).Count > 0) return;
            PlayableCharacter fallback = Characters.Current
                ?? (Characters.IsValid(_store.State.StartCharacter) ? (PlayableCharacter)_store.State.StartCharacter : PlayableCharacter.Franklin);
            _store.State.For(fallback).Unlocked = true;
            _store.Save();
            Log.Warn($"No character was unlocked in state.json, {fallback} unlocked.");
        }

        private void Sync(bool force)
        {
            PlayableCharacter? current = Characters.Current;
            foreach (PlayableCharacter character in Characters.All)
            {
                bool wanted = IsUnlocked(character) || character == current;
                if (force || _globals.IsCharacterAvailable(character) != wanted)
                {
                    _globals.SetCharacterAvailable(character, wanted);
                    if (!force) Log.Debug($"Availability of {character} restored to {wanted}.");
                }
            }
        }

        private void TrackCurrent()
        {
            PlayableCharacter? current = Characters.Current;
            if (current == null) return;
            int index = (int)current.Value;
            if (_store.State.LastCharacter == index) return;
            _store.State.LastCharacter = index;
            _store.Save();
            Log.Info($"Now playing {current.Value}.");
            Switched?.Invoke(current.Value);
        }
    }
}
