using System.Collections.Generic;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class Bouncy : Trap
    {
        private const int Interval = 100;
        private const int Cooldown = 450;
        private readonly Dictionary<int, int> _landedAt = new Dictionary<int, int>();
        private int _until;
        private int _next;

        public Bouncy(TrapsConfig config) : base(config)
        {
        }

        private BouncyConfig Settings => Config.Bouncy;
        public override string Id => TrapIds.Bouncy;
        public override string Title => "Boing Boing";
        public override string Subtitle => "Everything bounces.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Gravity;

        public override void Start()
        {
            _landedAt.Clear();
            _until = StartTimer(Settings.Duration);
            _next = 0;
            if (Settings.ProtectPlayer) Player.IsInvincible = true;
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            if (now >= _until) return false;
            if (now < _next) return true;
            _next = now + Interval;

            Ped player = Player;
            Vector3 center = player.Position;
            foreach (Vehicle vehicle in World.GetNearbyVehicles(center, Settings.Radius))
            {
                if (!vehicle.Exists() || vehicle.IsDead) continue;
                Model model = vehicle.Model;
                if (model.IsPlane || model.IsHelicopter || model.IsBoat || model.IsSubmarine || model.IsTrain) continue;
                if (vehicle.IsInWater || !vehicle.IsOnAllWheels) continue;
                if (Ready(vehicle, now)) Bounce(vehicle, Settings.VehicleBounce);
            }
            foreach (Ped ped in World.GetNearbyPeds(center, Settings.Radius))
            {
                if (!ped.Exists() || ped.IsDead || ped.IsInVehicle()) continue;
                if (ped.IsInAir || ped.IsFalling || ped.IsRagdoll || ped.IsSwimming || ped.IsInWater) continue;
                if (Ready(ped, now)) Bounce(ped, Settings.PedBounce);
            }
            if (!player.IsInVehicle() && !player.IsDead && !player.IsInAir && !player.IsFalling && !player.IsRagdoll
                && !player.IsSwimming && !player.IsInWater && Ready(player, now))
                Bounce(player, Settings.PedBounce);
            return true;
        }

        public override void Stop(bool interrupted)
        {
            _landedAt.Clear();
            if (Settings.ProtectPlayer) Player.IsInvincible = false;
        }

        private bool Ready(Entity entity, int now)
        {
            if (_landedAt.TryGetValue(entity.Handle, out int at) && now - at < Cooldown) return false;
            _landedAt[entity.Handle] = now;
            return true;
        }

        private static void Bounce(Entity entity, float speed)
        {
            Vector3 velocity = entity.Velocity;
            entity.Velocity = new Vector3(velocity.X, velocity.Y, speed);
        }
    }
}
