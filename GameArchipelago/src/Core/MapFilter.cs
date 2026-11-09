using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Native;
using GameEnvironment;

namespace GameArchipelago.Core
{
    internal sealed class MapGroup
    {
        public MapGroup(string id, string name, string[] activities, params BlipSprite[] sprites)
        {
            Id = id;
            Name = name;
            Activities = activities;
            Sprites = sprites;
        }

        public string[] Activities { get; }

        public string Id { get; }
        public string Name { get; }
        public BlipSprite[] Sprites { get; }
    }

    internal sealed class MapFilter
    {
        private const int Interval = 1000;
        private const float SpotRadius = 60f;
        private static readonly KeyValuePair<string, GTA.Math.Vector3>[] Spots =
        {
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_141", new GTA.Math.Vector3(-1230.6f, -2050f, 12.9f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_142", new GTA.Math.Vector3(2434.4f, 4284.3f, 35.5f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_143", new GTA.Math.Vector3(1591.7f, 3813.4f, 33.3f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_112", new GTA.Math.Vector3(-1939.5f, 4444.0f, 37.3f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_113", new GTA.Math.Vector3(-516.9f, 2008.0f, 204.1f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_114", new GTA.Math.Vector3(2996.8f, 2774.1f, 43.3f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_115", new GTA.Math.Vector3(-223.7f, 4224.6f, 43.7f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_116", new GTA.Math.Vector3(1606.6f, 3841.2f, 33.3f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_117", new GTA.Math.Vector3(2037.7f, 2137.4f, 92.7f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_132", new GTA.Math.Vector3(3063.1f, 639.9f, 0.0f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_133", new GTA.Math.Vector3(3447.7f, 5193.0f, 0.0f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_134", new GTA.Math.Vector3(198.1f, 3620.4f, 27.3f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_135", new GTA.Math.Vector3(621.7f, -2136.8f, 0.0f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_136", new GTA.Math.Vector3(369.3f, 312.4f, 103.2f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_137", new GTA.Math.Vector3(-813.1f, -2546.7f, 12.8f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_138", new GTA.Math.Vector3(791.6f, -1160.9f, 27.9f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_139", new GTA.Math.Vector3(-1063.0f, -1153.4f, 1.1f)),
            new KeyValuePair<string, GTA.Math.Vector3>("Activity_140", new GTA.Math.Vector3(-155.8f, -1567.0f, 34.0f)),
        };

        public static readonly MapGroup[] Groups =
        {
            new MapGroup("Golf", "Golf", new[] { "Activity_127" }, BlipSprite.Golf, BlipSprite.GolfFlag),
            new MapGroup("Tennis", "Tennis", new[] { "Activity_126" }, BlipSprite.Tennis),
            new MapGroup("Darts", "Darts", new[] { "Activity_130" }, BlipSprite.Dart),
            new MapGroup("Yoga", "Yoga", new[] { "Activity_300" }, BlipSprite.Yoga),
            new MapGroup("Range", "Shooting ranges", new[] { "Activity_119", "Activity_120", "Activity_121", "Activity_122", "Activity_123", "Activity_124" }, BlipSprite.ShootingRange, BlipSprite.AmmuNationShootingRange),
            new MapGroup("Flight", "Flight School and air races", new[] { "Activity_181", "Activity_182", "Activity_183", "Activity_184", "Activity_185", "Activity_186", "Activity_187", "Activity_188", "Activity_189", "Activity_190", "Activity_191", "Activity_192", "Activity_125" }, BlipSprite.Airport, BlipSprite.RaceAir),
            new MapGroup("Races", "Street and off-road races", new[] { "Activity_136", "Activity_137", "Activity_138", "Activity_139", "Activity_140", "Activity_112", "Activity_113", "Activity_114", "Activity_115", "Activity_116", "Activity_117" }, BlipSprite.RaceCar, BlipSprite.RaceBike, BlipSprite.OffRoadRaceFinish, BlipSprite.Race, BlipSprite.RaceFinish, BlipSprite.RaceFoot),
            new MapGroup("SeaRaces", "Sea races", new[] { "Activity_132", "Activity_133", "Activity_134", "Activity_135" }, BlipSprite.RaceSea),
            new MapGroup("Triathlon", "Triathlons", new[] { "Activity_141", "Activity_142", "Activity_143" }, BlipSprite.Triathlon),
            new MapGroup("Parachute", "Parachuting", new[] { "Activity_128", "Activity_129" }, BlipSprite.BaseJump, BlipSprite.Parachute),
            new MapGroup("Hunting", "Hunting", new[] { "Activity_180" }, BlipSprite.Hunting),
            new MapGroup("Cinema", "Movie theaters", new[] { "Activity_288" }, BlipSprite.MovieTheater),
            new MapGroup("CableCar", "Cable car", new[] { "Activity_301" }, BlipSprite.Lift),
            new MapGroup("CarWash", "Car washes", new[] { "Activity_302" }, BlipSprite.CarWash),
            new MapGroup("Barber", "Barbers", new[] { "Activity_306" }, BlipSprite.Barber),
            new MapGroup("Clothes", "Clothes shops", new[] { "Activity_304" }, BlipSprite.Clothes),
            new MapGroup("Tattoo", "Tattoo parlors", new[] { "Activity_307" }, BlipSprite.TattooParlor),
            new MapGroup("Customs", "Los Santos Customs", new[] { "Activity_305" }, BlipSprite.LosSantosCustoms),
            new MapGroup("Stores", "Stores", new[] { "Activity_297" }, BlipSprite.Store),
            new MapGroup("AmmuNation", "Ammu-Nation", new string[0], BlipSprite.AmmuNation),
        };

        private readonly ClientConfig _config;
        private readonly HashSet<int> _hidden = new HashSet<int>();
        private int _next;

        public MapFilter(ClientConfig config)
        {
            _config = config;
        }

        public bool IsHidden(MapGroup group) => _config.HiddenMapGroups.Contains(group.Id);

        public void SetHidden(MapGroup group, bool hidden)
        {
            bool has = IsHidden(group);
            if (has == hidden) return;
            if (hidden) _config.HiddenMapGroups.Add(group.Id);
            else _config.HiddenMapGroups.Remove(group.Id);
            _config.Save();
            _next = 0;
        }

        public bool ShowDone
        {
            get => _config.ShowDoneIcons;
            set
            {
                if (_config.ShowDoneIcons == value) return;
                _config.ShowDoneIcons = value;
                _config.Save();
                _next = 0;
            }
        }

        public bool IsDone(MapGroup group)
        {
            ArchipelagoScript core = ArchipelagoScript.Instance;
            SlotSave save = core?.Save;
            if (save == null || group.Activities.Length == 0) return false;
            int inWorld = 0;
            foreach (string id in group.Activities)
            {
                FeatLocation feat = ApData.FeatById(id);
                if (feat == null || !core.IsFeatInWorld(feat)) continue;
                inWorld++;
                if (!save.IsDone(feat.Location)) return false;
            }
            return inWorld > 0;
        }

        private static List<GTA.Math.Vector3> DoneSpots(MapGroup group)
        {
            var spots = new List<GTA.Math.Vector3>();
            SlotSave save = ArchipelagoScript.Instance?.Save;
            if (save == null) return spots;
            foreach (KeyValuePair<string, GTA.Math.Vector3> spot in Spots)
            {
                if (System.Array.IndexOf(group.Activities, spot.Key) < 0) continue;
                FeatLocation feat = ApData.FeatById(spot.Key);
                if (feat != null && save.IsDone(feat.Location)) spots.Add(spot.Value);
            }
            return spots;
        }

        private static bool NearDoneSpot(int blip, List<GTA.Math.Vector3> spots)
        {
            if (spots.Count == 0) return false;
            GTA.Math.Vector3 at = Function.Call<GTA.Math.Vector3>(Hash.GET_BLIP_INFO_ID_COORD, blip);
            foreach (GTA.Math.Vector3 spot in spots)
                if (new GTA.Math.Vector2(at.X - spot.X, at.Y - spot.Y).Length() < SpotRadius) return true;
            return false;
        }

        public void Tick()
        {
            if (Game.GameTime < _next) return;
            _next = Game.GameTime + Interval;
            var hiddenNow = new HashSet<int>();
            HashSet<int> finished = EnvironmentApi.FinishedActivityIcons();
            foreach (MapGroup group in Groups)
            {
                List<GTA.Math.Vector3> doneSpots = DoneSpots(group);
                bool groupDone = IsDone(group);
                bool off = IsHidden(group);
                foreach (BlipSprite sprite in group.Sprites)
                {
                    int blip = Function.Call<int>(Hash.GET_FIRST_BLIP_INFO_ID, (int)sprite);
                    int guard = 0;
                    while (blip != 0 && Function.Call<bool>(Hash.DOES_BLIP_EXIST, blip) && guard++ < 200)
                    {
                        bool done = groupDone || finished.Contains(blip) || NearDoneSpot(blip, doneSpots);
                        bool hide = off || (done && !_config.ShowDoneIcons);
                        if (hide)
                        {
                            if (Function.Call<int>(Hash.GET_BLIP_ALPHA, blip) != 0) Function.Call(Hash.SET_BLIP_ALPHA, blip, 0);
                            hiddenNow.Add(blip);
                        }
                        else if (_hidden.Contains(blip)) Function.Call(Hash.SET_BLIP_ALPHA, blip, 255);
                        if (!hide && done) Function.Call(Hash.SHOW_TICK_ON_BLIP, blip, true);
                        blip = Function.Call<int>(Hash.GET_NEXT_BLIP_INFO_ID, (int)sprite);
                    }
                }
            }
            _hidden.Clear();
            _hidden.UnionWith(hiddenNow);
        }
    }
}
