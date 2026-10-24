using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using GTA;
using GTA.Native;

namespace GameArchipelago.Core
{
    internal sealed class VehicleGift
    {
        public string Name { get; set; }
        public string Model { get; set; }
        public int Weight { get; set; } = 1;
        public bool Water { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    internal sealed class WeaponGift
    {
        public string Name { get; set; }
        public string Weapon { get; set; }
        public int Ammo { get; set; } = 100;
        public int Weight { get; set; } = 1;
    }

    internal sealed class GiftCatalog
    {
        private List<VehicleGift> _vehicles = new List<VehicleGift>();
        private List<WeaponGift> _weapons = new List<WeaponGift>();

        public IReadOnlyList<VehicleGift> Vehicles => _vehicles;
        public IReadOnlyList<WeaponGift> Weapons => _weapons;

        public void Load(string directory)
        {
            _vehicles = Read<VehicleGift>(Path.Combine(directory, "vehicles.json"))
                .Where(v => v != null && !string.IsNullOrWhiteSpace(v.Model) && v.Weight > 0).ToList();
            _weapons = Read<WeaponGift>(Path.Combine(directory, "weapons.json"))
                .Where(w => w != null && !string.IsNullOrWhiteSpace(w.Weapon) && w.Weight > 0).ToList();
            if (_vehicles.Count == 0) _vehicles.Add(new VehicleGift { Name = "Bravado Banshee", Model = "banshee" });
            if (_weapons.Count == 0) _weapons.Add(new WeaponGift { Name = "Pistol", Weapon = "WEAPON_PISTOL", Ammo = 120 });
            bool categories = _vehicles.Any(v => !string.IsNullOrWhiteSpace(v.Category));
            foreach (VehicleGift vehicle in _vehicles)
            {
                if (string.IsNullOrWhiteSpace(vehicle.Name)) vehicle.Name = vehicle.Model;
                if (!categories && KnownCategories.TryGetValue(vehicle.Model, out string category)) vehicle.Category = category;
            }
            foreach (WeaponGift weapon in _weapons)
                if (string.IsNullOrWhiteSpace(weapon.Name)) weapon.Name = weapon.Weapon;
            Log.Info($"Gifts: {_vehicles.Count} vehicles, {_weapons.Count} weapons.");
        }

        private static readonly Dictionary<string, string> KnownCategories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "zentorno", "Sports Car" }, { "adder", "Sports Car" }, { "t20", "Sports Car" }, { "pariah", "Sports Car" },
            { "ztype", "Sports Car" }, { "voltic2", "Sports Car" },
            { "shotaro", "Motorcycle" }, { "bati", "Motorcycle" }, { "hakuchou2", "Motorcycle" }, { "oppressor", "Motorcycle" },
            { "seasparrow", "Helicopter" }, { "akula", "Helicopter" }, { "buzzard", "Helicopter" }, { "maverick", "Helicopter" },
            { "stromberg", "Underwater Vehicle" }, { "toreador", "Underwater Vehicle" }, { "submersible2", "Underwater Vehicle" },
            { "submersible", "Underwater Vehicle" },
            { "seashark", "Boat" }, { "speeder", "Boat" }, { "longfin", "Boat" }, { "toro", "Boat" },
            { "scramjet", "Scramjet" },
        };

        private static readonly Dictionary<string, VehicleGift> CategoryDefaults = new Dictionary<string, VehicleGift>(StringComparer.OrdinalIgnoreCase)
        {
            { "Sports Car", new VehicleGift { Name = "Pegassi Zentorno", Model = "zentorno", Category = "Sports Car" } },
            { "Motorcycle", new VehicleGift { Name = "Pegassi Bati 801", Model = "bati", Category = "Motorcycle" } },
            { "Helicopter", new VehicleGift { Name = "Buckingham Maverick", Model = "maverick", Category = "Helicopter" } },
            { "Underwater Vehicle", new VehicleGift { Name = "Ocelot Stromberg", Model = "stromberg", Category = "Underwater Vehicle" } },
            { "Boat", new VehicleGift { Name = "Pegassi Speeder", Model = "speeder", Water = true, Category = "Boat" } },
            { "Scramjet", new VehicleGift { Name = "Declasse Scramjet", Model = "scramjet", Category = "Scramjet" } },
        };

        public VehicleGift PickVehicle(string seed, int index) => Pick(_vehicles, v => v.Weight, seed, index, IsValidVehicle);

        public VehicleGift PickCategory(string category, string seed, int index)
        {
            List<VehicleGift> matching = _vehicles.Where(v => string.Equals(v.Category ?? string.Empty, category, StringComparison.OrdinalIgnoreCase)).ToList();
            VehicleGift picked = matching.Count > 0 ? Pick(matching, v => v.Weight, seed, index, IsValidVehicle) : null;
            if (picked != null && IsValidVehicle(picked)) return picked;
            return CategoryDefaults.TryGetValue(category, out VehicleGift fallback) ? fallback : PickVehicle(seed, index);
        }

        public WeaponGift PickWeapon(string seed, int index) => Pick(_weapons, w => w.Weight, seed, index, IsValidWeapon);

        private static T Pick<T>(List<T> list, Func<T, int> weight, string seed, int index, Func<T, bool> valid) where T : class
        {
            var random = new Random(StableHash(seed + "#" + index));
            var candidates = list.ToList();
            while (candidates.Count > 0)
            {
                int total = candidates.Sum(weight);
                int roll = random.Next(total);
                T picked = candidates[candidates.Count - 1];
                foreach (T candidate in candidates)
                {
                    roll -= weight(candidate);
                    if (roll < 0)
                    {
                        picked = candidate;
                        break;
                    }
                }
                if (valid(picked)) return picked;
                candidates.Remove(picked);
            }
            return list.FirstOrDefault();
        }

        private static bool IsValidVehicle(VehicleGift gift)
        {
            var model = new Model(gift.Model);
            bool ok = model.IsInCdImage && model.IsValid && model.IsVehicle;
            if (!ok) Log.Warn($"vehicles.json: '{gift.Model}' is not a vehicle of this game, skipped.");
            return ok;
        }

        private static bool IsValidWeapon(WeaponGift gift)
        {
            if (string.Equals(gift.Weapon, "ARMOR", StringComparison.OrdinalIgnoreCase)) return true;
            bool ok = Function.Call<bool>(Hash.IS_WEAPON_VALID, StringHash.AtStringHash(gift.Weapon, 0u));
            if (!ok) Log.Warn($"weapons.json: '{gift.Weapon}' is not a weapon of this game, skipped.");
            return ok;
        }

        private static int StableHash(string text)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in text ?? string.Empty)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return (int)(hash & 0x7FFFFFFF);
            }
        }

        private static List<T> Read<T>(string path)
        {
            if (!File.Exists(path))
            {
                Log.Warn(Path.GetFileName(path) + " not found.");
                return new List<T>();
            }
            try
            {
                return new JavaScriptSerializer().Deserialize<List<T>>(File.ReadAllText(path)) ?? new List<T>();
            }
            catch (Exception ex)
            {
                Log.Error(Path.GetFileName(path) + " is invalid.", ex);
                return new List<T>();
            }
        }
    }
}
