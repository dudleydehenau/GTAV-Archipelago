using System;
using GTA;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal sealed class Economy
    {
        private const int TrackInterval = 1000;
        private static readonly string[] CashStats = { "SP0_TOTAL_CASH", "SP1_TOTAL_CASH", "SP2_TOTAL_CASH" };
        private readonly StateStore _store;
        private int _nextTrack;
        private int _weaponCheckAt = -1;
        private bool _ammoTracking;
        private bool _ammoDirty;
        private int _nextAmmoSave;
        private int _ammoTrackFrom;
        private int _firedAt = -100000;
        private const int FireWindow = 4000;

        public Economy(StateStore store)
        {
            _store = store;
        }

        public void ApplyMoney()
        {
            foreach (PlayableCharacter character in Characters.All)
                SetCash(character, _store.State.For(character).Money);
            _nextTrack = Game.GameTime + TrackInterval;
        }

        public void ScheduleWeaponCheck(int delay)
        {
            _weaponCheckAt = Game.GameTime + delay;
            _ammoTracking = false;
        }

        public void PauseAmmoTracking() => _ammoTracking = false;

        public void Tick()
        {
            Ped shooter = Game.Player.Character;
            if (shooter != null && shooter.Exists() && Function.Call<bool>(Hash.IS_PED_SHOOTING, shooter))
                _firedAt = Game.GameTime;
            if (_weaponCheckAt >= 0 && Game.GameTime >= _weaponCheckAt)
            {
                _weaponCheckAt = -1;
                EnsureWeapons();
            }
            if (Game.GameTime < _nextTrack) return;
            _nextTrack = Game.GameTime + TrackInterval;
            PlayableCharacter? current = Characters.Current;
            if (current == null) return;
            CharacterRecord record = _store.State.For(current.Value);
            TrackAmmo(record);
            int money = Game.Player.Money;
            if (record.Money == money) return;
            Log.Debug($"{current.Value} money {record.Money} -> {money}.");
            record.Money = money;
            _store.Save();
        }

        private void TrackAmmo(CharacterRecord record)
        {
            Ped ped = Game.Player.Character;
            if (_ammoTracking && Game.GameTime >= _ammoTrackFrom && ped != null && ped.Exists() && !ped.IsDead
                && !Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE) && Function.Call<bool>(Hash.IS_SCREEN_FADED_IN)
                && !Function.Call<bool>(Hash.GET_MISSION_FLAG))
            {
                int held = Function.Call<int>(Hash.GET_SELECTED_PED_WEAPON, ped);
                int heldAmmoType = Function.Call<int>(Hash.GET_PED_AMMO_TYPE_FROM_WEAPON, ped, unchecked((uint)held));
                bool fired = Game.GameTime - _firedAt < FireWindow;
                foreach (WeaponRecord weapon in record.Weapons)
                {
                    uint hash = unchecked((uint)weapon.Hash);
                    if (!Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, hash, false))
                    {
                        Arm(ped, (WeaponHash)hash, weapon.Ammo);
                        Log.Info($"Weapon {(WeaponHash)hash} was taken by the game, given back with {weapon.Ammo} rounds.");
                        continue;
                    }
                    int ammo = Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON, ped, hash);
                    var maxArgument = new OutputArgument();
                    if (Function.Call<bool>(Hash.GET_MAX_AMMO, ped, hash, maxArgument))
                    {
                        int max = maxArgument.GetResult<int>();
                        if (max > 0 && weapon.Ammo > max)
                        {
                            weapon.Ammo = max;
                            _ammoDirty = true;
                        }
                    }
                    if (ammo < 0 || ammo == weapon.Ammo) continue;
                    if (ammo == 0 && weapon.Ammo <= 1) continue;
                    if (ammo > weapon.Ammo)
                    {
                        weapon.Ammo = ammo;
                        _ammoDirty = true;
                        continue;
                    }
                    int ammoType = Function.Call<int>(Hash.GET_PED_AMMO_TYPE_FROM_WEAPON, ped, hash);
                    bool used = (fired && (weapon.Hash == held || (ammoType != 0 && ammoType == heldAmmoType)))
                        || (weapon.Hash == held && weapon.Ammo - ammo <= 5);
                    if (used)
                    {
                        weapon.Ammo = ammo;
                        _ammoDirty = true;
                        continue;
                    }
                    Function.Call(Hash.SET_PED_AMMO, ped, hash, weapon.Ammo, false);
                    Log.Info($"Ammo of {(WeaponHash)hash} dropped to {ammo} without firing, set back to {weapon.Ammo}.");
                }
            }
            if (_ammoDirty && Game.GameTime >= _nextAmmoSave)
            {
                _ammoDirty = false;
                _nextAmmoSave = Game.GameTime + 10000;
                _store.Save();
            }
        }

        public PlayableCharacter? Target(bool live)
        {
            if (live)
            {
                PlayableCharacter? current = Characters.Current;
                if (current != null) return current;
            }
            int last = _store.State.LastCharacter;
            return Characters.IsValid(last) ? (PlayableCharacter)last : (PlayableCharacter?)null;
        }

        public int Money(PlayableCharacter character) => _store.State.For(character).Money;

        public bool Give(int amount, bool live, out string error)
        {
            error = null;
            PlayableCharacter? target = Target(live);
            if (target == null)
            {
                error = "No character to give money to.";
                return false;
            }
            CharacterRecord record = _store.State.For(target.Value);
            long value = (long)record.Money + amount;
            record.Money = (int)Math.Max(0, Math.Min(int.MaxValue, value));
            _store.Save();
            if (live && Characters.Current == target) SetCash(target.Value, record.Money);
            Log.Info($"Money {(amount >= 0 ? "+" : string.Empty)}{amount} for {target.Value} (now {record.Money}).");
            return true;
        }

        public bool TrySpend(int amount, out string error)
        {
            error = null;
            PlayableCharacter? current = Characters.Current;
            if (current == null)
            {
                error = "Play as Michael, Franklin or Trevor first.";
                return false;
            }
            CharacterRecord record = _store.State.For(current.Value);
            record.Money = Game.Player.Money;
            if (record.Money < amount)
            {
                error = $"Not enough money ({GameText.Money(amount)} needed).";
                return false;
            }
            record.Money -= amount;
            _store.Save();
            SetCash(current.Value, record.Money);
            Log.Info($"{current.Value} spent {amount} (now {record.Money}).");
            return true;
        }

        public bool GiveWeapon(WeaponHash weapon, int ammo, bool live, out string error)
        {
            error = null;
            PlayableCharacter? target = Target(live);
            if (target == null)
            {
                error = "No character to give the weapon to.";
                return false;
            }
            return GiveWeaponTo(target.Value, weapon, ammo, live);
        }

        public bool GiveWeaponTo(PlayableCharacter character, WeaponHash weapon, int ammo, bool live)
        {
            PlayableCharacter? target = character;
            int hash = unchecked((int)(uint)weapon);
            ammo = Math.Max(0, ammo);
            CharacterRecord record = _store.State.For(target.Value);
            WeaponRecord existing = record.Weapons.Find(w => w.Hash == hash);
            if (existing == null) record.Weapons.Add(new WeaponRecord { Hash = hash, Ammo = ammo });
            else existing.Ammo = (int)Math.Min(int.MaxValue, (long)existing.Ammo + ammo);
            _store.Save();
            if (live && Characters.Current == target) Arm(Game.Player.Character, weapon, ammo);
            Log.Info($"Weapon {weapon} ({ammo}) for {target.Value}.");
            return true;
        }

        public bool RemoveWeapon(WeaponHash weapon, bool live)
        {
            PlayableCharacter? target = Target(live);
            if (target == null) return false;
            int hash = unchecked((int)(uint)weapon);
            bool removed = _store.State.For(target.Value).Weapons.RemoveAll(w => w.Hash == hash) > 0;
            if (removed) _store.Save();
            if (live && Characters.Current == target && Game.Player.Character.Weapons.HasWeapon(weapon))
                Game.Player.Character.Weapons.Remove(weapon);
            return removed;
        }

        public void EnsureWeapons()
        {
            PlayableCharacter? current = Characters.Current;
            if (current == null) return;
            Ped ped = Game.Player.Character;
            foreach (WeaponRecord record in _store.State.For(current.Value).Weapons)
            {
                var weapon = (WeaponHash)unchecked((uint)record.Hash);
                uint hash = unchecked((uint)record.Hash);
                if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, hash, false))
                {
                    int ammo = Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON, ped, hash);
                    if (ammo < record.Ammo)
                    {
                        Function.Call(Hash.SET_PED_AMMO, ped, hash, record.Ammo, false);
                        Log.Debug($"Ammo of {weapon} set back to {record.Ammo} for {current.Value}.");
                    }
                    continue;
                }
                Arm(ped, weapon, record.Ammo);
                Log.Debug($"Weapon {weapon} given back to {current.Value}.");
            }
            _ammoTracking = true;
            _ammoTrackFrom = Game.GameTime + 3000;
        }

        private static void Arm(Ped ped, WeaponHash weapon, int ammo)
        {
            if (ped == null || !ped.Exists()) return;
            if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, (uint)weapon, false))
            {
                Function.Call(Hash.ADD_AMMO_TO_PED, ped, (uint)weapon, Math.Max(1, ammo));
                return;
            }
            Function.Call(Hash.GIVE_WEAPON_TO_PED, ped, (uint)weapon, Math.Max(1, ammo), false, true);
        }

        public static void SetCash(PlayableCharacter character, int value) =>
            Function.Call(Hash.STAT_SET_INT, unchecked((int)StringHash.AtStringHash(CashStats[(int)character], 0u)), value, true);
    }
}
