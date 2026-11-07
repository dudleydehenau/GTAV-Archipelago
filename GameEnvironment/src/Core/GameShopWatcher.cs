using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal sealed class GameShopWatcher
    {
        private const float Radius = 35f;
        private const int SettleTime = 700;
        private const int MaskShop = 21;

        private sealed class GameShopSpot
        {
            public GameShopSpot(int index, ShopKind kind, float x, float y, float z)
            {
                Index = index;
                Kind = kind;
                Position = new Vector3(x, y, z);
            }

            public int Index { get; }
            public ShopKind Kind { get; }
            public Vector3 Position { get; }
        }

        private static readonly GameShopSpot[] Spots =
        {
            new GameShopSpot(0, ShopKind.Barber, -822.0f, -187.2f, 36.6f),
            new GameShopSpot(1, ShopKind.Barber, 133.6f, -1710.9f, 28.3f),
            new GameShopSpot(2, ShopKind.Barber, -1287.1f, -1116.6f, 6.0f),
            new GameShopSpot(3, ShopKind.Barber, 1933.1f, 3726.1f, 31.8f),
            new GameShopSpot(4, ShopKind.Barber, 1208.3f, -470.9f, 65.2f),
            new GameShopSpot(5, ShopKind.Barber, -30.7f, -148.5f, 56.1f),
            new GameShopSpot(6, ShopKind.Barber, -280.8f, 6231.8f, 30.7f),
            new GameShopSpot(7, ShopKind.Clothes, 80.7f, -1391.7f, 28.4f),
            new GameShopSpot(8, ShopKind.Clothes, 1687.9f, 4820.6f, 41.0f),
            new GameShopSpot(9, ShopKind.Clothes, 419.5f, -807.6f, 28.5f),
            new GameShopSpot(10, ShopKind.Clothes, -1094.0f, 2704.2f, 18.1f),
            new GameShopSpot(11, ShopKind.Clothes, 1198.0f, 2704.2f, 37.2f),
            new GameShopSpot(12, ShopKind.Clothes, -818.6f, -1077.5f, 10.3f),
            new GameShopSpot(13, ShopKind.Clothes, -0.2f, 6516.0f, 30.9f),
            new GameShopSpot(14, ShopKind.Clothes, -1199.8f, -776.7f, 16.3f),
            new GameShopSpot(15, ShopKind.Clothes, 618.2f, 2752.6f, 41.1f),
            new GameShopSpot(16, ShopKind.Clothes, 126.7f, -212.5f, 53.6f),
            new GameShopSpot(17, ShopKind.Clothes, -3169.0f, 1055.3f, 19.9f),
            new GameShopSpot(18, ShopKind.Clothes, -715.4f, -155.8f, 36.4f),
            new GameShopSpot(19, ShopKind.Clothes, -158.2f, -305.0f, 38.7f),
            new GameShopSpot(20, ShopKind.Clothes, -1455.0f, -233.2f, 48.8f),
            new GameShopSpot(21, ShopKind.Clothes, -1336.0f, -1278.6f, 3.9f),
            new GameShopSpot(22, ShopKind.Tattoo, 321.6f, 179.4f, 102.6f),
            new GameShopSpot(23, ShopKind.Tattoo, 1861.7f, 3750.1f, 32.0f),
            new GameShopSpot(24, ShopKind.Tattoo, -290.2f, 6199.1f, 30.5f),
            new GameShopSpot(25, ShopKind.Tattoo, -1153.9f, -1425.0f, 4.0f),
            new GameShopSpot(26, ShopKind.Tattoo, 1322.5f, -1651.1f, 51.2f),
            new GameShopSpot(27, ShopKind.Tattoo, -3169.4f, 1074.7f, 19.8f),
            new GameShopSpot(39, ShopKind.Customs, -1147.3f, -1992.4f, 12.2f),
            new GameShopSpot(40, ShopKind.Customs, 724.5f, -1089.1f, 21.2f),
            new GameShopSpot(41, ShopKind.Customs, -354.5f, -135.4f, 38.2f),
            new GameShopSpot(42, ShopKind.Customs, 113.3f, 6624.3f, 30.8f),
            new GameShopSpot(43, ShopKind.Customs, 1174.7f, 2644.5f, 36.8f),
            new GameShopSpot(44, ShopKind.Customs, -211.5f, -1324.2f, 30.3f),
        };

        private static readonly Dictionary<ShopKind, string> ScriptNames = new Dictionary<ShopKind, string>
        {
            { ShopKind.Barber, "hairdo_shop_sp" },
            { ShopKind.Clothes, "clothes_shop_sp" },
            { ShopKind.Tattoo, "tattoo_shop" },
            { ShopKind.Customs, "carmod_shop" },
        };

        private readonly Dictionary<ShopKind, int> _scripts = new Dictionary<ShopKind, int>();

        private static readonly HashSet<ShopKind> Locked = new HashSet<ShopKind>();

        private GameShopSpot _spot;
        private Dictionary<string, int> _baseline;
        private int _vehicle;
        private int _money;
        private int _settleAt = -1;
        private string _zone;

        public static bool IsLocked(ShopKind kind)
        {
            lock (Locked) return Locked.Contains(kind);
        }

        public static void SetLocked(ShopKind kind, bool locked)
        {
            lock (Locked)
            {
                if (locked) Locked.Add(kind);
                else Locked.Remove(kind);
            }
        }

        public static ShopKind? LockedNear(Vector3 here, float range)
        {
            lock (Locked)
            {
                if (Locked.Count == 0) return null;
                foreach (GameShopSpot spot in Spots)
                    if (Locked.Contains(spot.Kind) && spot.Position.DistanceTo(here) < range) return spot.Kind;
            }
            return null;
        }

        public static ShopKind? KindOf(int shop)
        {
            foreach (GameShopSpot spot in Spots)
                if (spot.Index == shop) return spot.Kind;
            return null;
        }

        public void Tick()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead)
            {
                _spot = null;
                return;
            }
            GameShopSpot spot = Nearest(player.Position);
            if (spot == null)
            {
                _spot = null;
                _settleAt = -1;
                return;
            }
            int vehicle = player.CurrentVehicle?.Handle ?? 0;
            if (spot != _spot || (spot.Kind == ShopKind.Customs && vehicle != _vehicle))
            {
                _spot = spot;
                _vehicle = vehicle;
                _baseline = Capture(spot.Kind, player);
                _money = Game.Player.Money;
                _settleAt = -1;
                return;
            }
            int money = Game.Player.Money;
            if (money < _money && _settleAt < 0)
            {
                _settleAt = Game.GameTime + SettleTime;
                if (spot.Kind == ShopKind.Tattoo) _zone = TattooZone(player);
            }
            _money = money;
            if (_settleAt < 0 || Game.GameTime < _settleAt) return;
            _settleAt = -1;
            Dictionary<string, int> now = Capture(spot.Kind, player);
            List<string> categories = Diff(spot, _baseline, now);
            _baseline = now;
            Log.Info($"Shop purchase: {spot.Kind} (shop {spot.Index}), {(categories.Count == 0 ? "no category found" : string.Join(", ", categories))}.");
            EnvironmentApi.RaiseGameShopPurchase(spot.Kind, categories.ToArray());
        }

        private int ScriptOf(ShopKind kind)
        {
            if (!_scripts.TryGetValue(kind, out int hash))
            {
                hash = Function.Call<int>(Hash.GET_HASH_KEY, ScriptNames[kind]);
                _scripts[kind] = hash;
            }
            return hash;
        }

        private GameShopSpot Nearest(Vector3 position)
        {
            GameShopSpot best = null;
            float bestDistance = Radius * Radius;
            foreach (GameShopSpot spot in Spots)
            {
                float distance = position.DistanceToSquared(spot.Position);
                if (distance > bestDistance) continue;
                if (Function.Call<int>(Hash.GET_NUMBER_OF_THREADS_RUNNING_THE_SCRIPT_WITH_THIS_HASH, ScriptOf(spot.Kind)) == 0) continue;
                best = spot;
                bestDistance = distance;
            }
            return best;
        }

        private static Dictionary<string, int> Capture(ShopKind kind, Ped player)
        {
            var values = new Dictionary<string, int>();
            switch (kind)
            {
                case ShopKind.Barber:
                    Component(values, player, 1);
                    Component(values, player, 2);
                    values["overlay1"] = Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY, player.Handle, 1);
                    break;
                case ShopKind.Clothes:
                    foreach (int component in new[] { 1, 3, 4, 6, 8, 11 }) Component(values, player, component);
                    foreach (int prop in new[] { 0, 1 })
                    {
                        values["p" + prop] = Function.Call<int>(Hash.GET_PED_PROP_INDEX, player.Handle, prop);
                        values["pt" + prop] = Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX, player.Handle, prop);
                    }
                    break;
                case ShopKind.Customs:
                    Vehicle vehicle = player.CurrentVehicle;
                    if (vehicle == null || !vehicle.Exists()) break;
                    int handle = vehicle.Handle;
                    for (int mod = 0; mod <= 48; mod++)
                    {
                        if (mod >= 17 && mod <= 22) continue;
                        values["m" + mod] = Function.Call<int>(Hash.GET_VEHICLE_MOD, handle, mod);
                    }
                    foreach (int toggle in new[] { 18, 20, 22 })
                        values["t" + toggle] = Function.Call<bool>(Hash.IS_TOGGLE_MOD_ON, handle, toggle) ? 1 : 0;
                    var a = new OutputArgument();
                    var b = new OutputArgument();
                    Function.Call(Hash.GET_VEHICLE_COLOURS, handle, a, b);
                    values["c1"] = a.GetResult<int>();
                    values["c2"] = b.GetResult<int>();
                    var pearl = new OutputArgument();
                    var rim = new OutputArgument();
                    Function.Call(Hash.GET_VEHICLE_EXTRA_COLOURS, handle, pearl, rim);
                    values["c3"] = pearl.GetResult<int>();
                    values["w1"] = rim.GetResult<int>();
                    values["w2"] = Function.Call<int>(Hash.GET_VEHICLE_WHEEL_TYPE, handle);
                    values["glass"] = Function.Call<int>(Hash.GET_VEHICLE_WINDOW_TINT, handle);
                    values["plate"] = Function.Call<int>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT_INDEX, handle);
                    values["burst"] = Function.Call<bool>(Hash.GET_VEHICLE_TYRES_CAN_BURST, handle) ? 1 : 0;
                    var r = new OutputArgument();
                    var g = new OutputArgument();
                    var bl = new OutputArgument();
                    Function.Call(Hash.GET_VEHICLE_TYRE_SMOKE_COLOR, handle, r, g, bl);
                    values["smoke"] = (r.GetResult<int>() << 16) | (g.GetResult<int>() << 8) | bl.GetResult<int>();
                    break;
            }
            return values;
        }

        private static void Component(Dictionary<string, int> values, Ped player, int component)
        {
            values["d" + component] = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, player.Handle, component);
            values["x" + component] = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, player.Handle, component);
        }

        private static bool Changed(Dictionary<string, int> before, Dictionary<string, int> after, params string[] keys)
        {
            foreach (string key in keys)
            {
                before.TryGetValue(key, out int a);
                after.TryGetValue(key, out int b);
                if (a != b) return true;
            }
            return false;
        }

        private List<string> Diff(GameShopSpot spot, Dictionary<string, int> before, Dictionary<string, int> after)
        {
            var found = new List<string>();
            switch (spot.Kind)
            {
                case ShopKind.Barber:
                    if (Changed(before, after, "d2", "x2")) found.Add("Haircut");
                    if (Changed(before, after, "d1", "x1", "overlay1")) found.Add("Beard");
                    break;
                case ShopKind.Clothes:
                    if (spot.Index == MaskShop)
                    {
                        found.Add("Masks");
                        break;
                    }
                    var parts = new List<string>();
                    if (Changed(before, after, "d3", "x3", "d8", "x8", "d11", "x11")) parts.Add("Tops");
                    if (Changed(before, after, "d4", "x4")) parts.Add("Pants");
                    if (Changed(before, after, "d6", "x6")) parts.Add("Shoes");
                    if (Changed(before, after, "p0", "pt0")) parts.Add("Hats");
                    if (Changed(before, after, "p1", "pt1")) parts.Add("Glasses");
                    if (Changed(before, after, "d1", "x1")) parts.Add("Masks");
                    if (parts.Count >= 3) found.Add("Outfits");
                    found.AddRange(parts);
                    break;
                case ShopKind.Tattoo:
                    if (_zone != null) found.Add(_zone);
                    _zone = null;
                    break;
                case ShopKind.Customs:
                    if (Changed(before, after, "m11")) found.Add("Engine");
                    if (Changed(before, after, "m12")) found.Add("Brakes");
                    if (Changed(before, after, "m13")) found.Add("Transmission");
                    if (Changed(before, after, "m15")) found.Add("Suspension");
                    if (Changed(before, after, "m16")) found.Add("Armor");
                    if (Changed(before, after, "t18")) found.Add("Turbo");
                    if (Changed(before, after, "m14")) found.Add("Horn");
                    if (Changed(before, after, "t22")) found.Add("Headlights");
                    if (Changed(before, after, "m23", "m24", "w1", "w2")) found.Add("Wheels");
                    if (Changed(before, after, "t20", "smoke", "burst")) found.Add("Tires");
                    if (Changed(before, after, "c1", "c2", "c3")) found.Add("Respray");
                    if (Changed(before, after, "glass")) found.Add("Windows");
                    if (Changed(before, after, "plate")) found.Add("Plate");
                    for (int mod = 0; mod <= 48; mod++)
                    {
                        if (mod >= 11 && mod <= 24) continue;
                        if (!Changed(before, after, "m" + mod)) continue;
                        found.Add("Body Kit");
                        break;
                    }
                    break;
            }
            return found;
        }

        private static string TattooZone(Ped player)
        {
            int cam = Function.Call<int>(Hash.GET_RENDERING_CAM);
            if (cam <= 0) return null;
            Vector3 origin = Function.Call<Vector3>(Hash.GET_CAM_COORD, cam);
            Vector3 rotation = Function.Call<Vector3>(Hash.GET_CAM_ROT, cam, 2);
            double pitch = rotation.X * Math.PI / 180.0;
            double yaw = rotation.Z * Math.PI / 180.0;
            var forward = new Vector3((float)(-Math.Sin(yaw) * Math.Cos(pitch)), (float)(Math.Cos(yaw) * Math.Cos(pitch)), (float)Math.Sin(pitch));
            float along = Vector3.Dot(player.Position - origin, forward);
            Vector3 target = origin + forward * along;
            Vector3 local = Function.Call<Vector3>(Hash.GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS, player.Handle, target.X, target.Y, target.Z);
            string zone;
            if (local.Z > 0.5f) zone = "Head";
            else if (local.Z < -0.2f) zone = local.X < 0f ? "Left Leg" : "Right Leg";
            else if (Math.Abs(local.X) > 0.18f) zone = local.X < 0f ? "Left Arm" : "Right Arm";
            else zone = "Torso";
            Log.Info($"Tattoo camera at {local.X:F2}, {local.Y:F2}, {local.Z:F2}: {zone}.");
            return zone;
        }
    }
}
