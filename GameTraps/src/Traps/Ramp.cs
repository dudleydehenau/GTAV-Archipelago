using System;
using GameTraps.Core;
using GTA;
using GTA.Math;

namespace GameTraps.Traps
{
    internal sealed class Ramp : Trap
    {
        private readonly EntityTracker _entities = new EntityTracker();
        private int _until;

        public Ramp(TrapsConfig config) : base(config)
        {
        }

        private RampConfig Settings => Config.Ramp;
        public override string Id => TrapIds.Ramp;
        public override string Title => "Ramp!";
        public override string Subtitle => "Watch the road.";
        public override bool Enabled => Settings.Enabled;

        public override bool CanStart(out string reason)
        {
            reason = null;
            Ped player = Player;
            if (!player.IsInVehicle() || PlayerInAircraft || player.CurrentVehicle.Model.IsBoat || player.CurrentVehicle.Speed < Settings.MinSpeed)
                reason = "waiting for you to drive";
            return reason == null;
        }

        public override void Start()
        {
            Model model = Spawner.LoadAny(Settings.Models);
            if (model.Hash == 0) return;
            Ped player = Player;
            Entity mover = player.IsInVehicle() ? (Entity)player.CurrentVehicle : player;
            Vector3 forward = mover.ForwardVector;
            float speed = mover.Velocity.Length();
            float distance = Math.Max(Settings.MinDistance, Math.Min(Settings.MaxDistance, speed * Settings.SecondsAhead));
            Vector3 position = mover.Position + forward * distance;
            position.Z = Spawner.GroundZ(position + new Vector3(0f, 0f, 5f), mover.Position.Z - 0.5f);
            var rotation = new Vector3(0f, 0f, mover.Heading + Settings.HeadingOffset);
            Prop ramp = Prop.Create(model, position, rotation, false, false);
            if (ramp == null) return;
            ramp.IsPositionFrozen = true;
            _entities.Add(ramp);
            _until = Game.GameTime + Settings.Lifetime;
            Log.Info($"Ramp placed {distance:0} m ahead.");
        }

        public override bool Update() => Game.GameTime < _until;

        public override void Stop(bool interrupted) => _entities.DeleteAll();
    }
}
