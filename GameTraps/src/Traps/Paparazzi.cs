using System.Collections.Generic;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class Paparazzi : Trap
    {
        private const string PhotoScenario = "WORLD_HUMAN_PAPARAZZI";
        private const float PhotoDistance = 5f;
        private const int Interval = 1000;
        private readonly EntityTracker _entities = new EntityTracker();
        private readonly List<Ped> _walkers = new List<Ped>();
        private readonly List<Ped> _drivers = new List<Ped>();
        private int _until;
        private int _next;

        public Paparazzi(TrapsConfig config) : base(config)
        {
        }

        private PaparazziConfig Settings => Config.Paparazzi;
        public override string Id => TrapIds.Paparazzi;
        public override string Title => "Paparazzi";
        public override string Subtitle => "Smile for the cameras!";
        public override bool Enabled => Settings.Enabled;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft ? "not while flying" : null;
            return reason == null;
        }

        public override void Start()
        {
            _walkers.Clear();
            _drivers.Clear();
            Model model = Spawner.LoadModel(Settings.Model);
            if (model.Hash == 0) return;
            Ped player = Player;

            for (int i = 0; i < Settings.Count; i++)
            {
                Vector3 position = Spawner.GroundAround(player.Position, Spawner.Range(Settings.SpawnDistance * 0.6f, Settings.SpawnDistance));
                Ped photographer = Ped.Create(model, position, Spawner.HeadingTowards(position, player.Position));
                if (photographer == null) continue;
                _entities.Add(photographer);
                _walkers.Add(photographer);
                photographer.BlockPermanentEvents = true;
            }

            Model car = Spawner.LoadModel(Settings.CarModel);
            if (car.Hash != 0 && player.IsInVehicle())
            {
                for (int i = 0; i < Settings.Cars; i++)
                {
                    Vector3 position = Spawner.StreetBehind(Settings.SpawnDistance + i * 12f);
                    Vehicle vehicle = Vehicle.Create(car, position, Spawner.HeadingTowards(position, player.Position));
                    if (vehicle == null) continue;
                    _entities.Add(vehicle);
                    Ped driver = vehicle.CreatePedOnSeat(VehicleSeat.Driver, model);
                    if (driver == null) continue;
                    _entities.Add(driver);
                    _drivers.Add(driver);
                    driver.BlockPermanentEvents = true;
                    Function.Call(Hash.TASK_VEHICLE_CHASE, driver, player);
                }
            }
            _until = StartTimer(Settings.Duration);
            _next = 0;
            Log.Info($"Paparazzi: {_walkers.Count} on foot, {_drivers.Count} in cars.");
        }

        public override bool Update()
        {
            if (Game.GameTime >= _until) return false;
            if (Game.GameTime < _next) return true;
            _next = Game.GameTime + Interval;
            Ped player = Player;
            foreach (Ped photographer in _walkers)
            {
                if (!photographer.Exists() || photographer.IsDead) continue;
                float distance = photographer.Position.DistanceTo(player.Position);
                bool shooting = Function.Call<bool>(Hash.IS_PED_USING_SCENARIO, photographer, PhotoScenario);
                if (distance <= PhotoDistance)
                {
                    if (shooting) continue;
                    Function.Call(Hash.CLEAR_PED_TASKS, photographer);
                    photographer.Heading = Spawner.HeadingTowards(photographer.Position, player.Position);
                    Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, photographer, PhotoScenario, 0, true);
                }
                else
                {
                    if (shooting) Function.Call(Hash.CLEAR_PED_TASKS, photographer);
                    Function.Call(Hash.TASK_GO_TO_ENTITY, photographer, player, -1, PhotoDistance * 0.6f, 3f, 0f, 0);
                }
            }
            foreach (Ped driver in _drivers)
                if (driver.Exists() && !driver.IsDead && !Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, driver, 167)) Function.Call(Hash.TASK_VEHICLE_CHASE, driver, player);
            return true;
        }

        public override void Stop(bool interrupted)
        {
            _walkers.Clear();
            _drivers.Clear();
            if (interrupted) _entities.DeleteAll();
            else _entities.ReleaseAll();
        }
    }
}
