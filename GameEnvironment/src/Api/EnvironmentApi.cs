using System;
using System.Collections.Generic;
using GameEnvironment.Core;
using GTA;

namespace GameEnvironment
{
    public static class EnvironmentApi
    {
        public const int ApiVersion = 5;

        private static EnvironmentScript Core => EnvironmentScript.Instance;

        public static bool IsReady => Core != null && Core.IsReady;

        public static EnvironmentMode Mode => Core?.Mode ?? EnvironmentMode.Starting;

        public static bool IsSterile => Mode == EnvironmentMode.Sterile;

        public static bool IsReplayActive => Core != null && Core.IsBusy;

        public static PlayableCharacter? CurrentCharacter => Characters.Current;

        public static bool OpenDoorsEnabled
        {
            get => Core != null && Core.Config.OpenDoors.Enabled;
            set
            {
                if (Core == null) return;
                Core.Config.OpenDoors.Enabled = value;
                if (!value) Core.Doors.Release("API");
            }
        }

        public static bool AreDoorsOpen => Core != null && Core.Doors.IsActive;

        public static PlayableCharacter? StartCharacter
        {
            get
            {
                int start = Core?.Store.State.StartCharacter ?? -1;
                return Characters.IsValid(start) ? (PlayableCharacter)start : (PlayableCharacter?)null;
            }
        }

        public static bool CharacterLockingEnabled => Core != null && Core.Config.CharacterLocking;

        public static IReadOnlyList<PlayableCharacter> UnlockedCharacters =>
            Core == null ? (IReadOnlyList<PlayableCharacter>)Array.Empty<PlayableCharacter>() : Characters.Where(c => Core.Roster.IsUnlocked(c));

        public static bool IsCharacterUnlocked(PlayableCharacter character) => Core != null && Core.Roster.IsUnlocked(character);

        public static bool UnlockCharacter(PlayableCharacter character)
        {
            if (!CanEdit(out _)) return false;
            if (!Core.Roster.Unlock(character, Core.IsLive)) return false;
            RaiseCharacterUnlocked(character);
            return true;
        }

        public static bool LockCharacter(PlayableCharacter character, out string error)
        {
            if (!CanEdit(out error)) return false;
            return Core.Roster.Lock(character, Core.IsLive, out error);
        }

        public static bool SetUnlockedCharacters(IEnumerable<PlayableCharacter> characters, out string error)
        {
            if (!CanEdit(out error)) return false;
            var wanted = new HashSet<PlayableCharacter>(characters ?? Array.Empty<PlayableCharacter>());
            if (wanted.Count == 0)
            {
                error = "At least one character must stay unlocked.";
                return false;
            }
            foreach (PlayableCharacter character in Characters.All)
            {
                bool unlock = wanted.Contains(character);
                if (Core.Roster.Force(character, unlock, Core.IsLive) && unlock) RaiseCharacterUnlocked(character);
            }
            return true;
        }

        public static string GameId => Core != null && Core.Store.State.Sterile ? Core.Store.State.CreatedAt : null;

        public static void SetNextStartCharacter(PlayableCharacter? character)
        {
            if (Core == null) return;
            Core.Store.State.PendingStartCharacter = character.HasValue ? (int)character.Value : -1;
            Core.Store.Save();
        }

        public static int GetMoney(PlayableCharacter character) => Core?.Economy.Money(character) ?? 0;

        public static bool GiveMoney(int amount, out string error)
        {
            if (!CanEdit(out error)) return false;
            return Core.Economy.Give(amount, Core.IsLive, out error);
        }

        public static bool GiveWeapon(WeaponHash weapon, int ammo, out string error)
        {
            if (!CanEdit(out error)) return false;
            return Core.Economy.GiveWeapon(weapon, ammo, Core.IsLive, out error);
        }

        public static bool GiveWeaponTo(PlayableCharacter character, WeaponHash weapon, int ammo, out string error)
        {
            if (!CanEdit(out error)) return false;
            return Core.Economy.GiveWeaponTo(character, weapon, ammo, Core.IsLive);
        }

        public static bool SwitchCharacter(PlayableCharacter character, out string error)
        {
            if (!CanEdit(out error)) return false;
            if (!Core.IsLive)
            {
                error = "Not during a replay.";
                return false;
            }
            if (!Core.Roster.IsUnlocked(character))
            {
                error = character + " is locked.";
                return false;
            }
            return Core.Roster.SwitchTo(character, out error);
        }

        public static string Owner
        {
            get => Core?.Store.State.Owner;
            set
            {
                if (Core == null) return;
                Core.Store.State.Owner = value;
                Core.Store.Save();
            }
        }

        public static bool ResetProgress(string owner, out string error)
        {
            if (!CanEdit(out error)) return false;
            if (!Core.IsLive)
            {
                error = "Not during a replay.";
                return false;
            }
            Core.ResetProgress(owner);
            return true;
        }

        public static bool RemoveWeapon(WeaponHash weapon)
        {
            if (!CanEdit(out _)) return false;
            return Core.Economy.RemoveWeapon(weapon, Core.IsLive);
        }

        public static IReadOnlyList<WeaponHash> GetWeapons(PlayableCharacter character)
        {
            var list = new List<WeaponHash>();
            if (Core == null) return list;
            foreach (WeaponRecord record in Core.Store.State.For(character).Weapons)
                list.Add((WeaponHash)unchecked((uint)record.Hash));
            return list;
        }

        public static IReadOnlyList<OwnedVehicle> Vehicles
        {
            get
            {
                var list = new List<OwnedVehicle>();
                if (Core == null) return list;
                foreach (VehicleRecord record in Core.Garage.Records)
                    list.Add(ToPublic(record));
                return list;
            }
        }

        public static OwnedVehicle FindVehicle(int id)
        {
            VehicleRecord record = Core?.Store.State.FindVehicle(id);
            return record == null ? null : ToPublic(record);
        }

        public static OwnedVehicle GrantVehicle(VehicleHash model, out string error) => GrantVehicle(new Model(model), out error);

        public static OwnedVehicle GrantVehicle(string modelName, out string error) => GrantVehicle(new Model(modelName ?? string.Empty), out error);

        public static bool RequestDelivery(int vehicleId, out string error)
        {
            if (!CanEdit(out error)) return false;
            if (!Core.IsLive)
            {
                error = "Vehicles cannot be delivered during a replay.";
                return false;
            }
            return Core.Garage.RequestDelivery(vehicleId, out error);
        }

        public static bool RepairVehicle(int vehicleId, out string error)
        {
            if (!CanEdit(out error)) return false;
            if (!Core.IsLive)
            {
                error = "Vehicles cannot be repaired during a replay.";
                return false;
            }
            return Core.Garage.Repair(vehicleId, out error);
        }

        public static bool RepairCurrentVehicle(out string error)
        {
            if (!CanEdit(out error)) return false;
            return Core.Garage.RepairCurrent(out error);
        }

        public static bool RemoveVehicle(int vehicleId)
        {
            if (!CanEdit(out _)) return false;
            return Core.Garage.Remove(vehicleId);
        }

        public static event Action<PlayableCharacter> SterileGameCreated;
        public static event Action StateApplied;
        public static event Action<PlayableCharacter> CharacterUnlocked;
        public static event Action<OwnedVehicle> VehicleGranted;
        public static event Action<OwnedVehicle> VehicleDestroyed;

        internal static void RaiseSterileGameCreated(PlayableCharacter start) => SafeInvoke(SterileGameCreated, h => h(start));
        internal static void RaiseStateApplied() => SafeInvoke(StateApplied, h => h());
        internal static void RaiseCharacterUnlocked(PlayableCharacter character) => SafeInvoke(CharacterUnlocked, h => h(character));
        internal static void RaiseVehicleDestroyed(VehicleRecord record) => SafeInvoke(VehicleDestroyed, h => h(ToPublic(record)));

        private static OwnedVehicle GrantVehicle(Model model, out string error)
        {
            if (!CanEdit(out error)) return null;
            VehicleRecord record = Core.Garage.Grant(model, out error);
            if (record == null) return null;
            OwnedVehicle vehicle = ToPublic(record);
            SafeInvoke(VehicleGranted, h => h(vehicle));
            return vehicle;
        }

        private static OwnedVehicle ToPublic(VehicleRecord record) =>
            new OwnedVehicle(record.Id, record.Model, record.Status, record.DeliveryPending, Core.Garage.RepairCost(record), Garage.DisplayName(record.Model));

        private static bool CanEdit(out string error)
        {
            error = null;
            if (Core == null || !Core.IsReady)
            {
                error = "GameEnvironment is not ready.";
                return false;
            }
            if (Core.Mode != EnvironmentMode.Sterile)
            {
                error = "GameEnvironment is only active in a sterile game.";
                return false;
            }
            return true;
        }

        private static void SafeInvoke<T>(T multicast, Action<T> invoke) where T : Delegate
        {
            if (multicast == null) return;
            foreach (Delegate d in multicast.GetInvocationList())
            {
                try
                {
                    invoke((T)d);
                }
                catch (Exception ex)
                {
                    Log.Error($"Exception in an event handler ({d.Method.DeclaringType?.FullName}.{d.Method.Name}).", ex);
                }
            }
        }
    }
}
