using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using GameTraps.Core;
using GTA;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class AnimalTransform : Trap
    {
        private static readonly int[] WheelControls = { 19, 166, 167, 168, 169 };
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "a_c_cat_01", "a cat" },
            { "a_c_hen", "a hen" },
            { "a_c_pig", "a pig" },
            { "a_c_cow", "a cow" },
            { "a_c_deer", "a deer" },
            { "a_c_boar", "a boar" },
            { "a_c_coyote", "a coyote" },
            { "a_c_rabbit_01", "a rabbit" },
            { "a_c_chimp", "a chimp" },
            { "a_c_westy", "a westie" },
            { "a_c_pug", "a pug" },
            { "a_c_poodle", "a poodle" },
            { "a_c_retriever", "a retriever" },
            { "a_c_husky", "a husky" },
            { "a_c_mtlion", "a mountain lion" },
            { "a_c_rat", "a rat" },
        };

        private const int Cooldown = 10000;
        private readonly string _statePath;
        private int _restoredAt = -100000;
        private LoadoutState _state;
        private string _animalName;
        private int _until;
        private int _nextSubtitle;
        private bool _diedWhileAnimal;
        private bool _loadChecked;

        public AnimalTransform(TrapsConfig config, string directory) : base(config)
        {
            _statePath = Path.Combine(directory, "transform.json");
        }

        private AnimalTransformConfig Settings => Config.AnimalTransform;
        public override string Id => TrapIds.AnimalTransform;
        public override string Title => "Wild side";
        public override string Subtitle => "You are an animal now.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Player;

        public override bool CanStart(out string reason)
        {
            reason = null;
            if (PlayerInAircraft) reason = "not while flying";
            else if (Game.GameTime < _restoredAt + Cooldown) reason = "cooling down";
            else if (File.Exists(_statePath)) reason = "already transformed";
            return reason == null;
        }

        public override void Start()
        {
            string name = Spawner.Pick(Settings.Models);
            Model animal = Spawner.LoadModel(name);
            if (animal.Hash == 0) return;
            Ped player = Player;
            Spawner.WarpOutOfVehicle(player);
            Script.Wait(250);

            Log.Info($"Transformation into {name}: saving the character.");
            _state = LoadoutState.Capture(player, animal.Hash);
            SaveState();
            Log.Info($"Character saved ({_state.Weapons.Count} weapons), changing the model.");
            Spawner.Poof(player.Position);
            bool changed = Game.Player.ChangeModel(animal);
            animal.MarkAsNoLongerNeeded();
            if (!changed)
            {
                Log.Warn($"Could not turn the player into {name}.");
                DeleteState();
                _state = null;
                return;
            }
            Script.Wait(0);
            Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, Player);
            Protect(Player, true);
            _animalName = Names.TryGetValue(name, out string label) ? label : name;
            _until = StartTimer(Settings.Duration);
            _nextSubtitle = 0;
            _diedWhileAnimal = false;
            Log.Info($"Player turned into {name} for {Settings.Duration / 1000} s.");
        }

        public override bool Update()
        {
            if (_state == null) return false;
            foreach (int control in WheelControls) Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, control, true);
            Ped player = Player;
            bool playing = Function.Call<bool>(Hash.IS_PLAYER_PLAYING, Game.Player) && !player.IsDead;
            if (!playing)
            {
                _diedWhileAnimal = true;
                return true;
            }
            if (_diedWhileAnimal) return !Function.Call<bool>(Hash.IS_SCREEN_FADED_IN);
            if (Game.GameTime >= _until) return false;
            Protect(player, true);
            if (Game.GameTime >= _nextSubtitle)
            {
                _nextSubtitle = Game.GameTime + 1000;
                GTA.UI.Screen.ShowSubtitle($"You are {_animalName}.", 1100);
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            if (_state == null) return;
            Restore(_state);
            _state = null;
        }

        public override void Abort()
        {
            _state = null;
        }

        public override void Background()
        {
            if (_loadChecked) return;
            if (!File.Exists(_statePath))
            {
                _loadChecked = true;
                return;
            }
            if (!Function.Call<bool>(Hash.IS_PLAYER_PLAYING, Game.Player) || Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE)) return;
            _loadChecked = true;
            LoadoutState saved = LoadState();
            if (saved == null)
            {
                DeleteState();
                return;
            }
            Log.Info("A transformation was still active when the scripts were reloaded, giving the character back.");
            Restore(saved);
        }

        private void Restore(LoadoutState state)
        {
            Ped player = Player;
            if (player.Model.Hash != state.AnimalModel)
            {
                Log.Info("The player is no longer the animal, nothing to restore.");
                DeleteState();
                return;
            }
            Spawner.Poof(player.Position);
            var model = new Model(state.Model);
            if (!model.Request(3000) || !Game.Player.ChangeModel(model))
            {
                Log.Warn("Could not give the character model back, retrying on the next load.");
                return;
            }
            model.MarkAsNoLongerNeeded();
            Script.Wait(0);
            Protect(Player, false);
            state.Apply(Player);
            _restoredAt = Game.GameTime;
            DeleteState();
            Log.Info("Character model, clothes and weapons given back.");
        }

        private static void Protect(Ped ped, bool on)
        {
            if (ped == null || !ped.Exists()) return;
            ped.IsInvincible = on;
            Function.Call(Hash.SET_PLAYER_INVINCIBLE, Game.Player, on);
            Function.Call(Hash.SET_ENTITY_PROOFS, ped, on, on, on, on, on, on, on, on);
            Function.Call(Hash.SET_PED_DIES_IN_WATER, ped, !on);
            Function.Call(Hash.SET_PED_DIES_INSTANTLY_IN_WATER, ped, false);
            if (on && ped.Health < ped.MaxHealth) ped.Health = ped.MaxHealth;
        }

        private void SaveState()
        {
            try
            {
                File.WriteAllText(_statePath, new JavaScriptSerializer().Serialize(_state));
            }
            catch (Exception ex)
            {
                Log.Error("Could not write transform.json.", ex);
            }
        }

        private LoadoutState LoadState()
        {
            try
            {
                return new JavaScriptSerializer().Deserialize<LoadoutState>(File.ReadAllText(_statePath));
            }
            catch (Exception ex)
            {
                Log.Error("transform.json is invalid.", ex);
                return null;
            }
        }

        private void DeleteState()
        {
            try
            {
                if (File.Exists(_statePath)) File.Delete(_statePath);
            }
            catch (Exception ex)
            {
                Log.Error("Could not delete transform.json.", ex);
            }
        }
    }

    internal sealed class LoadoutState
    {
        private static readonly WeaponHash[] AllWeapons = (WeaponHash[])Enum.GetValues(typeof(WeaponHash));
        private static readonly WeaponComponentHash[] AllComponents = (WeaponComponentHash[])Enum.GetValues(typeof(WeaponComponentHash));

        public int Model { get; set; }
        public int AnimalModel { get; set; }
        public int Health { get; set; }
        public int Armor { get; set; }
        public List<int> Components { get; set; } = new List<int>();
        public List<int> Props { get; set; } = new List<int>();
        public List<WeaponState> Weapons { get; set; } = new List<WeaponState>();
        public uint CurrentWeapon { get; set; }

        public static LoadoutState Capture(Ped ped, int animalModel)
        {
            var state = new LoadoutState
            {
                Model = ped.Model.Hash,
                AnimalModel = animalModel,
                Health = ped.Health,
                Armor = ped.Armor,
                CurrentWeapon = unchecked((uint)Function.Call<int>(Hash.GET_SELECTED_PED_WEAPON, ped)),
            };
            for (int slot = 0; slot < 12; slot++)
            {
                state.Components.Add(Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, ped, slot));
                state.Components.Add(Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, ped, slot));
                state.Components.Add(Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION, ped, slot));
            }
            for (int anchor = 0; anchor < 8; anchor++)
            {
                state.Props.Add(Function.Call<int>(Hash.GET_PED_PROP_INDEX, ped, anchor));
                state.Props.Add(Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX, ped, anchor));
            }
            var seen = new HashSet<uint>();
            foreach (WeaponHash weapon in AllWeapons)
            {
                uint hash = (uint)weapon;
                if (!seen.Add(hash) || weapon == WeaponHash.Unarmed) continue;
                if (!Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, hash, false)) continue;
                var record = new WeaponState
                {
                    Hash = hash,
                    Ammo = Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON, ped, hash),
                    Tint = weapon == WeaponHash.Parachute ? -1 : Function.Call<int>(Hash.GET_PED_WEAPON_TINT_INDEX, ped, hash),
                };
                if (weapon != WeaponHash.Parachute) CaptureComponents(ped, hash, record);
                state.Weapons.Add(record);
            }
            return state;
        }

        private static void CaptureComponents(Ped ped, uint weapon, WeaponState record)
        {
            foreach (WeaponComponentHash component in AllComponents)
            {
                uint id = (uint)component;
                if (record.Components.Contains(id) || !Function.Call<bool>(Hash.DOES_WEAPON_TAKE_WEAPON_COMPONENT, weapon, id)) continue;
                if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON_COMPONENT, ped, weapon, id)) record.Components.Add(id);
            }
        }

        public void Apply(Ped ped)
        {
            Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, ped);
            for (int slot = 0; slot < 12 && slot * 3 + 2 < Components.Count; slot++)
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, ped, slot, Components[slot * 3], Components[slot * 3 + 1], Components[slot * 3 + 2]);
            for (int anchor = 0; anchor < 8 && anchor * 2 + 1 < Props.Count; anchor++)
            {
                int index = Props[anchor * 2];
                if (index < 0) Function.Call(Hash.CLEAR_PED_PROP, ped, anchor);
                else Function.Call(Hash.SET_PED_PROP_INDEX, ped, anchor, index, Props[anchor * 2 + 1], true);
            }
            if (Health > 0) ped.Health = Math.Min(Health, ped.MaxHealth);
            ped.Armor = Armor;
            Function.Call(Hash.REMOVE_ALL_PED_WEAPONS, ped, true);
            foreach (WeaponState weapon in Weapons)
            {
                Function.Call(Hash.GIVE_WEAPON_TO_PED, ped, weapon.Hash, 0, false, false);
                Function.Call(Hash.SET_PED_AMMO, ped, weapon.Hash, weapon.Ammo);
                foreach (uint component in weapon.Components)
                    if (Function.Call<bool>(Hash.DOES_WEAPON_TAKE_WEAPON_COMPONENT, weapon.Hash, component))
                        Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED, ped, weapon.Hash, component);
                if (weapon.Tint >= 0) Function.Call(Hash.SET_PED_WEAPON_TINT_INDEX, ped, weapon.Hash, weapon.Tint);
            }
            if (Weapons.Any(w => w.Hash == CurrentWeapon)) Function.Call(Hash.SET_CURRENT_PED_WEAPON, ped, CurrentWeapon, true);
        }
    }

    internal sealed class WeaponState
    {
        public uint Hash { get; set; }
        public int Ammo { get; set; }
        public int Tint { get; set; }
        public List<uint> Components { get; set; } = new List<uint>();
    }
}
