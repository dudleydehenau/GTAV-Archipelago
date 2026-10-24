using GameTraps.Core;
using GTA;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class Ragdoll : Trap
    {
        public Ragdoll(TrapsConfig config) : base(config)
        {
        }

        public override string Id => TrapIds.Ragdoll;
        public override string Title => "Banana peel";
        public override string Subtitle => "Oops.";
        public override bool Enabled => Config.Ragdoll.Enabled;

        public override bool CanStart(out string reason)
        {
            reason = null;
            Ped player = Player;
            if (player.IsInVehicle() && !(player.CurrentVehicle.Model.IsBike || player.CurrentVehicle.Model.IsBicycle || player.CurrentVehicle.Model.IsQuadBike))
                reason = "waiting for you to be on foot";
            return reason == null;
        }

        public override void Start()
        {
            Ped player = Player;
            int duration = Config.Ragdoll.Duration;
            player.CanRagdoll = true;
            Function.Call(Hash.SET_PED_TO_RAGDOLL, player, duration, duration + 1000, 0, false, false, false);
        }

        public override bool Update() => false;

        public override void Stop(bool interrupted)
        {
        }
    }
}
