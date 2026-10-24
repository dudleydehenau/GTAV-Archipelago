using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class LiftOff : Trap
    {
        private const int Interval = 250;
        private const int LandingTime = 6000;
        private const int ZeroGravity = 3;
        private const int NormalGravity = 0;
        private int _liftUntil;
        private int _landUntil;
        private int _next;
        private bool _lifting;

        public LiftOff(TrapsConfig config) : base(config)
        {
        }

        private LiftOffConfig Settings => Config.LiftOff;
        public override string Id => TrapIds.LiftOff;
        public override string Title => "Lift-off";
        public override string Subtitle => "Gravity is upside down.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Gravity;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft ? "waiting for you to land" : null;
            return reason == null;
        }

        public override void Start()
        {
            Function.Call(Hash.SET_GRAVITY_LEVEL, ZeroGravity);
            if (Settings.ProtectPlayer) Player.IsInvincible = true;
            _lifting = true;
            _liftUntil = StartTimer(Settings.Duration);
            _landUntil = _liftUntil + LandingTime;
            _next = 0;
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            if (_lifting && now >= _liftUntil)
            {
                _lifting = false;
                Function.Call(Hash.SET_GRAVITY_LEVEL, NormalGravity);
                TrapTimer.Hide(Id);
            }
            if (!_lifting) return now < _landUntil;
            if (now < _next) return true;
            _next = now + Interval;

            Ped player = Player;
            foreach (Vehicle vehicle in World.GetNearbyVehicles(player.Position, Settings.Radius))
                if (vehicle.Exists()) Lift(vehicle);
            foreach (Ped ped in World.GetNearbyPeds(player.Position, Settings.Radius))
            {
                if (!ped.Exists() || ped.IsInVehicle()) continue;
                Function.Call(Hash.SET_PED_TO_RAGDOLL, ped, 1500, 2000, 0, false, false, false);
                Lift(ped);
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            Function.Call(Hash.SET_GRAVITY_LEVEL, NormalGravity);
            if (Settings.ProtectPlayer) Player.IsInvincible = false;
        }

        private void Lift(Entity entity)
        {
            Vector3 velocity = entity.Velocity;
            entity.Velocity = new Vector3(velocity.X * 0.9f, velocity.Y * 0.9f, Settings.UpSpeed);
        }
    }
}
