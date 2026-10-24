using System.Collections.Generic;
using System.Linq;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class Clones : Trap
    {
        private readonly EntityTracker _entities = new EntityTracker();
        private readonly List<Ped> _clones = new List<Ped>();
        private readonly List<Ped> _officers = new List<Ped>();
        private int _cloneGroup;
        private int _until;
        private int _nextOrder;

        public Clones(TrapsConfig config) : base(config)
        {
        }

        private ClonesConfig Settings => Config.Clones;
        public override string Id => TrapIds.Clones;
        public override string Title => "Attack of the clones";
        public override string Subtitle => "Which one is the real one?";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Police;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft || PlayerInWater ? "waiting for you to be on land" : null;
            return reason == null;
        }

        public override void Start()
        {
            _clones.Clear();
            _officers.Clear();
            Ped player = Player;
            var result = new OutputArgument();
            Function.Call(Hash.ADD_RELATIONSHIP_GROUP, "GAMETRAPS_CLONES", result);
            _cloneGroup = result.GetResult<int>();
            int cops = Safety.Joaat("COP");
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, cops, _cloneGroup);
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, _cloneGroup, cops);

            for (int i = 0; i < Settings.Count; i++)
            {
                int handle = Function.Call<int>(Hash.CLONE_PED, player, false, false, true);
                if (handle == 0) continue;
                var clone = Entity.FromHandle(handle) as Ped;
                if (clone == null) continue;
                _entities.Add(clone);
                _clones.Add(clone);
                Vector3 spot = Spawner.GroundAround(player.Position, Spawner.Range(1.5f, 4f));
                clone.Position = spot;
                clone.Heading = Spawner.Range(0f, 360f);
                Function.Call(Hash.REMOVE_ALL_PED_WEAPONS, clone, true);
                if (!string.IsNullOrEmpty(Settings.Weapon)) Spawner.Give(clone, Settings.Weapon);
                Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, clone, _cloneGroup);
                clone.BlockPermanentEvents = true;
                Spawner.Poof(spot + new Vector3(0f, 0f, 0.5f));
            }
            if (Settings.Stars > 0) Overrides.Stars(Settings.Stars);
            SpawnPolice(cops);
            Scatter();
            Hunt();
            _until = StartTimer(Settings.Duration);
            _nextOrder = Game.GameTime + 6000;
            Log.Info($"Clones: {_clones.Count} clones.");
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            if (now >= _until || Player.IsDead) return false;
            if (now < _nextOrder) return true;
            _nextOrder = now + 4000;
            Scatter();
            Hunt();
            return true;
        }

        public override void Stop(bool interrupted)
        {
            foreach (Ped clone in _clones)
                if (clone.Exists() && !clone.IsDead) Spawner.Poof(clone.Position);
            _clones.Clear();
            _officers.Clear();
            _entities.DeleteAll();
        }

        private void SpawnPolice(int cops)
        {
            Model car = Spawner.LoadAny(Settings.PoliceCars);
            Model cop = Spawner.LoadModel(Settings.CopModel);
            if (car.Hash == 0 || cop.Hash == 0) return;
            Ped player = Player;
            for (int i = 0; i < Settings.PoliceUnits; i++)
            {
                double angle = Spawner.Random.NextDouble() * System.Math.PI * 2;
                float distance = Spawner.Range(25f, 45f);
                Vector3 around = player.Position + new Vector3((float)System.Math.Cos(angle) * distance, (float)System.Math.Sin(angle) * distance, 0f);
                Vector3 street = World.GetNextPositionOnStreet(around);
                if (street.DistanceTo(around) > 40f) street = Spawner.GroundAround(player.Position, distance);
                Vehicle unit = Vehicle.Create(car, street, Spawner.HeadingTowards(street, player.Position));
                if (unit == null) continue;
                _entities.Add(unit);
                Function.Call(Hash.SET_VEHICLE_SIREN, unit, true);
                Function.Call(Hash.SET_VEHICLE_ENGINE_ON, unit, true, true, false);
                for (int seat = -1; seat <= 0; seat++)
                {
                    Ped officer = unit.CreatePedOnSeat((VehicleSeat)seat, cop);
                    if (officer == null) continue;
                    _entities.Add(officer);
                    _officers.Add(officer);
                    Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, officer, cops);
                    Spawner.Give(officer, "WEAPON_PISTOL");
                    Function.Call(Hash.SET_PED_AS_COP, officer, true);
                    Function.Call(Hash.TASK_COMBAT_HATED_TARGETS_AROUND_PED, officer, 150f, 0);
                }
            }
        }

        private void Hunt()
        {
            List<Ped> alive = _clones.Where(c => c.Exists() && !c.IsDead).ToList();
            if (alive.Count == 0) return;
            for (int i = 0; i < _officers.Count; i++)
            {
                Ped officer = _officers[i];
                if (!officer.Exists() || officer.IsDead) continue;
                if (i % 3 == 2) continue;
                Ped target = alive.OrderBy(c => c.Position.DistanceToSquared(officer.Position)).Skip(i % 2).FirstOrDefault() ?? alive[0];
                Function.Call(Hash.TASK_COMBAT_PED, officer, target, 0, 16);
            }
        }

        private void Scatter()
        {
            Ped player = Player;
            for (int i = 0; i < _clones.Count; i++)
            {
                Ped clone = _clones[i];
                if (!clone.Exists() || clone.IsDead) continue;
                if (clone.Position.DistanceTo(player.Position) > 120f)
                {
                    Vector3 near = Spawner.GroundAround(player.Position, Spawner.Range(10f, 25f));
                    clone.Position = near;
                }
                if (clone.IsInVehicle()) continue;
                switch (i % 3)
                {
                    case 1:
                        Vehicle ride = World.GetNearbyVehicles(clone.Position, 35f).FirstOrDefault(v => v.Exists() && !v.IsDead && Function.Call<bool>(Hash.IS_VEHICLE_SEAT_FREE, v, -1, false) && v != (player.IsInVehicle() ? player.CurrentVehicle : null));
                        if (ride != null)
                        {
                            Function.Call(Hash.TASK_ENTER_VEHICLE, clone, ride, 8000, -1, 2f, 1, 0);
                            continue;
                        }
                        break;
                    case 2:
                        Spawner.Give(clone, "WEAPON_PISTOL");
                        Vector3 sky = clone.Position + new Vector3(0f, 0f, 30f);
                        Function.Call(Hash.TASK_SHOOT_AT_COORD, clone, sky.X, sky.Y, sky.Z, 2500, unchecked((int)0xC6EE6B4C));
                        continue;
                }
                Vector3 goal = clone.Position + new Vector3(Spawner.Range(-80f, 80f), Spawner.Range(-80f, 80f), 0f);
                Function.Call(Hash.TASK_GO_STRAIGHT_TO_COORD, clone, goal.X, goal.Y, goal.Z, 3f, -1, 0f, 0f);
            }
            foreach (Ped clone in _clones)
            {
                if (!clone.Exists() || clone.IsDead || !clone.IsInVehicle() || clone.CurrentVehicle.Driver != clone) continue;
                Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER, clone, clone.CurrentVehicle, 35f, 1074528293);
            }
        }
    }
}
