using GTA;
using GTA.Math;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal static class FallGuard
    {
        private const int Window = 25000;
        private const int FallDelay = 1200;
        private static int _until = -1;
        private static int _fallingSince = -1;
        private static Vector3 _lastSafe;
        private static bool _hasSafe;

        public static void Arm(string reason)
        {
            if (_until < 0) Log.Debug($"Fall guard armed ({reason}).");
            _until = Game.GameTime + Window;
            _fallingSince = -1;
        }

        public static void Tick()
        {
            if (_until < 0) return;
            if (Game.GameTime > _until)
            {
                _until = -1;
                return;
            }
            Ped ped = Game.Player.Character;
            if (ped == null || !ped.Exists() || ped.IsDead || ped.IsInVehicle() || ped.IsInWater || ped.IsInParachuteFreeFall
                || Function.Call<int>(Hash.GET_PED_PARACHUTE_STATE, ped) >= 0)
            {
                _fallingSince = -1;
                return;
            }
            Vector3 position = ped.Position;
            if (!ped.IsFalling && !ped.IsInAir)
            {
                _fallingSince = -1;
                _lastSafe = position;
                _hasSafe = true;
                return;
            }
            if (_fallingSince < 0) _fallingSince = Game.GameTime;
            if (Game.GameTime - _fallingSince < FallDelay) return;
            Function.Call(Hash.REQUEST_COLLISION_AT_COORD, position.X, position.Y, position.Z);
            var ground = new OutputArgument();
            bool found = Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, position.X, position.Y, 1000f, ground, false, false);
            float groundZ = found ? ground.GetResult<float>() : 0f;
            Vector3 target;
            if (found && position.Z < groundZ - 3f) target = new Vector3(position.X, position.Y, groundZ + 1f);
            else if (!found && _hasSafe && position.Z < _lastSafe.Z - 25f) target = _lastSafe + new Vector3(0f, 0f, 1f);
            else return;
            ped.Velocity = Vector3.Zero;
            ped.PositionNoOffset = target;
            _fallingSince = -1;
            Log.Info($"Player was falling under the map at {position}, put back at {target}.");
        }
    }
}
