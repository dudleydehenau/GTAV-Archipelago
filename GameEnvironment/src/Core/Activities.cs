using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal sealed class Activities
    {
        private const int Interval = 5000;
        private static readonly int[] Bits = { 0, 1, 2, 3, 4, 5, 7, 8, 10, 11, 13, 15, 16, 17, 18 };
        private readonly StoryGlobals _globals;
        private int _next;
        private int _nextScan;
        private bool _warned;
        private bool _logged;
        private bool _blipWarned;
        private readonly Dictionary<int, Blip> _icons = new Dictionary<int, Blip>();
        private int _iconsLogged = -1;
        private bool _rangeOpened;
        private bool _doorsWarned;
        private bool _doorsLogged;
        private string _launchers = string.Empty;
        private static readonly string[] Launchers =
        {
            "launcher_offroadracing", "launcher_racing", "launcher_triathlon", "launcher_stunts", "launcher_pilotschool",
            "launcher_range", "launcher_golf", "launcher_tennis", "launcher_darts", "launcher_yoga", "launcher_basejumpheli",
            "launcher_basejumppack", "launcher_carwash", "launcher_hunting_ambient", "act_cinema", "cablecar",
            "offroad_races", "controller_races", "country_race", "triathlonsp", "stunt_plane_races", "pilot_school",
            "range_modern", "golf", "tennis", "darts", "yoga", "bj",
        };
        private static readonly string[] RangeWeapons = { "microsmg", "asltrifle", "pump", "mg" };
        private static readonly int[] Unused = { 65, 69 };
        private static readonly int[] Sprites = { 109, 122, 103, 197, 119, 110, 313, 314, 315, 316, 376, 127, 126, 94, 377, 141, 135, 36, 100, 71, 73, 75, 72, 52, 90 };

        public Activities(StoryGlobals globals)
        {
            _globals = globals;
        }

        private void OpenBlips()
        {
            if (!_globals.StaticBlipsSupported)
            {
                if (!_blipWarned)
                {
                    _blipWarned = true;
                    Log.Warn("Static map blips not found for this game build: activity icons stay as the game sets them.");
                }
                return;
            }
            int opened = 0;
            for (int i = 0; i < _globals.StaticBlipCountValue; i++)
            {
                if (System.Array.IndexOf(Sprites, _globals.StaticBlipSprite(i)) < 0 || System.Array.IndexOf(Unused, i) >= 0) continue;
                if ((_globals.StaticBlipFlags(i) & 1) != 0) continue;
                _globals.ActivateStaticBlip(i);
                opened++;
            }
            if (opened > 0) Log.Info($"{opened} activity and shop icons put on the map.");
            int own = 0;
            for (int i = 0; i < _globals.StaticBlipCountValue; i++)
            {
                int sprite = _globals.StaticBlipSprite(i);
                if (System.Array.IndexOf(Sprites, sprite) < 0 || System.Array.IndexOf(Unused, i) >= 0) continue;
                int handle = _globals.StaticBlipHandle(i);
                _icons.TryGetValue(i, out Blip mine);
                bool hasMine = mine != null && mine.Exists();
                bool gameBlip = handle != 0 && (!hasMine || handle != mine.Handle) && Function.Call<bool>(Hash.DOES_BLIP_EXIST, handle);
                if (gameBlip)
                {
                    if (hasMine) mine.Delete();
                    _icons.Remove(i);
                    continue;
                }
                if (hasMine)
                {
                    if (handle != mine.Handle) _globals.SetStaticBlipHandle(i, mine.Handle);
                    own++;
                    continue;
                }
                Vector3 position = _globals.StaticBlipPosition(i);
                if (position == Vector3.Zero) continue;
                Blip blip = Blip.Create(position);
                blip.Sprite = (BlipSprite)sprite;
                blip.IsShortRange = true;
                _icons[i] = blip;
                _globals.SetStaticBlipHandle(i, blip.Handle);
                own++;
            }
            if (own != _iconsLogged)
            {
                _iconsLogged = own;
                Log.Info($"{own} activity and shop icons drawn by GameEnvironment (the game did not draw them).");
            }
        }

        private void OpenRange()
        {
            _rangeOpened = true;
            int opened = 0;
            foreach (string weapon in RangeWeapons)
            {
                for (int character = 0; character < 3; character++)
                {
                    int stat = Function.Call<int>(Hash.GET_HASH_KEY, $"SP{character}_{weapon.ToUpperInvariant()}_HELDTIME");
                    var value = new OutputArgument();
                    if (!Function.Call<bool>(Hash.STAT_GET_INT, stat, value, -1) || value.GetResult<int>() > 0) continue;
                    Function.Call(Hash.STAT_SET_INT, stat, 1, true);
                    opened++;
                }
            }
            if (opened > 0) Log.Info("Shooting range: the SMG, rifle, shotgun and machine gun categories opened.");
        }

        private static readonly int[] Doors =
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
            32, 33, 34, 35, 36, 37, 54, 55, 56, 57, 58, 59, 60, 81, 82, 105, 106, 107, 108, 109, 117, 139, 140,
            162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184, 185,
            186, 187, 188, 189, 225,
        };

        private void OpenDoors()
        {
            if (!_globals.DoorsSupported)
            {
                if (!_doorsWarned)
                {
                    _doorsWarned = true;
                    Log.Warn("Door states not found for this game build: bars, clubs and shops keep the game's locks.");
                }
                return;
            }
            if (!_doorsLogged)
            {
                _doorsLogged = true;
                var locked = new List<int>();
                for (int door = 0; door < _globals.DoorCountValue; door++)
                    if (_globals.GetDoorState(door) != 0) locked.Add(door);
                Log.Info($"Locked doors at start: {(locked.Count == 0 ? "none" : string.Join(" ", locked))}.");
            }
            var opened = new List<int>();
            foreach (int door in Doors)
            {
                if (_globals.GetDoorState(door) != 1) continue;
                _globals.SetDoorState(door, 0);
                opened.Add(door);
            }
            if (opened.Count > 0) Log.Info($"Doors unlocked for activities and shops: {string.Join(" ", opened)}.");
        }

        private void LogLaunchers()
        {
            var running = new List<string>();
            foreach (string name in Launchers)
                if (Function.Call<int>(Hash.GET_NUMBER_OF_THREADS_RUNNING_THE_SCRIPT_WITH_THIS_HASH, Function.Call<int>(Hash.GET_HASH_KEY, name)) > 0)
                    running.Add(name);
            string now = string.Join(", ", running);
            if (now == _launchers) return;
            _launchers = now;
            Vector3 at = Game.Player.Character.Position;
            Log.Info($"Activity scripts running: {(now.Length == 0 ? "none" : now)} (player at {at.X:F0}, {at.Y:F0}, {at.Z:F0}, {Game.Player.Character.Model.Hash:X}).");
        }

        public void Clear()
        {
            foreach (KeyValuePair<int, Blip> icon in _icons)
            {
                if (icon.Value == null || !icon.Value.Exists()) continue;
                if (_globals.StaticBlipsSupported && _globals.StaticBlipHandle(icon.Key) == icon.Value.Handle) _globals.SetStaticBlipHandle(icon.Key, 0);
                icon.Value.Delete();
            }
            _icons.Clear();
        }

        public void Tick(int now)
        {
            if (now >= _nextScan)
            {
                _nextScan = now + 1000;
                LogLaunchers();
            }
            if (now < _next || !_globals.Available) return;
            _next = now + Interval;
            if (!_globals.MinigamesSupported)
            {
                if (!_warned)
                {
                    _warned = true;
                    Log.Warn("Activity flags not found for this game build: activities stay as the game sets them.");
                }
                return;
            }
            OpenBlips();
            int mask = 0;
            foreach (int bit in Bits) mask |= 1 << bit;
            int bits = _globals.MinigameBits;
            if (!_logged) Log.Info($"Activity flags: {bits:X}.");
            if ((bits & mask) != mask)
            {
                _globals.MinigameBits = bits | mask;
                if (!_logged) Log.Info($"Activities opened (flags {bits:X} -> {bits | mask:X}).");
                _logged = true;
            }
            if (!_rangeOpened) OpenRange();
            OpenDoors();
            if (_globals.RaiseFlowFlag(65) | _globals.RaiseFlowFlag(66)) Log.Info("Darts opened for the three characters.");
            if (_globals.OpenRaceProgress()) Log.Info("All off-road races, triathlons and street races unlocked.");
            int shops = _globals.ShopCount;
            int opened = 0;
            int closed = 0;
            bool mission = Function.Call<bool>(Hash.GET_MISSION_FLAG);
            for (int shop = 0; shop < shops; shop++)
            {
                int flags = _globals.GetShopFlags(shop);
                ShopKind? kind = GameShopWatcher.KindOf(shop);
                if (!mission && kind.HasValue && GameShopWatcher.IsLocked(kind.Value))
                {
                    if ((flags & 1) == 0) continue;
                    _globals.SetShopFlags(shop, flags & ~1);
                    closed++;
                    continue;
                }
                if ((flags & 1) != 0) continue;
                _globals.SetShopFlags(shop, flags | 1);
                opened++;
            }
            if (opened > 0) Log.Info($"{opened} shops opened.");
            if (closed > 0) Log.Info($"{closed} shops closed until their item is found.");
        }
    }
}
