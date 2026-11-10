using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal sealed class MissionArmory
    {
        private const int EndDelay = 3000;
        private static readonly WeaponHash[] AllWeapons = (WeaponHash[])Enum.GetValues(typeof(WeaponHash));
        private static readonly string[] Stats =
        {
            "WEAP_UNLOCK_0", "WEAP_UNLOCK_1", "WEAP_UNLOCK_2",
            "WEAP_ADDON_UNLOCK_0", "WEAP_ADDON_UNLOCK_1", "WEAP_ADDON_UNLOCK_2", "WEAP_ADDON_UNLOCK_3",
            "WEAP_ADDON_UNLOCK_4", "WEAP_ADDON_UNLOCK_5", "WEAP_ADDON_UNLOCK_6",
        };

        private readonly StateStore _store;
        private int[] _unlocks;
        private PlayableCharacter? _character;
        private HashSet<int> _carried;
        private int _offSince = -1;

        public MissionArmory(StateStore store)
        {
            _store = store;
        }

        public void Tick(bool mission)
        {
            if (mission)
            {
                _offSince = -1;
                if (_unlocks == null) Begin();
                return;
            }
            if (_unlocks == null) return;
            if (_offSince < 0) _offSince = Game.GameTime;
            if (Game.GameTime - _offSince >= EndDelay) End();
        }

        public void Release()
        {
            if (_unlocks != null) End();
        }

        private void Begin()
        {
            _unlocks = new int[3 * Stats.Length];
            for (int c = 0; c < 3; c++)
                for (int n = 0; n < Stats.Length; n++)
                {
                    int hash = StatHash(c, n);
                    var value = new OutputArgument();
                    _unlocks[c * Stats.Length + n] = Function.Call<bool>(Hash.STAT_GET_INT, hash, value, -1) ? value.GetResult<int>() : 0;
                    Function.Call(Hash.STAT_SET_INT, hash, -1, false);
                }
            _character = Characters.Current;
            _carried = Carried();
            Log.Info("Mission started: every Ammu-Nation weapon and attachment is in stock until it ends.");
        }

        private void End()
        {
            for (int c = 0; c < 3; c++)
                for (int n = 0; n < Stats.Length; n++)
                    Function.Call(Hash.STAT_SET_INT, StatHash(c, n), _unlocks[c * Stats.Length + n], false);
            _unlocks = null;
            Ped ped = Game.Player.Character;
            if (_carried == null || _character == null || Characters.Current != _character || ped == null || !ped.Exists() || ped.IsDead)
            {
                Log.Info("Mission over: Ammu-Nation stock set back.");
                return;
            }
            List<WeaponRecord> owned = _store.State.For(_character.Value).Weapons;
            var removed = new List<string>();
            foreach (WeaponHash weapon in AllWeapons)
            {
                int hash = unchecked((int)weapon);
                if (hash == unchecked((int)WeaponHash.Unarmed) || _carried.Contains(hash) || owned.Exists(w => w.Hash == hash)) continue;
                if (!Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, unchecked((uint)hash), false)) continue;
                Function.Call(Hash.REMOVE_WEAPON_FROM_PED, ped, unchecked((uint)hash));
                removed.Add(weapon.ToString());
            }
            _carried = null;
            Log.Info(removed.Count == 0
                ? "Mission over: Ammu-Nation stock set back."
                : $"Mission over: Ammu-Nation stock set back, weapons bought during the mission removed: {string.Join(", ", removed)}.");
        }

        private static HashSet<int> Carried()
        {
            var carried = new HashSet<int>();
            Ped ped = Game.Player.Character;
            if (ped == null || !ped.Exists()) return carried;
            foreach (WeaponHash weapon in AllWeapons)
                if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, (uint)weapon, false)) carried.Add(unchecked((int)weapon));
            return carried;
        }

        private static int StatHash(int character, int index) => Function.Call<int>(Hash.GET_HASH_KEY, $"SP{character}_{Stats[index]}");
    }
}
