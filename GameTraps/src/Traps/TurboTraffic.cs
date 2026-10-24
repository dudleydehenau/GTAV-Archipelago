using System.Collections.Generic;
using GameTraps.Core;
using GTA;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class TurboTraffic : Trap
    {
        private const int Interval = 300;
        private const int RushedDriving = 1074528293;
        private const int NormalDriving = 786603;
        private readonly HashSet<int> _boosted = new HashSet<int>();
        private readonly List<Vehicle> _vehicles = new List<Vehicle>();
        private int _until;
        private int _next;

        public TurboTraffic(TrapsConfig config) : base(config)
        {
        }

        private TurboTrafficConfig Settings => Config.TurboTraffic;
        public override string Id => TrapIds.TurboTraffic;
        public override string Title => "Nitro for everyone";
        public override string Subtitle => "Traffic has lost its brakes.";
        public override bool Enabled => Settings.Enabled;

        public override void Start()
        {
            _boosted.Clear();
            _vehicles.Clear();
            _until = StartTimer(Settings.Duration);
            _next = 0;
        }

        public override bool Update()
        {
            if (Game.GameTime >= _until) return false;
            if (Game.GameTime < _next) return true;
            _next = Game.GameTime + Interval;
            Ped player = Player;
            Vehicle own = player.IsInVehicle() ? player.CurrentVehicle : null;
            foreach (Vehicle vehicle in World.GetNearbyVehicles(player, Settings.Radius))
            {
                if (!vehicle.Exists() || vehicle == own) continue;
                Ped driver = vehicle.Driver;
                if (driver == null || !driver.Exists() || driver.IsPlayer || driver.IsDead) continue;
                Model model = vehicle.Model;
                if (!(model.IsCar || model.IsBike || model.IsQuadBike)) continue;
                if (_boosted.Add(vehicle.Handle))
                {
                    _vehicles.Add(vehicle);
                    Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER, driver, vehicle, Settings.Speed, RushedDriving);
                }
                if (vehicle.IsOnAllWheels && vehicle.Speed < Settings.Speed)
                    vehicle.ForwardSpeed = System.Math.Min(Settings.Speed, vehicle.Speed + Settings.Boost);
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            foreach (Vehicle vehicle in _vehicles)
            {
                if (!vehicle.Exists()) continue;
                Ped driver = vehicle.Driver;
                if (driver != null && driver.Exists() && !driver.IsDead && !driver.IsPlayer)
                    Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER, driver, vehicle, 15f, NormalDriving);
            }
            _vehicles.Clear();
            _boosted.Clear();
        }
    }
}
