using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Core
{
    internal sealed class Squad
    {
        private const int RespawnDelay = 1500;
        private const int OrderInterval = 3000;
        private readonly EntityTracker _entities = new EntityTracker();
        private readonly List<Ped> _members = new List<Ped>();
        private List<Model> _models = new List<Model>();
        private List<string> _weapons = new List<string>();
        private int _nextSpawn;
        private int _nextOrder;

        public int Accuracy { get; set; } = 20;
        public float MoveRate { get; set; } = 1f;
        public bool Invincible { get; set; }
        public bool Blips { get; set; } = true;
        public bool Poofs { get; set; } = true;
        public Action<Ped> Spawned { get; set; }
        public Action<Ped> Died { get; set; }
        public IReadOnlyList<Ped> Members => _members;
        public int Alive => _members.Count(m => m.Exists() && !m.IsDead);

        public bool Load(IList<string> models, IList<string> weapons)
        {
            _members.Clear();
            _models = Spawner.LoadAll(models ?? new List<string>());
            _weapons = weapons == null ? new List<string>() : weapons.ToList();
            _nextSpawn = 0;
            _nextOrder = 0;
            return _models.Count > 0;
        }

        public Ped SpawnAround(float distance)
        {
            Ped player = Game.Player.Character;
            Vector3 position = Spawner.GroundAround(player.Position, Spawner.Range(distance * 0.6f, distance));
            return Spawn(position);
        }

        public Ped Spawn(Vector3 position)
        {
            if (_models.Count == 0) return null;
            Ped player = Game.Player.Character;
            Ped ped = Ped.Create(Spawner.Pick(_models), position, Spawner.HeadingTowards(position, player.Position));
            if (ped == null) return null;
            Function.Call(Hash.SET_PED_RANDOM_COMPONENT_VARIATION, ped, 0);
            Adopt(ped);
            if (Poofs) Spawner.Poof(position + new Vector3(0f, 0f, 0.5f));
            return ped;
        }

        public void Adopt(Ped ped)
        {
            if (ped == null) return;
            _entities.Add(ped);
            _members.Add(ped);
            if (_weapons.Count > 0) Spawner.Give(ped, Spawner.Pick(_weapons));
            Function.Call(Hash.SET_PED_ACCURACY, ped, Accuracy);
            Function.Call(Hash.SET_PED_CAN_SWITCH_WEAPON, ped, true);
            if (Invincible)
            {
                ped.IsInvincible = true;
                ped.CanRagdoll = false;
            }
            Spawner.MakeHostile(ped, Blips);
            Spawned?.Invoke(ped);
        }

        public void Process(int target, float distance, bool respawn)
        {
            int now = Game.GameTime;
            Ped player = Game.Player.Character;
            foreach (Ped dead in _members.Where(m => m.Exists() && m.IsDead).ToList())
            {
                dead.AttachedBlip?.Delete();
                _members.Remove(dead);
                Died?.Invoke(dead);
                if (_nextSpawn == 0) _nextSpawn = now + RespawnDelay;
            }
            _members.RemoveAll(m => !m.Exists());
            if (MoveRate != 1f)
                foreach (Ped member in _members)
                    Function.Call(Hash.SET_PED_MOVE_RATE_OVERRIDE, member, MoveRate);
            if (respawn && _members.Count < target && _nextSpawn != 0 && now >= _nextSpawn)
            {
                SpawnAround(distance);
                _nextSpawn = _members.Count < target ? now + RespawnDelay : 0;
            }
            if (now < _nextOrder) return;
            _nextOrder = now + OrderInterval;
            foreach (Ped member in _members)
                if (!member.IsDead && !Function.Call<bool>(Hash.IS_PED_IN_COMBAT, member, player))
                    Function.Call(Hash.TASK_COMBAT_PED, member, player, 0, 16);
        }

        public void Clear(bool delete)
        {
            foreach (Ped member in _members)
                if (member.Exists() && !member.IsDead && Poofs) Spawner.Poof(member.Position);
            _members.Clear();
            if (delete) _entities.DeleteAll();
            else _entities.ReleaseAll();
        }
    }

    internal sealed class Convoy
    {
        private const int OrderInterval = 4000;
        private readonly EntityTracker _entities = new EntityTracker();
        private readonly List<Vehicle> _vehicles = new List<Vehicle>();
        private readonly List<Ped> _drivers = new List<Ped>();
        private readonly List<Ped> _riders = new List<Ped>();
        private int _nextOrder;
        private float _boost;

        private readonly Dictionary<int, int> _stuckSince = new Dictionary<int, int>();

        public float DirtLevel { get; set; } = -1f;
        public bool Invincible { get; set; }
        public float RamSpeed { get; set; }
        public int MaxVehicles { get; set; } = 30;
        public IReadOnlyList<Vehicle> Vehicles => _vehicles;

        public int Spawn(ConvoyConfig settings)
        {
            _boost = settings.Boost;
            int before = _vehicles.Count;
            List<Model> cars = Spawner.LoadAll(settings.VehicleModels ?? new List<string>());
            List<Model> people = Spawner.LoadAll(settings.PedModels ?? new List<string>());
            if (cars.Count == 0 || people.Count == 0) return 0;
            List<string> weapons = settings.Weapons ?? new List<string>();
            Ped player = Game.Player.Character;
            for (int i = 0; i < settings.Cars && _vehicles.Count < MaxVehicles; i++)
            {
                Vector3 position = Spawner.StreetBehind(settings.SpawnDistance + i * 14f);
                Vehicle vehicle = Vehicle.Create(Spawner.Pick(cars), position, Spawner.HeadingTowards(position, player.Position));
                if (vehicle == null) continue;
                _entities.Add(vehicle);
                _vehicles.Add(vehicle);
                if (settings.Color >= 0) Function.Call(Hash.SET_VEHICLE_COLOURS, vehicle, settings.Color, settings.Color);
                if (DirtLevel >= 0f) Function.Call(Hash.SET_VEHICLE_DIRT_LEVEL, vehicle, DirtLevel);
                if (Invincible) vehicle.IsInvincible = true;
                Function.Call(Hash.SET_VEHICLE_ENGINE_ON, vehicle, true, true, false);
                if (RamSpeed > 0f) Armour(vehicle);

                Ped driver = vehicle.CreatePedOnSeat(VehicleSeat.Driver, Spawner.Pick(people));
                if (driver != null)
                {
                    _entities.Add(driver);
                    _drivers.Add(driver);
                    Spawner.MakeHostile(driver, true, false);
                    Function.Call(Hash.SET_PED_ACCURACY, driver, settings.Accuracy);
                    if (weapons.Count > 0) Spawner.Give(driver, Spawner.Pick(weapons));
                    if (RamSpeed > 0f)
                    {
                        driver.IsInvincible = true;
                        Function.Call(Hash.SET_DRIVER_ABILITY, driver, 1f);
                        Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, driver, 1f);
                        Function.Call(Hash.SET_PED_KEEP_TASK, driver, true);
                        Ram(driver);
                        vehicle.ForwardSpeed = Math.Max(15f, RamSpeed * 0.5f);
                    }
                    else Chase(driver);
                }
                int seats = Function.Call<int>(Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS, vehicle);
                for (int seat = 0; seat < Math.Min(seats, settings.Passengers); seat++)
                {
                    Ped shooter = vehicle.CreatePedOnSeat((VehicleSeat)seat, Spawner.Pick(people));
                    if (shooter == null) continue;
                    _entities.Add(shooter);
                    _riders.Add(shooter);
                    if (weapons.Count > 0) Spawner.Give(shooter, Spawner.Pick(weapons));
                    Function.Call(Hash.SET_PED_ACCURACY, shooter, settings.Accuracy);
                    Spawner.MakeHostile(shooter, false);
                }
            }
            _nextOrder = Game.GameTime + OrderInterval;
            return _vehicles.Count - before;
        }

        public int ActiveVehicles => _vehicles.Count(v => v.Exists() && !v.IsDead && HasCrew(v));

        private bool HasCrew(Vehicle vehicle)
        {
            foreach (Ped ped in _drivers.Concat(_riders))
                if (ped.Exists() && !ped.IsDead && ped.IsInVehicle(vehicle)) return true;
            return false;
        }

        private void Tidy()
        {
            foreach (Ped ped in _drivers.Concat(_riders))
                if (ped.Exists() && ped.IsDead) ped.AttachedBlip?.Delete();
            foreach (Vehicle vehicle in _vehicles.ToList())
            {
                if (vehicle.Exists() && !vehicle.IsDead && HasCrew(vehicle)) continue;
                if (vehicle.Exists())
                {
                    vehicle.AttachedBlip?.Delete();
                    vehicle.IsInvincible = false;
                }
                _vehicles.Remove(vehicle);
            }
        }

        public void Process()
        {
            if (RamSpeed > 0f)
            {
                ProcessRam();
                return;
            }
            Ped player = Game.Player.Character;
            Entity target = player.IsInVehicle() ? (Entity)player.CurrentVehicle : player;
            float targetSpeed = target.Velocity.Length();
            foreach (Vehicle vehicle in _vehicles)
            {
                if (!vehicle.Exists() || vehicle.IsDead || !vehicle.IsOnAllWheels) continue;
                Ped driver = vehicle.Driver;
                if (driver == null || !driver.Exists() || driver.IsPlayer || driver.IsDead) continue;
                if (vehicle.Position.DistanceTo(target.Position) < 25f) continue;
                float wanted = targetSpeed + 6f;
                if (vehicle.Speed < wanted) vehicle.ForwardSpeed = Math.Min(wanted, vehicle.Speed + _boost * Game.LastFrameTime * 10f);
            }
            if (Game.GameTime < _nextOrder) return;
            _nextOrder = Game.GameTime + OrderInterval;
            Tidy();
            foreach (Ped driver in _drivers)
                if (driver.Exists() && !driver.IsDead && driver.IsInVehicle()) Chase(driver);
        }

        private void ProcessRam()
        {
            Ped player = Game.Player.Character;
            Entity target = player.IsInVehicle() ? (Entity)player.CurrentVehicle : player;
            Vector3 targetPosition = target.Position + target.Velocity * 0.6f;
            float targetSpeed = target.Velocity.Length();
            int now = Game.GameTime;
            foreach (Vehicle vehicle in _vehicles)
            {
                if (!vehicle.Exists() || vehicle.IsDead) continue;
                Ped driver = vehicle.Driver;
                if (driver == null || !driver.Exists() || driver.IsPlayer || driver.IsDead) continue;
                Vector3 toTarget = targetPosition - vehicle.Position;
                float distance = toTarget.Length();
                if (vehicle.Speed < 3f && distance > 20f)
                {
                    if (!_stuckSince.TryGetValue(vehicle.Handle, out int since)) _stuckSince[vehicle.Handle] = now;
                    else if (now - since > 2500) Reposition(vehicle, driver);
                }
                else _stuckSince.Remove(vehicle.Handle);
                if (distance > 260f)
                {
                    Reposition(vehicle, driver);
                    continue;
                }
                if (!vehicle.IsOnAllWheels) continue;
                Vector3 flat = new Vector3(toTarget.X, toTarget.Y, 0f);
                if (flat.Length() < 0.1f) continue;
                float facing = Vector3.Dot(flat.Normalized, vehicle.ForwardVector);
                float wanted = Math.Min(RamSpeed, Math.Max(targetSpeed + 18f, 26f));
                if (facing > 0.35f && vehicle.Speed < wanted) vehicle.ForwardSpeed = Math.Min(wanted, vehicle.Speed + _boost * Game.LastFrameTime * 10f);
            }
            if (now < _nextOrder) return;
            _nextOrder = now + 2500;
            Tidy();
            foreach (Ped driver in _drivers)
                if (driver.Exists() && !driver.IsDead && driver.IsInVehicle()) Ram(driver);
        }

        private void Reposition(Vehicle vehicle, Ped driver)
        {
            Ped player = Game.Player.Character;
            Vector3 position = Spawner.StreetBehind(55f + Spawner.Random.Next(40));
            Function.Call(Hash.SET_ENTITY_COORDS, vehicle, position.X, position.Y, position.Z, false, false, false, true);
            vehicle.Heading = Spawner.HeadingTowards(position, player.Position);
            vehicle.ForwardSpeed = Math.Max(18f, RamSpeed * 0.6f);
            _stuckSince.Remove(vehicle.Handle);
            Ram(driver);
        }

        private static void Armour(Vehicle vehicle)
        {
            Function.Call(Hash.SET_VEHICLE_STRONG, vehicle, true);
            Function.Call(Hash.SET_VEHICLE_CAN_BE_VISIBLY_DAMAGED, vehicle, false);
            Function.Call(Hash.SET_VEHICLE_TYRES_CAN_BURST, vehicle, false);
            Function.Call(Hash.SET_ENTITY_PROOFS, vehicle, true, true, true, true, true, true, true, true);
            Function.Call(Hash.SET_VEHICLE_CHEAT_POWER_INCREASE, vehicle, 4f);
            Function.Call(Hash.MODIFY_VEHICLE_TOP_SPEED, vehicle, 60f);
        }

        private void Ram(Ped driver)
        {
            Ped player = Game.Player.Character;
            Vehicle vehicle = driver.CurrentVehicle;
            if (vehicle == null) return;
            Function.Call(Hash.TASK_VEHICLE_MISSION_PED_TARGET, driver, vehicle, player, 2, RamSpeed, 1074528293, 0f, 0f, true);
        }

        public void Clear(bool delete)
        {
            _stuckSince.Clear();
            _vehicles.Clear();
            _drivers.Clear();
            _riders.Clear();
            if (delete) _entities.DeleteAll();
            else _entities.ReleaseAll();
        }

        private static void Chase(Ped driver)
        {
            Ped player = Game.Player.Character;
            Function.Call(Hash.TASK_VEHICLE_CHASE, driver, player);
            Function.Call(Hash.SET_TASK_VEHICLE_CHASE_IDEAL_PURSUIT_DISTANCE, driver, 0f);
            Function.Call(Hash.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG, driver, 32, true);
        }
    }

    internal sealed class SkyRain
    {
        private readonly EntityTracker _entities = new EntityTracker();
        private readonly Queue<Entity> _alive = new Queue<Entity>();
        private List<Model> _props = new List<Model>();
        private List<Model> _peds = new List<Model>();
        private int _count;
        private int _interval;
        private int _dropped;
        private int _next;
        private float _spread;
        private float _minHeight;
        private float _maxHeight;

        public int Dropped => _dropped;
        public int MaxAlive { get; set; } = 40;

        public bool Begin(IList<string> props, IList<string> peds, int count, int interval, float spread, float minHeight, float maxHeight)
        {
            _props = Spawner.LoadAll(props ?? new List<string>());
            _peds = Spawner.LoadAll(peds ?? new List<string>());
            _count = count;
            _interval = Math.Max(50, interval);
            _spread = spread;
            _minHeight = minHeight;
            _maxHeight = Math.Max(minHeight, maxHeight);
            _dropped = 0;
            _next = 0;
            return _props.Count + _peds.Count > 0;
        }

        public bool Process(Vector3 center)
        {
            if (_dropped >= _count || _props.Count + _peds.Count == 0) return false;
            if (Game.GameTime < _next) return true;
            _next = Game.GameTime + _interval;
            _dropped++;
            var drop = center + new Vector3(Spawner.Range(-_spread, _spread), Spawner.Range(-_spread, _spread), Spawner.Range(_minHeight, _maxHeight));
            bool usePed = _peds.Count > 0 && (_props.Count == 0 || Spawner.Random.Next(4) == 0);
            Entity entity;
            if (usePed)
            {
                entity = Ped.Create(Spawner.Pick(_peds), drop, Spawner.Range(0f, 360f));
            }
            else
            {
                Prop prop = Prop.Create(Spawner.Pick(_props), drop, true, false);
                if (prop != null)
                {
                    prop.IsPositionFrozen = false;
                    Function.Call(Hash.SET_ENTITY_DYNAMIC, prop, true);
                    Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, prop, true);
                    Function.Call(Hash.ACTIVATE_PHYSICS, prop);
                    prop.Rotation = new Vector3(Spawner.Range(0f, 360f), Spawner.Range(0f, 360f), Spawner.Range(0f, 360f));
                }
                entity = prop;
            }
            if (entity == null) return true;
            _entities.Add(entity);
            _alive.Enqueue(entity);
            while (_alive.Count > MaxAlive)
            {
                Entity old = _alive.Dequeue();
                if (old != null && old.Exists()) old.Delete();
            }
            entity.Velocity = new Vector3(0f, 0f, -8f);
            return true;
        }

        public void Clear(bool delete)
        {
            _alive.Clear();
            if (delete) _entities.DeleteAll();
            else _entities.Forget();
        }
    }

    internal sealed class ForcedRide
    {
        private const int ExitControl = 75;
        private const int RushedDriving = 1074528293;
        private const int CrazyDriving = 787004;
        private const int NormalDriving = 786603;
        private readonly EntityTracker _entities = new EntityTracker();
        private Vehicle _vehicle;
        private Ped _driver;
        private int _seat;
        private int _nextBoost;
        private RideConfig _settings;

        public Vehicle Vehicle => _vehicle;
        public Ped Driver => _driver;

        public bool Begin(RideConfig settings)
        {
            _settings = settings;
            Model car = Spawner.LoadModel(settings.VehicleModel);
            Model person = Spawner.LoadModel(settings.DriverModel);
            if (car.Hash == 0 || person.Hash == 0) return false;
            Ped player = Game.Player.Character;
            Spawner.WarpOutOfVehicle(player);
            Vector3 spot = World.GetNextPositionOnStreet(player.Position + player.RightVector * 4f);
            if (spot.DistanceTo(player.Position) > 40f) spot = player.Position + player.RightVector * 4f;
            _vehicle = Vehicle.Create(car, spot, player.Heading);
            if (_vehicle == null) return false;
            _entities.Add(_vehicle);
            _vehicle.IsInvincible = true;
            Function.Call(Hash.SET_VEHICLE_ENGINE_ON, _vehicle, true, true, false);
            _driver = _vehicle.CreatePedOnSeat(VehicleSeat.Driver, person);
            if (_driver == null)
            {
                End(true);
                return false;
            }
            _entities.Add(_driver);
            _driver.IsInvincible = true;
            _driver.BlockPermanentEvents = true;
            Function.Call(Hash.SET_DRIVER_ABILITY, _driver, 1f);
            Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, _driver, settings.Crazy ? 1f : 0.3f);
            int passengers = Function.Call<int>(Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS, _vehicle);
            _seat = passengers >= 2 ? 1 : 0;
            Function.Call(Hash.SET_PED_INTO_VEHICLE, player, _vehicle, _seat);
            Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, _vehicle, 4);
            Wander();
            _nextBoost = 0;
            return true;
        }

        public void DriveTo(Vector3 destination)
        {
            if (_driver == null || !_driver.Exists() || _vehicle == null || !_vehicle.Exists()) return;
            Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE, _driver, _vehicle, destination.X, destination.Y, destination.Z,
                _settings.Speed, _settings.Crazy ? CrazyDriving : RushedDriving, 15f);
        }

        public void Wander()
        {
            if (_driver == null || !_driver.Exists() || _vehicle == null || !_vehicle.Exists()) return;
            Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER, _driver, _vehicle, _settings.Speed, _settings.Crazy ? CrazyDriving : RushedDriving);
        }

        public void Process()
        {
            if (_vehicle == null || !_vehicle.Exists()) return;
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, ExitControl, true);
            Ped player = Game.Player.Character;
            if (!player.IsInVehicle(_vehicle) && !player.IsDead && !player.IsRagdoll)
                Function.Call(Hash.SET_PED_INTO_VEHICLE, player, _vehicle, _seat);
            if (!_settings.Crazy || Game.GameTime < _nextBoost) return;
            _nextBoost = Game.GameTime + 250;
            if (_vehicle.IsOnAllWheels && _vehicle.Speed < _settings.Speed) _vehicle.ForwardSpeed = Math.Min(_settings.Speed, _vehicle.Speed + 1.5f);
        }

        public void End(bool delete)
        {
            Ped player = Game.Player.Character;
            if (_vehicle != null && _vehicle.Exists())
            {
                Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, _vehicle, 1);
                _vehicle.IsInvincible = false;
                if (player.IsInVehicle(_vehicle))
                {
                    if (delete || _vehicle.Speed > 3f) Spawner.WarpOutOfVehicle(player);
                    else Function.Call(Hash.TASK_LEAVE_VEHICLE, player, _vehicle, 0);
                }
            }
            if (_driver != null && _driver.Exists() && _vehicle != null && _vehicle.Exists() && !delete)
                Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER, _driver, _vehicle, 15f, NormalDriving);
            if (_driver != null && _driver.Exists()) _driver.IsInvincible = false;
            _vehicle = null;
            _driver = null;
            if (delete) _entities.DeleteAll();
            else _entities.Forget();
        }
    }

    internal sealed class ScriptCam
    {
        private int _cam = -1;

        public bool Active => _cam != -1;

        public void Begin()
        {
            End();
            Vector3 position = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_COORD);
            Vector3 rotation = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 2);
            float fov = Function.Call<float>(Hash.GET_GAMEPLAY_CAM_FOV);
            _cam = Function.Call<int>(Hash.CREATE_CAM_WITH_PARAMS, "DEFAULT_SCRIPTED_CAMERA", position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z, fov, true, 2);
            Function.Call(Hash.RENDER_SCRIPT_CAMS, true, false, 0, true, false, 0);
        }

        public void Set(Vector3 position, Vector3 rotation, float fov)
        {
            if (_cam == -1) return;
            Function.Call(Hash.SET_CAM_COORD, _cam, position.X, position.Y, position.Z);
            Function.Call(Hash.SET_CAM_ROT, _cam, rotation.X, rotation.Y, rotation.Z, 2);
            if (fov > 0f) Function.Call(Hash.SET_CAM_FOV, _cam, fov);
        }

        public void End()
        {
            if (_cam == -1) return;
            Function.Call(Hash.RENDER_SCRIPT_CAMS, false, false, 0, true, false, 0);
            Function.Call(Hash.SET_CAM_ACTIVE, _cam, false);
            Function.Call(Hash.DESTROY_CAM, _cam, false);
            _cam = -1;
        }
    }

    internal sealed class Ambience
    {
        private Weather _weather;
        private int _hour;
        private int _minute;
        private bool _saved;
        private bool _weatherChanged;
        private bool _clockChanged;
        private bool _timecycle;
        private bool _blackout;

        public void Blackout()
        {
            Save();
            _blackout = true;
            Function.Call(Hash.SET_ARTIFICIAL_LIGHTS_STATE, true);
        }

        public void Save()
        {
            if (_saved) return;
            _weather = World.Weather;
            _hour = Function.Call<int>(Hash.GET_CLOCK_HOURS);
            _minute = Function.Call<int>(Hash.GET_CLOCK_MINUTES);
            _saved = true;
            _weatherChanged = false;
            _clockChanged = false;
            _timecycle = false;
            _blackout = false;
        }

        public void Weather(string weather)
        {
            if (string.IsNullOrEmpty(weather)) return;
            Save();
            _weatherChanged = true;
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, weather);
            Function.Call(Hash.SET_OVERRIDE_WEATHER, weather);
        }

        public void Night(int hour)
        {
            Save();
            _clockChanged = true;
            Overrides.Hour(hour);
            Function.Call(Hash.PAUSE_CLOCK, true);
        }

        public void Timecycle(string name, float strength)
        {
            if (string.IsNullOrEmpty(name)) return;
            Save();
            _timecycle = true;
            Overrides.Timecycle(name, strength);
        }

        public void Restore()
        {
            if (!_saved) return;
            _saved = false;
            if (_timecycle) Overrides.ClearTimecycle();
            if (_blackout)
            {
                Function.Call(Hash.SET_ARTIFICIAL_LIGHTS_STATE, false);
            }
            if (_weatherChanged)
            {
                Function.Call(Hash.CLEAR_OVERRIDE_WEATHER);
                Function.Call(Hash.CLEAR_WEATHER_TYPE_PERSIST);
                World.Weather = _weather;
            }
            if (_clockChanged)
            {
                Function.Call(Hash.PAUSE_CLOCK, false);
                Function.Call(Hash.SET_CLOCK_TIME, _hour, _minute, 0);
            }
        }
    }

    internal static class Overrides
    {
        public static void Weather(string weather)
        {
            if (string.IsNullOrEmpty(weather)) return;
            Function.Call(Hash.SET_WEATHER_TYPE_OVERTIME_PERSIST, weather, 5f);
        }

        public static void ClearWeather()
        {
            Function.Call(Hash.CLEAR_WEATHER_TYPE_PERSIST);
            Function.Call(Hash.CLEAR_OVERRIDE_WEATHER);
        }

        public static void Timecycle(string name, float strength)
        {
            if (string.IsNullOrEmpty(name)) return;
            Function.Call(Hash.SET_TIMECYCLE_MODIFIER, name);
            Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, strength);
        }

        public static void ClearTimecycle() => Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);

        public static void Stars(int level)
        {
            int value = System.Math.Max(0, System.Math.Min(5, level));
            if (value <= Game.Player.Wanted.WantedLevel) return;
            Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, Game.Player, value, false);
            Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, false);
        }

        public static void Hour(int hour)
        {
            int value = ((hour % 24) + 24) % 24;
            Function.Call(Hash.SET_CLOCK_TIME, value, 0, 0);
        }

        public static void Subtitle(string text, int ms) => GTA.UI.Screen.ShowSubtitle(text, ms);

        public static void Banner(string text, System.Drawing.Color color, float y, float scale)
        {
            var element = new GTA.UI.TextElement(text, new System.Drawing.PointF(640f, y), scale, color, GTA.UI.Font.Pricedown, GTA.UI.Alignment.Center, true, true);
            element.Draw();
        }

        public static void Beep(string sound, string set) =>
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, sound, set, true);
    }
}
