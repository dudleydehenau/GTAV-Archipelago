using System.Collections.Generic;
using GameTraps.Core;
using GTA;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class Blizzard : Trap
    {
        private const Hash SetForceVehicleTrails = (Hash)0x4CC7F0FEA5283FE0UL;
        private const Hash SetForcePedFootstepsTracks = (Hash)0xAEEDAD1420C65CC0UL;
        private const Hash ForceGroundSnowPass = (Hash)0x6E9EF3A33C8899F8UL;
        private const Hash SetSnowLevel = (Hash)0x7F06937B0CDCBC1AUL;
        private const Hash SetParticleFxOverride = (Hash)0xEA1E2D93F6F75ED9UL;
        private const Hash ResetParticleFxOverride = (Hash)0x89C8553DD3274AAEUL;
        private const string SnowAsset = "core_snow";
        private const int GripInterval = 1000;
        private readonly List<Vehicle> _slippery = new List<Vehicle>();
        private readonly HashSet<int> _handles = new HashSet<int>();
        private Weather _previous;
        private int _until;
        private int _nextGrip;

        public Blizzard(TrapsConfig config) : base(config)
        {
        }

        private BlizzardConfig Settings => Config.Blizzard;
        public override string Id => TrapIds.Blizzard;
        public override string Title => "North Yankton weather";
        public override string Subtitle => "Snow on Los Santos. Roads are icy.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Weather;

        public override void Start()
        {
            _slippery.Clear();
            _handles.Clear();
            _previous = World.Weather;
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, Settings.Weather);
            Function.Call(Hash.SET_OVERRIDE_WEATHER, Settings.Weather);
            Snow(true);
            _until = StartTimer(Settings.Duration);
            _nextGrip = 0;
        }

        public override bool Update()
        {
            if (Game.GameTime >= _until) return false;
            if (!Settings.IcyRoads || Game.GameTime < _nextGrip) return true;
            _nextGrip = Game.GameTime + GripInterval;
            foreach (Vehicle vehicle in World.GetNearbyVehicles(Player.Position, Settings.IcyRadius))
            {
                if (!vehicle.Exists() || !_handles.Add(vehicle.Handle)) continue;
                Model model = vehicle.Model;
                if (!(model.IsCar || model.IsBike || model.IsQuadBike || model.IsBicycle)) continue;
                Function.Call(Hash.SET_VEHICLE_REDUCE_GRIP, vehicle, true);
                Function.Call(Hash.SET_VEHICLE_REDUCE_GRIP_LEVEL, vehicle, Settings.GripLevel);
                _slippery.Add(vehicle);
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            foreach (Vehicle vehicle in _slippery)
                if (vehicle.Exists()) Function.Call(Hash.SET_VEHICLE_REDUCE_GRIP, vehicle, false);
            _slippery.Clear();
            _handles.Clear();
            Function.Call(Hash.CLEAR_OVERRIDE_WEATHER);
            Function.Call(Hash.CLEAR_WEATHER_TYPE_PERSIST);
            Snow(false);
            World.Weather = _previous;
        }

        private void Snow(bool on)
        {
            if (Settings.GroundSnow)
            {
                if (on)
                {
                    Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, SnowAsset);
                    int deadline = System.Environment.TickCount + 2000;
                    while (!Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, SnowAsset) && System.Environment.TickCount < deadline) Script.Wait(0);
                    Function.Call(Hash.USE_PARTICLE_FX_ASSET, SnowAsset);
                    Function.Call(SetParticleFxOverride, SnowAsset, SnowAsset);
                    Function.Call(ForceGroundSnowPass, true);
                    Function.Call(SetSnowLevel, 1f);
                }
                else
                {
                    Function.Call(ForceGroundSnowPass, false);
                    Function.Call(SetSnowLevel, -1f);
                    Function.Call(ResetParticleFxOverride, SnowAsset);
                    if (Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, SnowAsset)) Function.Call(Hash.REMOVE_NAMED_PTFX_ASSET, SnowAsset);
                }
            }
            if (!Settings.SnowTracks) return;
            Function.Call(SetForceVehicleTrails, on);
            Function.Call(SetForcePedFootstepsTracks, on);
            Function.Call(Hash.USE_SNOW_FOOT_VFX_WHEN_UNSHELTERED, on);
            Function.Call(Hash.USE_SNOW_WHEEL_VFX_WHEN_UNSHELTERED, on);
        }
    }
}
