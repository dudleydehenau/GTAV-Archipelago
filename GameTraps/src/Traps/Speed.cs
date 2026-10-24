using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class Speed : Trap
    {
        private readonly EntityTracker _entities = new EntityTracker();
        private Vehicle _vehicle;
        private int _until;
        private int _armedAt;
        private int _slowSince;
        private int _protectUntil;
        private bool _exploded;

        public Speed(TrapsConfig config) : base(config)
        {
        }

        private SpeedConfig Settings => Config.Speed;
        public override string Id => TrapIds.Speed;
        public override string Title => "Speed";
        public override string Subtitle => "Pop quiz, hotshot.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Player;

        public override bool CanStart(out string reason)
        {
            reason = null;
            if (PlayerInAircraft || PlayerInWater) reason = "waiting for you to be on land";
            else if (Player.IsInVehicle() && Player.CurrentVehicle.Model.IsBoat) reason = "not on a boat";
            return reason == null;
        }

        public override void Start()
        {
            _exploded = false;
            _protectUntil = 0;
            Ped player = Player;
            if (player.IsInVehicle() && player.CurrentVehicle.Driver == player)
            {
                _vehicle = player.CurrentVehicle;
            }
            else
            {
                Model bus = Spawner.LoadAny(Settings.BusModels);
                if (bus.Hash == 0) return;
                Spawner.WarpOutOfVehicle(player);
                Vector3 street = World.GetNextPositionOnStreet(player.Position);
                if (street.DistanceTo(player.Position) > 60f) street = player.Position;
                _vehicle = Vehicle.Create(bus, street + new Vector3(0f, 0f, 0.5f), player.Heading);
                if (_vehicle == null) return;
                _entities.Add(_vehicle);
                Function.Call(Hash.SET_PED_INTO_VEHICLE, player, _vehicle, -1);
                Function.Call(Hash.SET_VEHICLE_ENGINE_ON, _vehicle, true, true, false);
            }
            _until = StartTimer(Settings.Duration);
            _armedAt = Game.GameTime + Settings.ArmDelay;
            _slowSince = -1;
            Overrides.Beep("Timer_10s", "DLC_HALLOWEEN_FVJ_Sounds");
            Overrides.Subtitle($"There's a bomb on board. Above {Settings.MinSpeedKmh:0} km/h, it arms. Below, it blows up.", 6000);
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            Ped player = Player;
            if (_exploded) return now < _protectUntil;
            if (now >= _until || player.IsDead || _vehicle == null || !_vehicle.Exists() || _vehicle.IsDead) return false;

            float kmh = _vehicle.Speed * 3.6f;
            bool armed = now >= _armedAt;
            bool slow = kmh < Settings.MinSpeedKmh;
            System.Drawing.Color color = !armed ? System.Drawing.Color.Orange : slow ? System.Drawing.Color.Red : System.Drawing.Color.LimeGreen;
            string text = $"{kmh:0} / {Settings.MinSpeedKmh:0} km/h";
            if (!armed) text += $"  (arming in {(_armedAt - now + 999) / 1000})";
            Overrides.Banner(text, color, 600f, 0.9f);

            if (!player.IsInVehicle(_vehicle))
            {
                Explode();
                return true;
            }
            if (!armed) return true;
            if (!slow)
            {
                _slowSince = -1;
                return true;
            }
            if (_slowSince < 0)
            {
                _slowSince = now;
                Overrides.Beep("5_SEC_WARNING", "HUD_MINI_GAME_SOUNDSET");
            }
            if (now - _slowSince >= Settings.Tolerance) Explode();
            return true;
        }

        public override void Stop(bool interrupted)
        {
            Ped player = Player;
            if (Settings.SurviveBlast && _exploded)
            {
                Function.Call(Hash.SET_ENTITY_PROOFS, player, false, false, false, false, false, false, false, false);
                player.IsInvincible = false;
            }
            if (!_exploded && !interrupted && _vehicle != null && _vehicle.Exists())
                Overrides.Subtitle("Bomb defused. You survived.", 4000);
            _vehicle = null;
            _entities.Forget();
        }

        private void Explode()
        {
            _exploded = true;
            TrapTimer.Hide(Id);
            Ped player = Player;
            if (Settings.SurviveBlast)
            {
                player.IsInvincible = true;
                Function.Call(Hash.SET_ENTITY_PROOFS, player, true, true, true, true, true, true, true, true);
                _protectUntil = Game.GameTime + 2500;
            }
            else
            {
                _protectUntil = Game.GameTime + 1500;
            }
            if (Settings.SurviveBlast && player.IsInVehicle(_vehicle))
            {
                Spawner.WarpOutOfVehicle(player);
                Function.Call(Hash.SET_PED_TO_RAGDOLL, player, 2500, 3500, 0, false, false, false);
                player.Velocity = _vehicle.Velocity * 0.5f + new Vector3(0f, 0f, 6f);
            }
            _vehicle.Explode();
            if (!Settings.SurviveBlast && !player.IsDead)
            {
                Function.Call(Hash.ADD_OWNED_EXPLOSION, player, player.Position.X, player.Position.Y, player.Position.Z, 7, 1f, true, false, 1f);
                player.Kill();
            }
            Log.Info("Speed: the bomb went off.");
        }
    }
}
