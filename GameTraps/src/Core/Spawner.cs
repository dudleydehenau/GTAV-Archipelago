using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Core
{
    internal static class Spawner
    {
        public static readonly Random Random = new Random();
        private const int Hate = 5;
        private static int _hostileGroup;

        public static int HostileGroup()
        {
            int playerGroup = Function.Call<int>(Hash.GET_PED_RELATIONSHIP_GROUP_HASH, Game.Player.Character);
            if (_hostileGroup == 0)
            {
                var result = new OutputArgument();
                Function.Call(Hash.ADD_RELATIONSHIP_GROUP, "GAMETRAPS_HOSTILE", result);
                _hostileGroup = result.GetResult<int>();
            }
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, Hate, _hostileGroup, playerGroup);
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, Hate, playerGroup, _hostileGroup);
            return _hostileGroup;
        }

        public static void MakeHostile(Ped ped, bool blip) => MakeHostile(ped, blip, true);

        public static void MakeHostile(Ped ped, bool blip, bool attack)
        {
            Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, ped, HostileGroup());
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 46, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 5, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 3, true);
            Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, ped, 0, false);
            ped.BlockPermanentEvents = true;
            if (blip)
            {
                Blip marker = ped.AddBlip();
                marker.Color = BlipColor.Red;
                marker.Scale = 0.7f;
            }
            if (attack) Function.Call(Hash.TASK_COMBAT_PED, ped, Game.Player.Character, 0, 16);
        }

        public static uint WeaponHash(string name) =>
            unchecked((uint)Safety.Joaat(string.IsNullOrWhiteSpace(name) ? "WEAPON_UNARMED" : name.Trim()));

        public static void Give(Ped ped, string weapon)
        {
            if (ped == null || !ped.Exists() || string.IsNullOrWhiteSpace(weapon)) return;
            Function.Call(Hash.GIVE_WEAPON_TO_PED, ped, WeaponHash(weapon), 9999, false, true);
        }

        public static void TeleportVehicle(Vehicle vehicle, Vector3 target, float heading)
        {
            vehicle.IsPositionFrozen = true;
            vehicle.Position = target;
            vehicle.Heading = heading;
            Function.Call(Hash.NEW_LOAD_SCENE_START_SPHERE, target.X, target.Y, target.Z, 120f, 0);
            int deadline = Environment.TickCount + 10000;
            while (!Function.Call<bool>(Hash.IS_NEW_LOAD_SCENE_LOADED) && Environment.TickCount < deadline) Script.Wait(0);
            Function.Call(Hash.NEW_LOAD_SCENE_STOP);
            deadline = Environment.TickCount + 8000;
            while (!Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, vehicle) && Environment.TickCount < deadline)
            {
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, target.X, target.Y, target.Z);
                Script.Wait(0);
            }
            var node = new OutputArgument();
            var nodeHeading = new OutputArgument();
            if (Function.Call<bool>(Hash.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING, target.X, target.Y, target.Z, node, nodeHeading, 1, 3f, 0))
            {
                Vector3 found = node.GetResult<Vector3>();
                if (found.DistanceTo(target) < 300f)
                {
                    target = found;
                    heading = nodeHeading.GetResult<float>();
                }
            }
            float ground = GroundZ(target + new Vector3(0f, 0f, 3f), 0f);
            if (ground <= 0f) ground = GroundZ(target + new Vector3(0f, 0f, 100f), target.Z);
            target.Z = ground + 0.6f;
            vehicle.Position = target;
            vehicle.Heading = heading;
            vehicle.IsPositionFrozen = false;
            Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, vehicle, 5f);
            Function.Call(Hash.SET_GAMEPLAY_CAM_RELATIVE_HEADING, 0f);
        }

        public static Vector3 StreetBehind(float distance)
        {
            Ped player = Game.Player.Character;
            Entity mover = player.IsInVehicle() ? (Entity)player.CurrentVehicle : player;
            Vector3 point = mover.Position - mover.ForwardVector * distance;
            Vector3 street = World.GetNextPositionOnStreet(point);
            return street.DistanceTo(point) < distance * 1.5f ? street : point;
        }

        public static float HeadingTowards(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            return (float)(Math.Atan2(-direction.X, direction.Y) * 180.0 / Math.PI);
        }

        public static T Pick<T>(IList<T> list) => list[Random.Next(list.Count)];

        public static float Range(float min, float max) => min + (float)Random.NextDouble() * (max - min);

        public static Model LoadModel(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return default;
            var model = new Model(name.Trim());
            if (!model.IsInCdImage || !model.IsValid)
            {
                Log.Warn($"Model '{name}' does not exist.");
                return default;
            }
            if (!model.Request(3000))
            {
                Log.Warn($"Model '{name}' could not be loaded.");
                return default;
            }
            return model;
        }

        public static Model LoadAny(IList<string> names)
        {
            var order = names.OrderBy(_ => Random.Next()).ToList();
            foreach (string name in order)
            {
                Model model = LoadModel(name);
                if (model.Hash != 0) return model;
            }
            return default;
        }

        public static List<Model> LoadAll(IList<string> names)
        {
            var models = new List<Model>();
            foreach (string name in names)
            {
                Model model = LoadModel(name);
                if (model.Hash != 0) models.Add(model);
            }
            return models;
        }

        public static float GroundZ(Vector3 position, float fallback)
        {
            var result = new OutputArgument();
            if (Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, position.X, position.Y, position.Z, result, false, false))
                return result.GetResult<float>();
            return fallback;
        }

        public static Vector3 GroundAround(Vector3 center, float distance)
        {
            double angle = Random.NextDouble() * Math.PI * 2;
            var point = new Vector3(center.X + (float)Math.Cos(angle) * distance, center.Y + (float)Math.Sin(angle) * distance, center.Z + 40f);
            float z = GroundZ(point, center.Z);
            if (Math.Abs(z - center.Z) > 25f) z = center.Z;
            point.Z = z + 0.5f;
            return point;
        }

        public static bool IsAmbient(Ped ped, Ped player)
        {
            if (ped == null || !ped.Exists() || ped == player) return false;
            if (ped.IsPlayer || !ped.IsHuman || ped.IsDead) return false;
            return !Function.Call<bool>(Hash.IS_ENTITY_A_MISSION_ENTITY, ped);
        }

        public static List<Ped> AmbientPeds(float radius, int max, Func<Ped, bool> filter)
        {
            Ped player = Game.Player.Character;
            Vector3 center = player.Position;
            return World.GetNearbyPeds(player, radius)
                .Where(p => IsAmbient(p, player) && (filter == null || filter(p)))
                .OrderBy(p => p.Position.DistanceToSquared(center))
                .Take(Math.Max(0, max))
                .ToList();
        }

        public static Ped ReplacePed(Ped old, Model model)
        {
            Vector3 position = old.Position;
            float heading = old.Heading;
            old.Delete();
            Ped ped = Ped.Create(model, position, heading);
            if (ped == null) return null;
            Function.Call(Hash.SET_PED_RANDOM_COMPONENT_VARIATION, ped, 0);
            return ped;
        }

        public static void WarpOutOfVehicle(Ped ped)
        {
            if (!ped.IsInVehicle()) return;
            Vehicle vehicle = ped.CurrentVehicle;
            Vector3 side = vehicle.Position + vehicle.RightVector * -2.5f;
            ped.Position = new Vector3(side.X, side.Y, GroundZ(side + new Vector3(0f, 0f, 3f), side.Z));
        }

        public static void Teleport(Destination destination)
        {
            Ped player = Game.Player.Character;
            WarpOutOfVehicle(player);
            var target = new Vector3(destination.X, destination.Y, destination.Z);
            player.IsPositionFrozen = true;
            player.Position = target;
            player.Heading = destination.Heading;

            Function.Call(Hash.NEW_LOAD_SCENE_START_SPHERE, target.X, target.Y, target.Z, 80f, 0);
            int deadline = Environment.TickCount + 10000;
            while (!Function.Call<bool>(Hash.IS_NEW_LOAD_SCENE_LOADED) && Environment.TickCount < deadline) Script.Wait(0);
            Function.Call(Hash.NEW_LOAD_SCENE_STOP);

            deadline = Environment.TickCount + 8000;
            while (!Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, player) && Environment.TickCount < deadline)
            {
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, target.X, target.Y, target.Z);
                Script.Wait(0);
            }

            float ground = GroundZ(target + new Vector3(0f, 0f, 2f), 0f);
            if (ground <= 0f || Math.Abs(ground - destination.Z) >= 3f) ground = GroundZ(target + new Vector3(0f, 0f, 50f), 0f);
            if (ground > 0f) target.Z = ground + 1f;
            float heading = destination.Heading;
            if (destination.Clearance > 0f)
            {
                if (!FindClearSpot(target, destination.Clearance, out Vector3 clear))
                {
                    clear = FlatAround(target, destination.Clearance);
                    Log.Warn($"No walkable spot found near {destination.Name}, using a point {destination.Clearance:0} m away.");
                }
                heading = HeadingTowards(clear, target);
                target = clear + new Vector3(0f, 0f, 1f);
            }
            player.Position = target;
            player.Heading = heading;
            player.IsPositionFrozen = false;
            Log.Info(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Teleport to {0}: asked {1:0.00}, {2:0.00}, {3:0.00} (Clearance {4:0}), landed {5:0.00}, {6:0.00}, {7:0.00}.",
                destination.Name, destination.X, destination.Y, destination.Z, destination.Clearance, target.X, target.Y, target.Z));
            Function.Call(Hash.SET_GAMEPLAY_CAM_RELATIVE_HEADING, 0f);
        }

        private static bool FindClearSpot(Vector3 center, float clearance, out Vector3 spot)
        {
            for (int ring = 0; ring < 3; ring++)
            {
                float distance = clearance + ring * 4f;
                for (int i = 0; i < 8; i++)
                {
                    double angle = i * Math.PI / 4;
                    var probe = new Vector3(center.X + (float)Math.Cos(angle) * distance, center.Y + (float)Math.Sin(angle) * distance, center.Z);
                    var result = new OutputArgument();
                    if (!Function.Call<bool>(Hash.GET_SAFE_COORD_FOR_PED, probe.X, probe.Y, probe.Z, true, result, 16)) continue;
                    Vector3 safe = result.GetResult<Vector3>();
                    if (safe.DistanceTo2D(center) < clearance * 0.8f || Math.Abs(safe.Z - center.Z) > 2.5f) continue;
                    spot = safe;
                    return true;
                }
            }
            spot = center;
            return false;
        }

        private static Vector3 FlatAround(Vector3 center, float distance)
        {
            double angle = Random.NextDouble() * Math.PI * 2;
            var point = new Vector3(center.X + (float)Math.Cos(angle) * distance, center.Y + (float)Math.Sin(angle) * distance, center.Z);
            float z = GroundZ(point + new Vector3(0f, 0f, 2f), center.Z);
            point.Z = Math.Abs(z - center.Z) < 3f ? z : center.Z;
            return point;
        }

        private static readonly HashSet<string> MissingAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static void Ptfx(string asset, string effect, Vector3 position, float scale)
        {
            if (string.IsNullOrEmpty(asset) || string.IsNullOrEmpty(effect) || MissingAssets.Contains(asset)) return;
            Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, asset);
            int deadline = Environment.TickCount + 1000;
            while (!Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, asset) && Environment.TickCount < deadline) Script.Wait(0);
            if (!Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, asset))
            {
                MissingAssets.Add(asset);
                Log.Warn($"Particle asset '{asset}' could not be loaded.");
                return;
            }
            Function.Call(Hash.USE_PARTICLE_FX_ASSET, asset);
            Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD, effect, position.X, position.Y, position.Z, 0f, 0f, 0f, scale, false, false, false);
        }

        public static void Poof(Vector3 position) => Ptfx("scr_rcbarry2", "scr_clown_appears", position, 1.2f);
    }
}
