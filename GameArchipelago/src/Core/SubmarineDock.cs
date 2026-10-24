using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameArchipelago.Core
{
    internal static class SubmarineDock
    {
        private static readonly Vector3[] Fallbacks =
        {
            new Vector3(-1950f, -1150f, 0f),
            new Vector3(-3150f, 450f, 0f),
            new Vector3(-1650f, 5400f, 0f),
            new Vector3(-200f, 6900f, 0f),
            new Vector3(3450f, 5600f, 0f),
            new Vector3(3200f, -350f, 0f),
            new Vector3(1250f, -3550f, 0f),
            new Vector3(1300f, 4050f, 30f),
        };

        private const float ReturnDistance = 350f;
        private const int CheckInterval = 2000;
        private static Vehicle _submarine;
        private static string _name;
        private static int _nextCheck;

        public static bool Call(string modelName, string displayName, out string message)
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead)
            {
                message = "You need to be alive.";
                return false;
            }
            if (Function.Call<bool>(Hash.GET_MISSION_FLAG))
            {
                message = "Not during a mission.";
                return false;
            }
            if (!FindWater(player.Position, out Vector3 spot, out float heading))
            {
                message = "No deep water found nearby.";
                return false;
            }
            var model = new Model(modelName);
            if (!model.IsInCdImage || !model.Request(3000))
            {
                message = displayName + " could not be loaded.";
                return false;
            }
            if (_submarine != null && _submarine.Exists())
            {
                if (player.IsInVehicle(_submarine))
                {
                    model.MarkAsNoLongerNeeded();
                    message = "You are already in it.";
                    return false;
                }
                _submarine.Delete();
            }
            _submarine = Vehicle.Create(model, spot, heading);
            model.MarkAsNoLongerNeeded();
            if (_submarine == null)
            {
                message = displayName + " could not be created.";
                return false;
            }
            _submarine.IsPersistent = true;
            _name = displayName;
            Blip blip = _submarine.AddBlip();
            blip.Sprite = model.IsSubmarine ? BlipSprite.Sub : BlipSprite.Boat;
            blip.Color = BlipColor.Yellow;
            blip.Name = displayName;
            Function.Call(Hash.SET_NEW_WAYPOINT, spot.X, spot.Y);
            message = $"{displayName} waiting {Math.Round(spot.DistanceTo2D(player.Position))} m away.";
            return true;
        }

        public static void Process(Notifier notifier)
        {
            if (_submarine == null || Game.GameTime < _nextCheck) return;
            _nextCheck = Game.GameTime + CheckInterval;
            if (!_submarine.Exists())
            {
                _submarine = null;
                return;
            }
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsInVehicle(_submarine)) return;
            if (!_submarine.IsSeatFree(VehicleSeat.Driver)) return;
            bool wrecked = _submarine.IsDead;
            if (!wrecked && _submarine.Position.DistanceTo(player.Position) < ReturnDistance) return;
            if (_submarine.AttachedBlip != null && _submarine.AttachedBlip.Exists()) _submarine.AttachedBlip.Delete();
            _submarine.Delete();
            _submarine = null;
            notifier?.Post($"~b~{Notifier.Escape(_name)}~s~ went back to port. Call it again from F6 > Boats.");
            History.Write("BOAT_HOME", "name", _name, "wrecked", wrecked);
        }

        public static bool RepairCurrent()
        {
            Ped player = Game.Player.Character;
            if (_submarine == null || !_submarine.Exists() || player == null || !player.IsInVehicle(_submarine)) return false;
            _submarine.Repair();
            return true;
        }

        public static void Release()
        {
            if (_submarine == null || !_submarine.Exists()) return;
            if (_submarine.AttachedBlip != null && _submarine.AttachedBlip.Exists()) _submarine.AttachedBlip.Delete();
            _submarine.MarkAsNoLongerNeeded();
            _submarine = null;
        }

        private static bool FindWater(Vector3 from, out Vector3 spot, out float heading)
        {
            spot = Vector3.Zero;
            heading = 0f;
            for (float radius = 25f; radius <= 600f; radius += 25f)
            {
                for (int i = 0; i < 24; i++)
                {
                    float angle = (float)(i * Math.PI * 2 / 24);
                    var point = new Vector3(from.X + (float)Math.Cos(angle) * radius, from.Y + (float)Math.Sin(angle) * radius, from.Z);
                    if (IsDeepWater(point, out float surface))
                    {
                        spot = new Vector3(point.X, point.Y, surface + 0.5f);
                        heading = (float)(angle * 180.0 / Math.PI) + 90f;
                        return true;
                    }
                }
            }
            float best = float.MaxValue;
            foreach (Vector3 candidate in Fallbacks)
            {
                if (!IsDeepWater(candidate, out float surface)) continue;
                float distance = candidate.DistanceTo2D(from);
                if (distance >= best) continue;
                best = distance;
                spot = new Vector3(candidate.X, candidate.Y, surface + 0.5f);
            }
            return best < float.MaxValue;
        }

        private static bool IsDeepWater(Vector3 point, out float surface)
        {
            surface = 0f;
            var water = new OutputArgument();
            if (!Function.Call<bool>(Hash.GET_WATER_HEIGHT_NO_WAVES, point.X, point.Y, 200f, water)) return false;
            surface = water.GetResult<float>();
            var ground = new OutputArgument();
            if (!Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, point.X, point.Y, surface, ground, false, false)) return true;
            return surface - ground.GetResult<float>() >= 6f;
        }
    }
}
