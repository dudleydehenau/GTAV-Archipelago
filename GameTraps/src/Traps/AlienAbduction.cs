using System;
using System.Collections.Generic;
using System.Linq;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class AlienAbduction : Trap
    {
        private const string VisionIn = "DrugsMichaelAliensFightIn";
        private const string Vision = "DrugsMichaelAliensFight";
        private const string VisionOut = "DrugsMichaelAliensFightOut";
        private const int RespawnDelay = 1500;
        private readonly EntityTracker _entities = new EntityTracker();
        private readonly List<Ped> _aliens = new List<Ped>();
        private Prop _ufo;
        private Phase _phase;
        private int _phaseEnd;
        private int _nextSpawn;
        private float _spin;
        private Model _alienModel;
        private const int OrderInterval = 2500;
        private readonly List<uint> _alienWeapons = new List<uint>();
        private readonly List<uint> _givenWeapons = new List<uint>();
        private int _nextOrder;
        private int _protectedUntil;
        private int _nextKnockback;
        private int _savedMaxHealth;
        private int _savedHealth;
        private bool _toughened;

        public AlienAbduction(TrapsConfig config) : base(config)
        {
        }

        private enum Phase
        {
            Beam,
            Fight,
        }

        private AlienAbductionConfig Settings => Config.AlienAbduction;
        public override string Id => TrapIds.AlienAbduction;
        public override string Title => "Close encounter";
        public override string Subtitle => "They came for you.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Player | TrapChannel.Vision | TrapChannel.Police;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft ? "waiting for you to land" : null;
            return reason == null;
        }

        public override void Start()
        {
            _aliens.Clear();
            _givenWeapons.Clear();
            Ped player = Player;
            Spawner.WarpOutOfVehicle(player);
            Model ufoModel = Spawner.LoadModel(Settings.UfoModel);
            if (ufoModel.Hash != 0)
            {
                _ufo = Prop.Create(ufoModel, player.Position + new Vector3(0f, 0f, Settings.UfoHeight), false, false);
                if (_ufo != null)
                {
                    _ufo.IsPositionFrozen = true;
                    _entities.Add(_ufo);
                }
            }
            ScreenEffects.Start(VisionIn, false);
            player.IsInvincible = true;
            player.CanRagdoll = false;
            _phase = Phase.Beam;
            _phaseEnd = Game.GameTime + Settings.BeamDuration;
        }

        public override bool Update()
        {
            Ped player = Player;
            switch (_phase)
            {
                case Phase.Beam:
                    DrawBeam(player);
                    player.Velocity = new Vector3(0f, 0f, 4.5f);
                    if (Game.GameTime >= _phaseEnd) Abduct();
                    return true;
                case Phase.Fight:
                    if (Game.GameTime >= _phaseEnd || player.IsDead) return false;
                    Fight(player);
                    return true;
            }
            return false;
        }

        public override void Stop(bool interrupted)
        {
            foreach (Ped alien in _aliens)
                if (alien.Exists() && !alien.IsDead) Spawner.Poof(alien.Position);
            _aliens.Clear();
            _entities.DeleteAll();
            _ufo = null;
            ScreenEffects.FadeOut(Vision, VisionOut, 2500);
            NoPolice.End();
            Soften();
            Ped player = Player;
            player.IsInvincible = false;
            player.CanRagdoll = true;
            player.IsPositionFrozen = false;
            if (Settings.RemoveWeaponsAfter)
                foreach (uint weapon in _givenWeapons)
                    if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, player, weapon, false)) Function.Call(Hash.REMOVE_WEAPON_FROM_PED, player, weapon);
            _givenWeapons.Clear();
            if (GTA.UI.Screen.IsFadedOut || GTA.UI.Screen.IsFadingOut) GTA.UI.Screen.FadeIn(500);
        }

        private void DrawBeam(Ped player)
        {
            if (_ufo == null || !_ufo.Exists()) return;
            _spin = (_spin + 3f) % 360f;
            _ufo.Heading = _spin;
            Vector3 from = _ufo.Position - new Vector3(0f, 0f, 2f);
            Vector3 down = (player.Position - from).Normalized;
            Function.Call(Hash.DRAW_SPOT_LIGHT, from.X, from.Y, from.Z, down.X, down.Y, down.Z, 120, 255, 120, 60f, 30f, 0f, 18f, 1f);
            Function.Call(Hash.DRAW_LIGHT_WITH_RANGE, player.Position.X, player.Position.Y, player.Position.Z, 120, 255, 120, 6f, 4f);
        }

        private void Abduct()
        {
            GTA.UI.Screen.FadeOut(800);
            int deadline = Environment.TickCount + 3000;
            while (!GTA.UI.Screen.IsFadedOut && Environment.TickCount < deadline) Script.Wait(0);
            _entities.DeleteAll();
            _ufo = null;

            Destination destination = Spawner.Pick(Settings.Destinations);
            Log.Info($"Abducted to {destination.Name}.");
            Spawner.Teleport(destination);
            Ped player = Player;
            player.Velocity = Vector3.Zero;
            player.IsInvincible = true;
            player.CanRagdoll = true;
            player.Armor = 100;
            Toughen(player);
            NoPolice.Begin();
            _protectedUntil = Game.GameTime + Settings.SpawnProtection;

            _alienModel = Spawner.LoadModel(Settings.AlienModel);
            _alienWeapons.Clear();
            foreach (string name in Settings.AlienWeapons) _alienWeapons.Add(unchecked((uint)Safety.Joaat(name)));
            if (_alienWeapons.Count == 0) _alienWeapons.Add(unchecked((uint)Safety.Joaat("WEAPON_UNARMED")));
            uint first = 0;
            foreach (string name in Settings.PlayerWeapons)
            {
                uint weapon = unchecked((uint)Safety.Joaat(name));
                if (first == 0) first = weapon;
                if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, player, weapon, false))
                {
                    Function.Call(Hash.SET_PED_AMMO, player, weapon, Settings.PlayerAmmo);
                    continue;
                }
                Function.Call(Hash.GIVE_WEAPON_TO_PED, player, weapon, Settings.PlayerAmmo, false, false);
                if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, player, weapon, false)) _givenWeapons.Add(weapon);
                else Log.Warn($"Weapon {name} could not be given (not available in this game version?).");
            }
            if (first != 0) Function.Call(Hash.SET_CURRENT_PED_WEAPON, player, first, true);
            if (_alienModel.Hash != 0)
                for (int i = 0; i < Settings.Aliens; i++) SpawnAlien(player);

            ScreenEffects.Start(Vision, true);
            Script.Wait(300);
            GTA.UI.Screen.FadeIn(1500);
            _phase = Phase.Fight;
            _phaseEnd = StartTimer(Settings.FightDuration);
            _nextSpawn = 0;
            _nextOrder = Game.GameTime + OrderInterval;
        }

        private void Fight(Ped player)
        {
            NoPolice.Keep();
            player.IsInvincible = Settings.InvinciblePlayer || Game.GameTime < _protectedUntil;
            if (Settings.CannotDie && !player.IsInvincible)
            {
                int floor = 100 + (player.MaxHealth - 100) / 4;
                if (player.Health < floor) player.Health = floor;
            }
            Knockback(player);
            if (Game.GameTime >= _nextOrder)
            {
                _nextOrder = Game.GameTime + OrderInterval;
                foreach (Ped alien in _aliens)
                    if (alien.Exists() && !alien.IsDead && !Function.Call<bool>(Hash.IS_PED_IN_COMBAT, alien, player))
                        Function.Call(Hash.TASK_COMBAT_PED, alien, player, 0, 16);
            }
            foreach (Ped dead in _aliens.Where(a => a.Exists() && a.IsDead).ToList())
            {
                dead.AttachedBlip?.Delete();
                _aliens.Remove(dead);
                if (_nextSpawn == 0) _nextSpawn = Game.GameTime + RespawnDelay;
            }
            _aliens.RemoveAll(a => !a.Exists());
            if (_alienModel.Hash == 0 || _aliens.Count >= Settings.Aliens || _nextSpawn == 0 || Game.GameTime < _nextSpawn) return;
            SpawnAlien(player);
            _nextSpawn = _aliens.Count < Settings.Aliens ? Game.GameTime + RespawnDelay : 0;
        }

        private bool _outfitLogged;

        private void DressAlien(Ped alien)
        {
            Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, alien);
            Function.Call(Hash.CLEAR_ALL_PED_PROPS, alien);
            if (Settings.AlienOutfit != null)
                foreach (OutfitPart part in Settings.AlienOutfit)
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, alien, part.Slot, part.Drawable, part.Texture, 0);
            if (_outfitLogged) return;
            _outfitLogged = true;
            var counts = new List<string>();
            for (int slot = 0; slot < 12; slot++)
                counts.Add(slot + ":" + Function.Call<int>(Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS, alien, slot));
            Log.Info("Alien outfit slots (slot:variations) " + string.Join(" ", counts) + ".");
        }

        private void Knockback(Ped player)
        {
            if (Settings.KnockbackForce <= 0f || Game.GameTime < _nextKnockback) return;
            bool hit = false;
            foreach (uint weapon in _alienWeapons)
                if (Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_WEAPON, player, weapon, 0)) hit = true;
            if (!hit) return;
            Function.Call(Hash.CLEAR_ENTITY_LAST_WEAPON_DAMAGE, player);
            _nextKnockback = Game.GameTime + 1200;
            Ped shooter = _aliens.Where(a => a.Exists() && !a.IsDead).OrderBy(a => a.Position.DistanceToSquared(player.Position)).FirstOrDefault();
            Vector3 away = shooter != null ? (player.Position - shooter.Position).Normalized : -player.ForwardVector;
            player.CanRagdoll = true;
            Function.Call(Hash.SET_PED_TO_RAGDOLL, player, 2500, 3500, 0, false, false, false);
            player.Velocity = new Vector3(away.X * Settings.KnockbackForce, away.Y * Settings.KnockbackForce, Settings.KnockbackForce * 0.6f);
            Function.Call(Hash.SHAKE_GAMEPLAY_CAM, "SMALL_EXPLOSION_SHAKE", 0.4f);
        }

        private void Toughen(Ped player)
        {
            if (!Settings.CannotDie || _toughened) return;
            _toughened = true;
            _savedMaxHealth = player.MaxHealth;
            _savedHealth = player.Health;
            player.MaxHealth = Settings.FightHealth;
            player.Health = Settings.FightHealth;
            Function.Call(Hash.SET_ENTITY_PROOFS, player, false, true, false, true, false, false, false, true);
        }

        private void Soften()
        {
            if (!_toughened) return;
            _toughened = false;
            Ped player = Player;
            Function.Call(Hash.SET_ENTITY_PROOFS, player, false, false, false, false, false, false, false, false);
            player.MaxHealth = _savedMaxHealth;
            player.Health = System.Math.Max(101, System.Math.Min(_savedHealth, _savedMaxHealth));
        }

        private void SpawnAlien(Ped player)
        {
            Vector3 position = Spawner.GroundAround(player.Position, Spawner.Range(Settings.MinDistance, Settings.MaxDistance));
            Ped alien = Ped.Create(_alienModel, position, Spawner.HeadingTowards(position, player.Position));
            if (alien == null) return;
            _entities.Add(alien);
            _aliens.Add(alien);
            Spawner.Poof(position);
            Function.Call(Hash.GIVE_WEAPON_TO_PED, alien, Spawner.Pick(_alienWeapons), 9999, false, true);
            Function.Call(Hash.SET_PED_COMBAT_ABILITY, alien, 2);
            Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, alien, 2);
            Function.Call(Hash.SET_PED_ACCURACY, alien, Settings.AlienAccuracy);
            DressAlien(alien);
            Spawner.MakeHostile(alien, true);
        }
    }
}
