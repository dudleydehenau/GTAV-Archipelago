using System.Collections.Generic;
using System.Linq;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class ScooterBrother : Trap
    {
        private const int NormalDriving = 786603;
        private const int OwnedWreckTimeout = 5000;
        private readonly List<Vehicle> _ownedWrecks = new List<Vehicle>();
        private int _wreckDeadline;

        public ScooterBrother(TrapsConfig config) : base(config)
        {
        }

        private ScooterBrotherConfig Settings => Config.ScooterBrother;
        public override string Id => TrapIds.ScooterBrother;
        public override string Title => "Scooter Brothers!";
        public override string Subtitle => "Everyone rides a Faggio now.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Phone;

        public override void Start()
        {
            _ownedWrecks.Clear();
            TrevorLine.Play(Settings.Line);
            List<Model> models = Spawner.LoadAll(Settings.Models);
            if (models.Count == 0) return;
            var scooterHashes = new HashSet<int>(models.Select(m => m.Hash));
            Ped player = Player;
            Vehicle own = player.IsInVehicle() ? player.CurrentVehicle : Game.Player.LastVehicle;
            if (own != null && (!own.Exists() || own.Position.DistanceTo(player.Position) > Settings.Radius)) own = null;

            List<Vehicle> vehicles = World.GetNearbyVehicles(player, Settings.Radius)
                .Where(v => v.Exists() && IsLandVehicle(v) && !scooterHashes.Contains(v.Model.Hash))
                .Where(v => v == own || !Function.Call<bool>(Hash.IS_ENTITY_A_MISSION_ENTITY, v))
                .OrderBy(v => v == own ? 0f : v.Position.DistanceToSquared(player.Position))
                .Take(Settings.MaxVehicles)
                .ToList();
            if (own != null && !vehicles.Contains(own) && IsLandVehicle(own) && !scooterHashes.Contains(own.Model.Hash)) vehicles.Insert(0, own);

            int converted = 0;
            foreach (Vehicle vehicle in vehicles)
                if (Convert(vehicle, Spawner.Pick(models), vehicle == own)) converted++;
            _wreckDeadline = Game.GameTime + OwnedWreckTimeout;
            Log.Info($"Scooter Brothers: {converted} vehicles turned into scooters.");
        }

        public override bool Update()
        {
            TrevorLine.Tick();
            for (int i = _ownedWrecks.Count - 1; i >= 0; i--)
            {
                Vehicle wreck = _ownedWrecks[i];
                bool released = !wreck.Exists() || !Function.Call<bool>(Hash.IS_ENTITY_A_MISSION_ENTITY, wreck);
                if (!released && Game.GameTime < _wreckDeadline) continue;
                if (wreck.Exists()) wreck.Delete();
                _ownedWrecks.RemoveAt(i);
            }
            return _ownedWrecks.Count > 0 || TrevorLine.CallActive;
        }

        public override void Stop(bool interrupted)
        {
            TrevorLine.HangUp();
            foreach (Vehicle wreck in _ownedWrecks)
                if (wreck.Exists()) wreck.Delete();
            _ownedWrecks.Clear();
        }

        private static bool IsLandVehicle(Vehicle vehicle)
        {
            Model model = vehicle.Model;
            return model.IsCar || model.IsBike || model.IsQuadBike || model.IsBicycle;
        }

        private bool Convert(Vehicle vehicle, Model model, bool isPlayers)
        {
            Ped player = Player;
            Vector3 position = vehicle.Position;
            float heading = vehicle.Heading;
            float speed = vehicle.Speed;
            Ped driver = vehicle.Driver;
            Ped passenger = vehicle.GetPedOnSeat(VehicleSeat.Passenger);
            List<Ped> others = vehicle.Occupants.Where(p => p != null && p.Exists() && p != driver && p != passenger).ToList();
            bool owned = Function.Call<bool>(Hash.IS_ENTITY_A_MISSION_ENTITY, vehicle);

            foreach (Ped occupant in vehicle.Occupants)
                if (occupant != null && occupant.Exists()) occupant.Position = position + new Vector3(0f, 0f, 3f);

            if (isPlayers && owned)
            {
                vehicle.IsVisible = false;
                vehicle.IsCollisionEnabled = false;
                vehicle.Position = position + new Vector3(0f, 0f, -20f);
                vehicle.IsPositionFrozen = true;
                Function.Call(Hash.SET_VEHICLE_UNDRIVEABLE, vehicle, true);
                vehicle.EngineHealth = -4000f;
                _ownedWrecks.Add(vehicle);
                Log.Info("The player's own vehicle was turned into a scooter (destroyed).");
            }
            else vehicle.Delete();

            Vehicle scooter = Vehicle.Create(model, position, heading);
            if (scooter == null) return false;
            scooter.PlaceOnGround();
            if (driver != null && driver.Exists()) Function.Call(Hash.SET_PED_INTO_VEHICLE, driver, scooter, -1);
            if (passenger != null && passenger.Exists()) Function.Call(Hash.SET_PED_INTO_VEHICLE, passenger, scooter, 0);
            foreach (Ped other in others) other.Position = position + scooter.RightVector * 2f;
            scooter.IsEngineRunning = true;
            scooter.ForwardSpeed = speed;

            if (driver != null && driver.Exists() && driver != player)
                Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER, driver, scooter, 20f, NormalDriving);
            if (!isPlayers && driver != player) scooter.MarkAsNoLongerNeeded();
            Spawner.Poof(position + new Vector3(0f, 0f, 0.5f));
            return true;
        }
    }
}
