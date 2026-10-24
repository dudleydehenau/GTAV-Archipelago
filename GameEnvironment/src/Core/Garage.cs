using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal sealed class Garage
    {
        private const int MonitorInterval = 500;
        private const int LookInterval = 2000;
        private const float NearbyDistance = 30f;
        private readonly EnvironmentConfig _config;
        private readonly StateStore _store;
        private readonly Economy _economy;
        private readonly Dictionary<int, Deployment> _deployed = new Dictionary<int, Deployment>();
        private readonly Queue<int> _deliveries = new Queue<int>();
        private int _nextMonitor;
        private int _nextLook;
        private bool _helpPending;

        public Garage(EnvironmentConfig config, StateStore store, Economy economy)
        {
            _config = config;
            _store = store;
            _economy = economy;
        }

        public event Action<VehicleRecord> Destroyed;

        public IReadOnlyList<VehicleRecord> Records => _store.State.Vehicles;

        public static string DisplayName(int model)
        {
            var m = new Model(model);
            string label = Vehicle.GetModelDisplayName(m);
            return GameText.Get(label, string.IsNullOrEmpty(label) ? model.ToString() : label);
        }

        public int RepairCost(VehicleRecord record) => _config.RepairCostFor(Vehicle.GetModelClass(new Model(record.Model)));

        public void ResetDeployments()
        {
            bool changed = false;
            foreach (VehicleRecord record in _store.State.Vehicles)
            {
                if (record.Status != VehicleStatus.Delivered) continue;
                record.Status = VehicleStatus.Stored;
                changed = true;
            }
            if (changed) _store.Save();
            foreach (VehicleRecord record in _store.State.Vehicles)
            {
                if (!record.DeliveryPending) continue;
                record.DeliveryPending = false;
                changed = true;
            }
            if (changed) _store.Save();
            if (!_store.State.VehicleHelpShown && _store.State.Vehicles.Count > 0) _helpPending = true;
        }

        public VehicleRecord Grant(Model model, out string error)
        {
            error = null;
            if (!model.IsInCdImage || !model.IsVehicle)
            {
                error = $"Unknown vehicle model {model.Hash}.";
                return null;
            }
            var record = new VehicleRecord
            {
                Id = _store.State.NextVehicleId++,
                Model = model.Hash,
                Status = VehicleStatus.Stored,
                DeliveryPending = false,
            };
            _store.State.Vehicles.Add(record);
            _store.Save();
            if (record.DeliveryPending) _deliveries.Enqueue(record.Id);
            if (!_store.State.VehicleHelpShown) _helpPending = true;
            Log.Info($"Vehicle granted: #{record.Id} {DisplayName(record.Model)} ({model.Hash}).");
            return record;
        }

        public bool RequestDelivery(int id, out string error)
        {
            error = null;
            VehicleRecord record = _store.State.FindVehicle(id);
            if (record == null)
            {
                error = "Unknown vehicle.";
                return false;
            }
            if (record.Status == VehicleStatus.Destroyed)
            {
                error = "This vehicle is destroyed: repair it first.";
                return false;
            }
            if (_deployed.TryGetValue(id, out Deployment deployment) && deployment.Vehicle != null && deployment.Vehicle.Exists() && !deployment.Vehicle.IsDead)
            {
                Ped player = Game.Player.Character;
                if (player.IsInVehicle(deployment.Vehicle))
                {
                    error = "You are already in this vehicle.";
                    return false;
                }
                if (deployment.Vehicle.Position.DistanceTo(player.Position) < NearbyDistance)
                {
                    error = "This vehicle is already next to you.";
                    return false;
                }
            }
            if (!CanDeliverHere(out error)) return false;
            if (!_deliveries.Contains(id)) _deliveries.Enqueue(id);
            return true;
        }

        public bool Repair(int id, out string error)
        {
            error = null;
            VehicleRecord record = _store.State.FindVehicle(id);
            if (record == null)
            {
                error = "Unknown vehicle.";
                return false;
            }
            if (record.Status != VehicleStatus.Destroyed) return true;
            int cost = RepairCost(record);
            if (cost > 0 && !_economy.TrySpend(cost, out error)) return false;
            record.Status = VehicleStatus.Stored;
            _store.Save();
            Log.Info($"Vehicle #{record.Id} {DisplayName(record.Model)} repaired for {cost}.");
            GTA.UI.Notification.PostTicker($"{DisplayName(record.Model)} repaired.", false);
            return true;
        }

        public bool RepairCurrent(out string error)
        {
            error = null;
            Ped player = Game.Player.Character;
            Vehicle current = player != null && player.Exists() ? player.CurrentVehicle : null;
            if (current == null || !current.Exists())
            {
                error = "Get in one of your vehicles first.";
                return false;
            }
            foreach (KeyValuePair<int, Deployment> pair in _deployed)
            {
                if (pair.Value.Vehicle == null || pair.Value.Vehicle.Handle != current.Handle) continue;
                current.Repair();
                current.DirtLevel = 0f;
                Function.Call(Hash.SET_VEHICLE_ENGINE_HEALTH, current, 1000f);
                Function.Call(Hash.SET_VEHICLE_PETROL_TANK_HEALTH, current, 1000f);
                Log.Info($"Vehicle #{pair.Key} repaired on the spot.");
                return true;
            }
            error = "This is not one of your vehicles.";
            return false;
        }

        public bool Remove(int id)
        {
            VehicleRecord record = _store.State.FindVehicle(id);
            if (record == null) return false;
            Despawn(id, true);
            _store.State.Vehicles.Remove(record);
            _store.Save();
            Log.Info($"Vehicle #{id} removed.");
            return true;
        }

        public void Tick()
        {
            if (_helpPending && !Function.Call<bool>(Hash.IS_HELP_MESSAGE_ON_SCREEN))
            {
                _helpPending = false;
                _store.State.VehicleHelpShown = true;
                _store.Save();
                ShowHelp();
            }
            if (_deliveries.Count > 0)
            {
                int id = _deliveries.Dequeue();
                VehicleRecord record = _store.State.FindVehicle(id);
                if (record != null && record.Status != VehicleStatus.Destroyed)
                {
                    if (!Spawn(record, out string error))
                        GTA.UI.Notification.PostTicker("~y~" + error, false);
                }
            }
            if (Game.GameTime >= _nextLook)
            {
                _nextLook = Game.GameTime + LookInterval;
                SaveLooks();
            }
            if (Game.GameTime < _nextMonitor) return;
            _nextMonitor = Game.GameTime + MonitorInterval;
            Monitor();
        }

        private void SaveLooks()
        {
            bool changed = false;
            foreach (KeyValuePair<int, Deployment> pair in _deployed)
            {
                VehicleRecord record = _store.State.FindVehicle(pair.Key);
                if (record != null && CaptureLook(record, pair.Value)) changed = true;
            }
            if (changed) _store.Save();
        }

        private static bool CaptureLook(VehicleRecord record, Deployment deployment)
        {
            Vehicle vehicle = deployment.Vehicle;
            if (vehicle == null || !vehicle.Exists() || vehicle.IsDead) return false;
            VehicleLook look = VehicleLook.Capture(vehicle);
            string signature = look.Signature();
            if (signature == deployment.LookSignature) return false;
            deployment.LookSignature = signature;
            record.Look = look;
            Log.Debug($"Vehicle #{record.Id} look saved.");
            return true;
        }

        public void DespawnAll()
        {
            foreach (int id in _deployed.Keys.ToList()) Despawn(id, true);
        }

        private static bool CanDeliverHere(out string error)
        {
            error = null;
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead)
            {
                error = "You need to be alive.";
                return false;
            }
            if (Function.Call<bool>(Hash.IS_ENTITY_IN_WATER, player) || player.IsSwimming)
            {
                error = "Not in the water: get back on land first.";
                return false;
            }
            if (player.IsInVehicle() && (player.CurrentVehicle.Model.IsHelicopter || player.CurrentVehicle.Model.IsPlane || player.CurrentVehicle.Model.IsBoat))
            {
                error = "Land on a road first.";
                return false;
            }
            if (Function.Call<bool>(Hash.IS_PED_FALLING, player) || player.HeightAboveGround > 4f)
            {
                error = "Be on the ground first.";
                return false;
            }
            return true;
        }

        private static bool FindDeliverySpot(Ped player, out Vector3 spot, out float heading)
        {
            spot = Vector3.Zero;
            heading = player.Heading;
            Vector3 from = player.Position;
            for (int n = 1; n <= 12; n++)
            {
                var node = new OutputArgument();
                var nodeHeading = new OutputArgument();
                var lanes = new OutputArgument();
                if (!Function.Call<bool>(Hash.GET_NTH_CLOSEST_VEHICLE_NODE_WITH_HEADING, from.X, from.Y, from.Z, n, node, nodeHeading, lanes, 0, 3f, 0f)) continue;
                Vector3 candidate = node.GetResult<Vector3>();
                float distance = candidate.DistanceTo(from);
                if (distance < 6f || distance > 80f) continue;
                if (Math.Abs(candidate.Z - from.Z) > 12f) continue;
                var water = new OutputArgument();
                if (Function.Call<bool>(Hash.GET_WATER_HEIGHT_NO_WAVES, candidate.X, candidate.Y, candidate.Z + 2f, water) && water.GetResult<float>() > candidate.Z - 0.5f) continue;
                if (Function.Call<bool>(Hash.IS_POINT_OBSCURED_BY_A_MISSION_ENTITY, candidate.X, candidate.Y, candidate.Z, 3f, 3f, 3f, 0)) continue;
                if (Function.Call<bool>(Hash.IS_ANY_VEHICLE_NEAR_POINT, candidate.X, candidate.Y, candidate.Z, 3f)) continue;
                spot = candidate + new Vector3(0f, 0f, 0.6f);
                heading = nodeHeading.GetResult<float>();
                return true;
            }
            return false;
        }

        private bool Spawn(VehicleRecord record, out string error)
        {
            error = null;
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead)
            {
                error = "The player must be alive to receive a vehicle.";
                return false;
            }
            var model = new Model(record.Model);
            if (!model.Request(10000))
            {
                error = $"Could not load {DisplayName(record.Model)}.";
                return false;
            }
            if (!FindDeliverySpot(player, out Vector3 position, out float heading))
            {
                model.MarkAsNoLongerNeeded();
                error = "No safe road nearby for the delivery: go to a street and ask again.";
                return false;
            }
            Despawn(record.Id, true);
            Vehicle vehicle = Vehicle.Create(model, position, heading);
            model.MarkAsNoLongerNeeded();
            if (vehicle == null || !vehicle.Exists())
            {
                error = $"Could not create {DisplayName(record.Model)}.";
                return false;
            }
            if (record.Look != null)
            {
                try
                {
                    record.Look.Apply(vehicle);
                }
                catch (Exception ex)
                {
                    Log.Error($"Could not restore the look of vehicle #{record.Id}.", ex);
                }
            }
            vehicle.PlaceOnGround();
            WheelFix.Queue(vehicle);
            vehicle.IsPersistent = true;
            Function.Call(Hash.SET_VEHICLE_HAS_BEEN_OWNED_BY_PLAYER, vehicle, true);
            Function.Call(Hash.SET_VEHICLE_IS_STOLEN, vehicle, false);
            Blip blip = vehicle.AddBlip();
            blip.Sprite = vehicle.Model.IsBike || vehicle.Model.IsBicycle ? BlipSprite.PersonalVehicleBike : BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Blue;
            blip.Name = DisplayName(record.Model);
            var deployment = new Deployment(vehicle, blip);
            _deployed[record.Id] = deployment;
            CaptureLook(record, deployment);
            record.Status = VehicleStatus.Delivered;
            record.DeliveryPending = false;
            _store.Save();
            Log.Info($"Vehicle #{record.Id} {DisplayName(record.Model)} delivered.");
            GTA.UI.Notification.PostTicker($"{DisplayName(record.Model)} delivered next to you.", false);
            return true;
        }

        private void Monitor()
        {
            if (_deployed.Count == 0) return;
            Ped player = Game.Player.Character;
            bool changed = false;
            foreach (KeyValuePair<int, Deployment> pair in _deployed.ToList())
            {
                VehicleRecord record = _store.State.FindVehicle(pair.Key);
                Deployment deployment = pair.Value;
                Vehicle vehicle = deployment.Vehicle;
                if (record == null)
                {
                    Despawn(pair.Key, true);
                    continue;
                }
                if (vehicle == null || !vehicle.Exists())
                {
                    Despawn(pair.Key, false);
                    record.Status = VehicleStatus.Stored;
                    changed = true;
                    continue;
                }
                bool inside = player != null && player.Exists() && player.IsInVehicle(vehicle);
                if (vehicle.IsDead || !vehicle.IsDriveable)
                {
                    deployment.RemoveBlip();
                    vehicle.IsPersistent = false;
                    vehicle.MarkAsNoLongerNeeded();
                    _deployed.Remove(pair.Key);
                    record.Status = VehicleStatus.Destroyed;
                    changed = true;
                    Log.Info($"Vehicle #{record.Id} {DisplayName(record.Model)} destroyed.");
                    GTA.UI.Notification.PostTicker($"~r~{DisplayName(record.Model)} destroyed.~s~ Repair it from the menu ({GameText.Money(RepairCost(record))}).", false);
                    Destroyed?.Invoke(record);
                    continue;
                }
                if (!inside && player != null && vehicle.Position.DistanceTo(player.Position) > _config.VehicleStoreDistance)
                {
                    Despawn(pair.Key, true);
                    record.Status = VehicleStatus.Stored;
                    changed = true;
                    Log.Info($"Vehicle #{record.Id} {DisplayName(record.Model)} left behind, stored.");
                    GTA.UI.Notification.PostTicker($"{DisplayName(record.Model)} was left behind and stored. Order it again from the menu.", false);
                    continue;
                }
                deployment.SetBlipVisible(!inside);
            }
            if (changed) _store.Save();
        }

        private void Despawn(int id, bool deleteVehicle)
        {
            if (!_deployed.TryGetValue(id, out Deployment deployment)) return;
            VehicleRecord record = _store.State.FindVehicle(id);
            if (record != null && CaptureLook(record, deployment)) _store.Save();
            _deployed.Remove(id);
            deployment.RemoveBlip();
            Vehicle vehicle = deployment.Vehicle;
            if (vehicle == null || !vehicle.Exists()) return;
            Ped player = Game.Player.Character;
            if (deleteVehicle && (player == null || !player.IsInVehicle(vehicle)))
            {
                vehicle.IsPersistent = false;
                vehicle.Delete();
            }
            else
            {
                vehicle.IsPersistent = false;
                vehicle.MarkAsNoLongerNeeded();
            }
        }

        private void ShowHelp()
        {
            string key = _config.EnableMenu ? $" ({_config.MenuKey})" : string.Empty;
            GTA.UI.Screen.ShowHelpText($"Your vehicles are delivered next to you. Leave one behind and it is stored automatically: order it again any time from the menu{key}, Vehicles. A destroyed vehicle can be repaired there for a fee.",
                15000, true, false);
        }

        private sealed class Deployment
        {
            private bool _blipVisible = true;

            public Deployment(Vehicle vehicle, Blip blip)
            {
                Vehicle = vehicle;
                Blip = blip;
            }

            public Vehicle Vehicle { get; }
            public Blip Blip { get; private set; }
            public string LookSignature { get; set; }

            public void SetBlipVisible(bool visible)
            {
                if (Blip == null || !Blip.Exists() || _blipVisible == visible) return;
                _blipVisible = visible;
                Blip.Alpha = visible ? 255 : 0;
            }

            public void RemoveBlip()
            {
                if (Blip != null && Blip.Exists()) Blip.Delete();
                Blip = null;
            }
        }
    }
}
