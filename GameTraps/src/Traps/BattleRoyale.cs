using System;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class BattleRoyale : Trap
    {
        private const int WallMarker = 1;
        private Vector3 _center;
        private Blip _zone;
        private Blip _centerBlip;
        private int _start;
        private int _until;
        private int _nextDamage;
        private int _nextBlip;
        private bool _outside;
        private bool _entered;
        private bool _won;
        private Ped _enemy;

        public BattleRoyale(TrapsConfig config) : base(config)
        {
        }

        private BattleRoyaleConfig Settings => Config.BattleRoyale;
        public override string Id => TrapIds.BattleRoyale;
        public override string Title => "Battle royale";
        public override string Subtitle => "The storm is closing in.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Vision;

        public override void Start()
        {
            Ped player = Player;
            double angle = Spawner.Random.NextDouble() * Math.PI * 2;
            float offset = Spawner.Range(Settings.MinOffset, Settings.MaxOffset);
            _center = player.Position + new Vector3((float)Math.Cos(angle) * offset, (float)Math.Sin(angle) * offset, 0f);
            Vector3 street = World.GetNextPositionOnStreet(_center);
            if (street.DistanceTo2D(_center) < 250f) _center = street;
            _center.Z = Spawner.GroundZ(_center + new Vector3(0f, 0f, 200f), player.Position.Z);
            _start = Game.GameTime;
            _until = StartTimer(Settings.Duration);
            _nextDamage = 0;
            _nextBlip = 0;
            _outside = false;
            _entered = false;
            _won = false;
            _enemy = null;
            _centerBlip = Blip.Create(_center);
            _centerBlip.Sprite = BlipSprite.Standard;
            _centerBlip.Color = BlipColor.White;
            _centerBlip.Name = "Safe zone";
            Overrides.Subtitle("~p~Get into the zone before it closes!", 5000);
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            Ped player = Player;
            if (now >= _until || player.IsDead) return false;
            float progress = Math.Min(1f, (now - _start) / (float)Math.Max(1, Settings.Duration));
            float radius = Settings.StartRadius + (Settings.EndRadius - Settings.StartRadius) * progress;

            Function.Call(Hash.DRAW_MARKER, WallMarker, _center.X, _center.Y, _center.Z - 50f, 0f, 0f, 0f, 0f, 0f, 0f,
                radius * 2f, radius * 2f, 250f, 120, 40, 255, 70, false, false, 2, false, 0, 0, false);

            if (now >= _nextBlip)
            {
                _nextBlip = now + 1000;
                _zone?.Delete();
                _zone = Blip.Create(_center, radius);
                _zone.Color = BlipColor.Purple;
                _zone.Alpha = 90;
            }

            bool outside = player.Position.DistanceTo2D(_center) > radius;
            if (!outside && !_entered)
            {
                _entered = true;
                SpawnEnemy(radius);
            }
            if (_enemy != null && _enemy.Exists() && _enemy.IsDead && !_won)
            {
                _won = true;
                _enemy.AttachedBlip?.Delete();
                Overrides.Beep("CHECKPOINT_PERFECT", "HUD_MINI_GAME_SOUNDSET");
                return false;
            }
            if (outside != _outside)
            {
                _outside = outside;
                if (outside)
                {
                    Overrides.Timecycle("glasses_purple", 0.8f);
                    Overrides.Beep("5_SEC_WARNING", "HUD_MINI_GAME_SOUNDSET");
                }
                else
                {
                    Overrides.ClearTimecycle();
                }
            }
            int grace = Settings.Grace - (now - _start);
            if (outside && grace > 0)
            {
                Overrides.Banner($"REACH THE ZONE: {(grace + 999) / 1000} s", System.Drawing.Color.MediumPurple, 90f, 1f);
            }
            else if (outside)
            {
                Overrides.Banner("OUTSIDE THE ZONE", System.Drawing.Color.MediumPurple, 90f, 1f);
                if (now >= _nextDamage)
                {
                    _nextDamage = now + Settings.DamageInterval;
                    player.Health = Math.Max(0, player.Health - Settings.DamagePerTick);
                    Function.Call(Hash.SHAKE_GAMEPLAY_CAM, "SMALL_EXPLOSION_SHAKE", 0.05f);
                }
            }
            return true;
        }

        private void SpawnEnemy(float radius)
        {
            Model model = Spawner.LoadModel(Settings.EnemyModel);
            if (model.Hash == 0) return;
            Ped player = Player;
            Vector3 spot = Spawner.GroundAround(_center, Spawner.Range(0f, System.Math.Min(radius * 0.4f, 40f)));
            _enemy = Ped.Create(model, spot, Spawner.HeadingTowards(spot, player.Position));
            if (_enemy == null) return;
            _enemy.MaxHealth = Settings.EnemyHealth;
            _enemy.Health = Settings.EnemyHealth;
            _enemy.Armor = 100;
            Spawner.Give(_enemy, Settings.EnemyWeapon);
            Function.Call(Hash.SET_PED_ACCURACY, _enemy, 25);
            Function.Call(Hash.SET_PED_COMBAT_ABILITY, _enemy, 2);
            Spawner.MakeHostile(_enemy, true);
            if (_enemy.AttachedBlip != null) _enemy.AttachedBlip.Name = Settings.EnemyName;
            Spawner.Poof(spot + new Vector3(0f, 0f, 0.5f));
            Overrides.Subtitle($"~r~{Settings.EnemyName}~s~ is the last one standing. Take him out!", 5000);
        }

        public override void Stop(bool interrupted)
        {
            if (_enemy != null && _enemy.Exists())
            {
                _enemy.AttachedBlip?.Delete();
                if (!_enemy.IsDead) Spawner.Poof(_enemy.Position);
                _enemy.Delete();
            }
            _enemy = null;
            _zone?.Delete();
            _zone = null;
            _centerBlip?.Delete();
            _centerBlip = null;
            if (_outside) Overrides.ClearTimecycle();
            _outside = false;
            if (!interrupted && !Player.IsDead)
            {
                if (_won) Overrides.Subtitle("~p~Victory royale! Top 1!", 5000);
                else Overrides.Subtitle("~p~You survived the storm.", 4000);
            }
        }
    }
}
