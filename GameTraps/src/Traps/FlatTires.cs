using GameTraps.Core;
using GTA;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class FlatTires : Trap
    {
        private static readonly int[] Wheels = { 0, 1, 2, 3, 4, 5, 45, 47 };

        public FlatTires(TrapsConfig config) : base(config)
        {
        }

        public override string Id => TrapIds.FlatTires;
        public override string Title => "Flat tires";
        public override string Subtitle => "Bang, bang, bang, bang.";
        public override bool Enabled => Config.FlatTires.Enabled;

        public override bool CanStart(out string reason)
        {
            reason = null;
            Ped player = Player;
            if (!player.IsInVehicle()) reason = "waiting for a vehicle";
            else
            {
                Model model = player.CurrentVehicle.Model;
                if (!(model.IsCar || model.IsBike || model.IsQuadBike || model.IsBicycle)) reason = "waiting for a vehicle with tires";
            }
            return reason == null;
        }

        public override void Start()
        {
            Vehicle vehicle = Player.CurrentVehicle;
            Function.Call(Hash.SET_VEHICLE_TYRES_CAN_BURST, vehicle, true);
            foreach (int wheel in Wheels) Function.Call(Hash.SET_VEHICLE_TYRE_BURST, vehicle, wheel, true, 1000f);
        }

        public override bool Update() => false;

        public override void Stop(bool interrupted)
        {
        }
    }
}
