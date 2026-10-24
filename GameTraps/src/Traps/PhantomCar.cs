using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class PhantomCar : Trap
    {
        private readonly EntityTracker _entities = new EntityTracker();
        private readonly Ambience _ambience = new Ambience();
        private Vehicle _car;
        private Ped _ghost;
        private int _until;
        private int _nextVanish;
        private int _nextOrder;

        public PhantomCar(TrapsConfig config) : base(config)
        {
        }

        private PhantomCarConfig Settings => Config.PhantomCar;
        public override string Id => TrapIds.PhantomCar;
        public override string Title => "Phantom car";
        public override string Subtitle => "Nobody is driving it.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Vision | TrapChannel.Weather;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft ? "not while flying" : null;
            return reason == null;
        }

        public override void Start()
        {
            Model model = Spawner.LoadAny(Settings.Models);
            Model ghost = Spawner.LoadModel("s_m_m_movprem_01");
            if (model.Hash == 0 || ghost.Hash == 0) return;
            _ambience.Night(Settings.Hour);
            _ambience.Weather(Settings.Weather);
            _ambience.Timecycle(Settings.Timecycle, 0.7f);
            Vector3 position = Spawner.StreetBehind(50f);
            _car = Vehicle.Create(model, position, Spawner.HeadingTowards(position, Player.Position));
            if (_car == null) return;
            _entities.Add(_car);
            _car.IsInvincible = true;
            Function.Call(Hash.SET_VEHICLE_COLOURS, _car, 12, 12);
            Function.Call(Hash.SET_VEHICLE_LIGHTS, _car, 2);
            Function.Call(Hash.SET_VEHICLE_FULLBEAM, _car, true);
            Function.Call(Hash.SET_VEHICLE_ENGINE_ON, _car, true, true, false);
            Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, _car, 4);
            _ghost = _car.CreatePedOnSeat(VehicleSeat.Driver, ghost);
            if (_ghost != null)
            {
                _entities.Add(_ghost);
                _ghost.IsVisible = false;
                _ghost.IsInvincible = true;
                _ghost.BlockPermanentEvents = true;
                Function.Call(Hash.SET_DRIVER_ABILITY, _ghost, 1f);
                Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, _ghost, 1f);
            }
            Blip blip = _car.AddBlip();
            blip.Color = BlipColor.Red;
            Spawner.Poof(position);
            Overrides.Beep("Bed", "WastedSounds");
            _until = StartTimer(Settings.Duration);
            _nextVanish = Game.GameTime + Settings.VanishInterval;
            _nextOrder = 0;
            Overrides.Subtitle("Headlights in the fog... and no one behind the wheel.", 5000);
        }

        public override bool Update()
        {
            if (Game.GameTime >= _until || Player.IsDead) return false;
            if (_car == null || !_car.Exists()) return false;
            Ped player = Player;
            Entity target = player.IsInVehicle() ? (Entity)player.CurrentVehicle : player;
            Vector3 front = _car.Position + _car.ForwardVector * 3f + new Vector3(0f, 0f, 0.5f);
            Function.Call(Hash.DRAW_LIGHT_WITH_RANGE, front.X, front.Y, front.Z, 255, 0, 0, 12f, 6f);
            float distance = _car.Position.DistanceTo(target.Position);

            if (Game.GameTime >= _nextVanish || distance > 150f)
            {
                _nextVanish = Game.GameTime + Settings.VanishInterval;
                Spawner.Poof(_car.Position);
                Vector3 behind = Spawner.StreetBehind(35f);
                _car.Position = behind;
                _car.Heading = Spawner.HeadingTowards(behind, target.Position);
                _car.ForwardSpeed = Settings.Speed * 0.5f;
                Spawner.Poof(behind);
                Overrides.Beep("Bed", "WastedSounds");
                _nextOrder = 0;
            }

            if (Game.GameTime >= _nextOrder && _ghost != null && _ghost.Exists())
            {
                _nextOrder = Game.GameTime + 3000;
                Function.Call(Hash.TASK_VEHICLE_CHASE, _ghost, player);
                Function.Call(Hash.SET_TASK_VEHICLE_CHASE_IDEAL_PURSUIT_DISTANCE, _ghost, 0f);
            }

            if (distance < Settings.ChargeDistance && _car.IsOnAllWheels)
            {
                Vector3 direction = (target.Position - _car.Position).Normalized;
                float forward = Vector3.Dot(direction, _car.ForwardVector);
                if (forward > 0.6f && _car.Speed < Settings.Speed)
                    _car.ForwardSpeed = System.Math.Min(Settings.Speed, _car.Speed + 1.2f);
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            if (_car != null && _car.Exists()) Spawner.Poof(_car.Position);
            _entities.DeleteAll();
            _car = null;
            _ghost = null;
            _ambience.Restore();
        }
    }
}
