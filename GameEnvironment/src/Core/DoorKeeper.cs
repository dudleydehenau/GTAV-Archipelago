using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal sealed class DoorKeeper
    {
        private const int Interval = 1000;
        private const int Unlocked = 0;
        private const Hash ActivateInteriorEntitySet = (Hash)0x55E86AF2712B36A1UL;

        private readonly OpenDoorsConfig _config;
        private readonly StoryGlobals _globals;
        private readonly List<Area> _areas = new List<Area>();
        private readonly Dictionary<int, int> _original = new Dictionary<int, int>();
        private int _next;

        public DoorKeeper(OpenDoorsConfig config, StoryGlobals globals)
        {
            _config = config;
            _globals = globals;
            foreach (DoorGroup group in config.Groups)
            {
                if (group == null) continue;
                float radius = group.Radius > 0f ? group.Radius : config.Radius;
                var area = new Area
                {
                    Group = group,
                    Name = string.IsNullOrEmpty(group.Name) ? "?" : group.Name,
                    Center = new Vector3(group.X, group.Y, group.Z),
                    RadiusSquared = radius * radius,
                };
                if (group.Doors != null)
                    foreach (string door in group.Doors)
                        if (!string.IsNullOrWhiteSpace(door)) area.Doors.Add(HashOf(door.Trim()));
                if (group.Buildings != null)
                    foreach (KeyValuePair<string, int> building in group.Buildings)
                        if (int.TryParse(building.Key, out int index) && index >= 0) area.Buildings[index] = building.Value;
                _areas.Add(area);
            }
        }

        public bool IsActive => _original.Count > 0;

        public void Tick()
        {
            if (!_config.Enabled || Game.GameTime < _next) return;
            _next = Game.GameTime + Interval;
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists()) return;
            Vector3 position = player.Position;
            foreach (Area area in _areas)
                if (position.DistanceToSquared(area.Center) <= area.RadiusSquared) Open(area);
        }

        public void Release(string reason)
        {
            _next = 0;
            foreach (Area area in _areas) area.Announced = false;
            if (_original.Count == 0) return;
            int relocked = 0;
            foreach (KeyValuePair<int, int> entry in _original)
            {
                if (entry.Value == Unlocked) continue;
                if (!Function.Call<bool>(Hash.IS_DOOR_REGISTERED_WITH_SYSTEM, entry.Key)) continue;
                if (Function.Call<int>(Hash.DOOR_SYSTEM_GET_DOOR_STATE, entry.Key) == entry.Value) continue;
                Function.Call(Hash.DOOR_SYSTEM_SET_DOOR_STATE, entry.Key, entry.Value, false, true);
                relocked++;
            }
            _original.Clear();
            Log.Info($"Doors handed back to the game ({reason}), {relocked} locked again.");
        }

        private void Open(Area area)
        {
            ApplyBuildings(area);
            UnlockInterior(area);
            if (!area.Prepared) area.Prepared = Prepare(area.Group);
            int opened = 0;
            foreach (int door in area.Doors)
            {
                if (!Function.Call<bool>(Hash.IS_DOOR_REGISTERED_WITH_SYSTEM, door) && !Register(door, area)) continue;
                int state = Function.Call<int>(Hash.DOOR_SYSTEM_GET_DOOR_STATE, door);
                if (!_original.TryGetValue(door, out int known) || (known == Unlocked && state != Unlocked)) _original[door] = state;
                if (state == Unlocked) continue;
                Function.Call(Hash.DOOR_SYSTEM_SET_DOOR_STATE, door, Unlocked, false, true);
                opened++;
            }
            if (opened > 0 && !area.Announced)
            {
                area.Announced = true;
                Log.Info($"Doors opened: {area.Name} ({opened}).");
            }
        }

        private void ApplyBuildings(Area area)
        {
            if (area.Buildings.Count == 0 || _globals == null || !_globals.Available) return;
            if (!_globals.BuildingsSupported)
            {
                if (!area.BuildingsWarned)
                {
                    area.BuildingsWarned = true;
                    Log.Warn($"Building states not supported by this game build ({area.Name}).");
                }
                return;
            }
            foreach (KeyValuePair<int, int> building in area.Buildings)
            {
                int state = _globals.GetBuildingState(building.Key);
                if (state == building.Value) continue;
                _globals.SetBuildingState(building.Key, building.Value);
                Log.Info($"Building {building.Key} set to state {building.Value} (was {state}, {area.Name}).");
            }
        }

        private static int FindInterior(InteriorSets sets) =>
            string.IsNullOrWhiteSpace(sets.Name)
                ? Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, sets.X, sets.Y, sets.Z)
                : Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS_WITH_TYPE, sets.X, sets.Y, sets.Z, sets.Name.Trim());

        private static void UnlockInterior(Area area)
        {
            InteriorSets sets = area.Group.Interior;
            if (sets == null || !sets.Unlock) return;
            int interior = FindInterior(sets);
            if (interior == 0)
            {
                if (!area.InteriorWarned)
                {
                    area.InteriorWarned = true;
                    Log.Warn($"Interior {sets.Name} not found ({area.Name}).");
                }
                return;
            }
            bool disabled = Function.Call<bool>(Hash.IS_INTERIOR_DISABLED, interior);
            bool capped = Function.Call<bool>(Hash.IS_INTERIOR_CAPPED, interior);
            if (disabled) Function.Call(Hash.DISABLE_INTERIOR, interior, false);
            if (capped) Function.Call(Hash.CAP_INTERIOR, interior, false);
            if (!area.InteriorPinned)
            {
                area.InteriorPinned = true;
                Function.Call(Hash.PIN_INTERIOR_IN_MEMORY, interior);
            }
            if (disabled || capped) Log.Info($"Interior {sets.Name} unlocked (disabled={disabled}, capped={capped}, {area.Name}).");
        }

        private bool Register(int door, Area area)
        {
            if (!Catalog.TryGetValue(door, out KnownDoor known)) return false;
            if (area.Missing.Contains(door)) return false;
            Function.Call(Hash.ADD_DOOR_TO_SYSTEM, door, Game.GenerateHash(known.Model), known.Position.X, known.Position.Y, known.Position.Z, false, false, false);
            if (Function.Call<bool>(Hash.IS_DOOR_REGISTERED_WITH_SYSTEM, door))
            {
                Log.Info($"Door {known.Model} registered ({area.Name}).");
                return true;
            }
            area.Missing.Add(door);
            Log.Warn($"Door {known.Model} could not be registered ({area.Name}).");
            return false;
        }

        private static readonly Dictionary<int, KnownDoor> Catalog = new Dictionary<int, KnownDoor>
        {
            { -826072862, new KnownDoor("v_ilev_ss_door8", 717f, -975f, 25f) },
            { 763780711, new KnownDoor("v_ilev_ss_door7", 719f, -975f, 25f) },
            { -874851305, new KnownDoor("v_ilev_ss_door02", 709.9813f, -963.5311f, 30.5453f) },
            { -1480820165, new KnownDoor("v_ilev_ss_door03", 709.9894f, -960.6675f, 30.5453f) },
            { 949391213, new KnownDoor("v_ilev_store_door", 707.8046f, -962.4564f, 30.5453f) },
            { -565026078, new KnownDoor("v_ilev_lester_doorfront", 1274f, -1721f, 55f) },
            { 1646172266, new KnownDoor("v_ilev_lester_doorveranda", 1271.89f, -1707.57f, 53.79f) },
            { 204467342, new KnownDoor("v_ilev_lester_doorveranda", 1270.77f, -1708.1f, 53.75f) },
        };

        private sealed class KnownDoor
        {
            public readonly string Model;
            public readonly Vector3 Position;

            public KnownDoor(string model, float x, float y, float z)
            {
                Model = model;
                Position = new Vector3(x, y, z);
            }
        }

        private static bool Prepare(DoorGroup group)
        {
            if (group.Ipls != null)
                foreach (string ipl in group.Ipls)
                    if (!string.IsNullOrWhiteSpace(ipl) && !Function.Call<bool>(Hash.IS_IPL_ACTIVE, ipl)) Function.Call(Hash.REQUEST_IPL, ipl);

            InteriorSets sets = group.Interior;
            if (sets == null) return true;
            int interior = FindInterior(sets);
            if (!Function.Call<bool>(Hash.IS_VALID_INTERIOR, interior)) return false;
            bool changed = false;
            if (sets.Disable != null)
                foreach (string name in sets.Disable)
                    if (Function.Call<bool>(Hash.IS_INTERIOR_ENTITY_SET_ACTIVE, interior, name))
                    {
                        Function.Call(Hash.DEACTIVATE_INTERIOR_ENTITY_SET, interior, name);
                        changed = true;
                    }
            if (sets.Enable != null)
                foreach (string name in sets.Enable)
                    if (!Function.Call<bool>(Hash.IS_INTERIOR_ENTITY_SET_ACTIVE, interior, name))
                    {
                        Function.Call(ActivateInteriorEntitySet, interior, name);
                        changed = true;
                    }
            if (changed) Function.Call(Hash.REFRESH_INTERIOR, interior);
            return true;
        }

        private static int HashOf(string name)
        {
            if (int.TryParse(name, out int value)) return value;
            uint hash = 0;
            foreach (char c in name.ToLowerInvariant())
            {
                hash += c;
                hash += hash << 10;
                hash ^= hash >> 6;
            }
            hash += hash << 3;
            hash ^= hash >> 11;
            hash += hash << 15;
            return unchecked((int)hash);
        }

        private sealed class Area
        {
            public DoorGroup Group;
            public string Name;
            public Vector3 Center;
            public float RadiusSquared;
            public bool Prepared;
            public bool Announced;
            public readonly List<int> Doors = new List<int>();
            public readonly HashSet<int> Missing = new HashSet<int>();
            public readonly Dictionary<int, int> Buildings = new Dictionary<int, int>();
            public bool BuildingsWarned;
            public bool InteriorWarned;
            public bool InteriorPinned;
        }
    }
}
