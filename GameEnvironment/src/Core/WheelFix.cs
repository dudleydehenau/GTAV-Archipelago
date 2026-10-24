using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal static class WheelFix
    {
        private const int Interval = 500;
        private const int Patience = 4000;
        private const int FrontWheels = 23;
        private const int RearWheels = 24;
        private static readonly List<Pending> Waiting = new List<Pending>();
        private static int _next;
        private static int _lastVehicle;

        private sealed class Pending
        {
            public int Handle;
            public int Since;
            public int NotBefore;
        }

        public static void Queue(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return;
            Add(vehicle.Handle, 0);
            Add(vehicle.Handle, 3000);
        }

        private static void Add(int handle, int delay)
        {
            Waiting.Add(new Pending { Handle = handle, Since = Game.GameTime, NotBefore = Game.GameTime + delay });
        }

        public static void Tick()
        {
            if (Game.GameTime < _next) return;
            _next = Game.GameTime + Interval;
            if (Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE)) return;
            Ped ped = Game.Player.Character;
            Vehicle current = ped != null && ped.Exists() ? ped.CurrentVehicle : null;
            int handle = current != null && current.Exists() ? current.Handle : 0;
            if (handle != _lastVehicle)
            {
                _lastVehicle = handle;
                if (handle != 0) Queue(current);
            }
            if (Waiting.Count == 0) return;
            for (int i = Waiting.Count - 1; i >= 0; i--)
            {
                Pending entry = Waiting[i];
                if (Game.GameTime < entry.NotBefore) continue;
                var vehicle = Entity.FromHandle(entry.Handle) as Vehicle;
                if (vehicle == null || !vehicle.Exists())
                {
                    Waiting.RemoveAt(i);
                    continue;
                }
                bool streamed = Function.Call<bool>(Hash.HAVE_VEHICLE_MODS_STREAMED_IN, vehicle);
                if (!streamed && Game.GameTime - entry.Since < Patience) continue;
                Refresh(vehicle);
                Waiting.RemoveAt(i);
            }
            if (Waiting.Count > 64) Waiting.RemoveRange(0, Waiting.Count - 64);
        }

        private static void Refresh(Vehicle vehicle)
        {
            Function.Call(Hash.SET_VEHICLE_MOD_KIT, vehicle, 0);
            bool bike = vehicle.Model.IsBike;
            int wheelType = Function.Call<int>(Hash.GET_VEHICLE_WHEEL_TYPE, vehicle);
            bool validType = bike ? wheelType == 6 : wheelType >= 0 && wheelType <= 12 && wheelType != 6;
            if (!validType)
            {
                Function.Call(Hash.SET_VEHICLE_WHEEL_TYPE, vehicle, bike ? 6 : 0);
                Function.Call(Hash.REMOVE_VEHICLE_MOD, vehicle, FrontWheels);
                if (bike) Function.Call(Hash.REMOVE_VEHICLE_MOD, vehicle, RearWheels);
                Log.Info($"Invalid wheel type {wheelType} on {vehicle.DisplayName}: stock wheels put back.");
            }
            foreach (int type in bike ? new[] { FrontWheels, RearWheels } : new[] { FrontWheels })
            {
                if (Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS, vehicle, type) <= 0) continue;
                int index = Function.Call<int>(Hash.GET_VEHICLE_MOD, vehicle, type);
                bool custom = Function.Call<bool>(Hash.GET_VEHICLE_MOD_VARIATION, vehicle, type);
                if (index >= Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS, vehicle, type)) index = -1;
                if (index >= 0)
                {
                    Function.Call(Hash.SET_VEHICLE_MOD, vehicle, type, index, custom);
                }
                else
                {
                    Function.Call(Hash.SET_VEHICLE_MOD, vehicle, type, 0, false);
                    Function.Call(Hash.REMOVE_VEHICLE_MOD, vehicle, type);
                }
            }
            Log.Debug($"Wheels refreshed on {vehicle.DisplayName}.");
        }
    }
}
