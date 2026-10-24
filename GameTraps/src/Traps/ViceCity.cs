using System.Collections.Generic;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class ViceCity : Trap
    {
        private static readonly int[][] Colors = { new[] { 255, 20, 180 }, new[] { 0, 230, 255 }, new[] { 170, 40, 255 }, new[] { 255, 120, 0 } };
        private readonly Convoy _cops = new Convoy();
        private readonly Ambience _ambience = new Ambience();
        private readonly Dictionary<int, Vehicle> _neon = new Dictionary<int, Vehicle>();
        private int _until;
        private int _nextNeon;

        public ViceCity(TrapsConfig config) : base(config)
        {
        }

        private ViceCityConfig Settings => Config.ViceCity;
        public override string Id => TrapIds.ViceCity;
        public override string Title => "Welcome to the 80s";
        public override string Subtitle => "Vice City vice squad.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Vision | TrapChannel.Weather | TrapChannel.Police;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft || PlayerInWater ? "waiting for you to be on land" : null;
            return reason == null;
        }

        public override void Start()
        {
            _neon.Clear();
            _ambience.Night(Settings.Hour);
            _ambience.Weather(Settings.Weather);
            _ambience.Timecycle(Settings.Timecycle, 0.8f);
            Function.Call(Hash.SET_AMBIENT_VEHICLE_NEON_ENABLED, true);
            _nextNeon = 0;
            var convoy = new ConvoyConfig
            {
                Cars = Settings.Cars,
                Passengers = 1,
                Accuracy = 20,
                Color = 111,
                SpawnDistance = 60f,
                Boost = 6f,
                VehicleModels = Settings.VehicleModels,
                PedModels = Settings.CopModels,
                Weapons = new List<string> { "WEAPON_PISTOL", "WEAPON_MICROSMG" },
            };
            int count = _cops.Spawn(convoy);
            foreach (Vehicle car in _cops.Vehicles)
            {
                Function.Call(Hash.SET_VEHICLE_EXTRA_COLOURS, car, 111, 156);
                Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, car, "VICE");
                Blip blip = car.AttachedBlip ?? car.AddBlip();
                blip.Color = BlipColor.Blue;
            }
            if (Settings.Stars > 0) Overrides.Stars(Settings.Stars);
            _until = StartTimer(Settings.Duration);
            Overrides.Subtitle("~p~Vice City, 1986. Two cops in a white Infernus.", 5000);
            Log.Info($"Vice City: {count} cars.");
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            Ped player = Player;
            if (now >= _until || player.IsDead) return false;
            _cops.Process();
            Glow(player, now);
            foreach (Vehicle car in _cops.Vehicles)
            {
                if (!car.Exists()) continue;
                Vector3 roof = car.Position + new Vector3(0f, 0f, 1.6f);
                bool red = (now / 250) % 2 == 0;
                Function.Call(Hash.DRAW_LIGHT_WITH_RANGE, roof.X, roof.Y, roof.Z, red ? 255 : 30, 30, red ? 30 : 255, 10f, 8f);
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            _cops.Clear(true);
            Function.Call(Hash.CLEAR_PLAYER_WANTED_LEVEL, Game.Player);
            Function.Call(Hash.SET_AMBIENT_VEHICLE_NEON_ENABLED, false);
            foreach (Vehicle car in _neon.Values)
            {
                if (!car.Exists()) continue;
                for (int side = 0; side < 4; side++) Function.Call(Hash.SET_VEHICLE_NEON_ENABLED, car, side, false);
            }
            _neon.Clear();
            _ambience.Restore();
        }

        private void Glow(Ped player, int now)
        {
            Vector3 center = player.Position;
            float pulse = 0.75f + 0.25f * (float)System.Math.Sin(now / 300.0);
            for (int i = 0; i < Settings.Lights; i++)
            {
                double angle = i * System.Math.PI * 2 / Settings.Lights + now / 4000.0;
                float distance = i % 2 == 0 ? 9f : 20f;
                int[] color = Colors[i % Colors.Length];
                var spot = new Vector3(center.X + (float)System.Math.Cos(angle) * distance, center.Y + (float)System.Math.Sin(angle) * distance, center.Z + 2.5f);
                Function.Call(Hash.DRAW_LIGHT_WITH_RANGE, spot.X, spot.Y, spot.Z, color[0], color[1], color[2], 14f, 6f * pulse);
            }
            if (now < _nextNeon) return;
            _nextNeon = now + 500;
            foreach (Vehicle car in World.GetNearbyVehicles(center, Settings.NeonRadius))
            {
                if (!car.Exists() || _neon.ContainsKey(car.Handle)) continue;
                _neon[car.Handle] = car;
                int[] color = Spawner.Pick(Colors);
                Function.Call(Hash.SET_VEHICLE_MOD_KIT, car, 0);
                Function.Call(Hash.SET_VEHICLE_NEON_COLOUR, car, color[0], color[1], color[2]);
                for (int side = 0; side < 4; side++) Function.Call(Hash.SET_VEHICLE_NEON_ENABLED, car, side, true);
            }
        }
    }
}
