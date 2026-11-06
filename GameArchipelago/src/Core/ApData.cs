using System;
using System.Collections.Generic;
using System.Linq;

namespace GameArchipelago.Core
{
    internal sealed class MarkerPoint
    {
        public MarkerPoint(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
    }

    internal sealed class MissionInfo
    {
        public MissionInfo(string code, string name, bool strangers, int triggers, int players, int minutes, long item, long location,
            long[] bonus, MarkerPoint michael, MarkerPoint franklin, MarkerPoint trevor)
        {
            Code = code;
            Name = name;
            Strangers = strangers;
            Triggers = triggers;
            Players = players;
            Minutes = minutes;
            Item = item;
            Location = location;
            Bonus = bonus;
            Points = new[] { michael, franklin, trevor };
        }

        public string Code { get; }
        public string Name { get; }
        public bool Strangers { get; }
        public int Triggers { get; }
        public int Players { get; }
        public int Minutes { get; }
        public long Item { get; }
        public long Location { get; }
        public long[] Bonus { get; }
        public MarkerPoint[] Points { get; }

        public bool TriggeredBy(int character) => (Triggers & (1 << character)) != 0;

        public bool Needs(int character) => (Players & (1 << character)) != 0;

        public bool PlayableWith(int mask) => (Triggers & mask) != 0 && (Players & mask) == Players;
    }

    internal sealed class VehicleItem
    {
        public VehicleItem(long item, string category)
        {
            Item = item;
            Category = category;
        }

        public long Item { get; }
        public string Category { get; }
    }

    internal sealed class TrapInfo
    {
        public TrapInfo(long item, string id, string name)
        {
            Item = item;
            Id = id;
            Name = name;
        }

        public long Item { get; }
        public string Id { get; }
        public string Name { get; }
    }

    internal sealed class MoneyInfo
    {
        public MoneyInfo(long item, string name, int amount)
        {
            Item = item;
            Name = name;
            Amount = amount;
        }

        public long Item { get; }
        public string Name { get; }
        public int Amount { get; }
    }

    internal sealed class CollectibleSet
    {
        public CollectibleSet(string key, string name, int total, bool underwater, long baseLocation, string label, bool freeRoam)
        {
            Key = key;
            Name = name;
            Total = total;
            Underwater = underwater;
            BaseLocation = baseLocation;
            Label = label;
            FreeRoam = freeRoam;
        }

        public string Label { get; }
        public bool FreeRoam { get; }

        public string Describe(int count) => Name + ": " + Label.Replace("{n}", count.ToString());

        public string Key { get; }
        public string Name { get; }
        public int Total { get; }
        public bool Underwater { get; }
        public long BaseLocation { get; }

        public long LocationFor(int count) => BaseLocation + count;

        public long LocationFor(int count, int bonus) => BaseLocation + bonus * 1000L + count;

        public bool Contains(long location)
        {
            if (location > BaseLocation && location <= BaseLocation + Total) return true;
            for (int b = 1; b <= ApData.MaxCollectibleBonus; b++)
            {
                long start = BaseLocation + b * 1000L;
                if (location > start && location <= start + Total) return true;
            }
            return false;
        }
    }

    internal sealed class VehicleUnlock
    {
        public VehicleUnlock(long item, string model, string name, bool water)
        {
            Item = item;
            Model = model;
            Name = name;
            Water = water;
        }

        public long Item { get; }
        public string Model { get; }
        public string Name { get; }
        public bool Water { get; }
    }

    internal sealed class ShopUpgrade
    {
        public ShopUpgrade(string component, string name, long location)
        {
            Component = component;
            Name = name;
            Location = location;
        }

        public string Component { get; }
        public string Name { get; }
        public long Location { get; }
    }

    internal sealed class GameShopCategory
    {
        public GameShopCategory(string name, long location)
        {
            Name = name;
            Location = location;
        }

        public string Name { get; }
        public long Location { get; }
    }

    internal sealed class GameShopInfo
    {
        public GameShopInfo(string key, string label, long item, GameShopCategory[] categories)
        {
            Key = key;
            Label = label;
            Item = item;
            Categories = categories;
        }

        public string Key { get; }
        public string Label { get; }
        public long Item { get; }
        public GameShopCategory[] Categories { get; }
    }

    internal sealed class ShopWeapon
    {
        public ShopWeapon(string id, string name, long item, long location, long upgradesItem, ShopUpgrade[] upgrades)
        {
            Id = id;
            Name = name;
            Item = item;
            Location = location;
            UpgradesItem = upgradesItem;
            Upgrades = upgrades ?? Array.Empty<ShopUpgrade>();
        }

        public string Id { get; }
        public string Name { get; }
        public long Item { get; }
        public long Location { get; }
        public long UpgradesItem { get; }
        public ShopUpgrade[] Upgrades { get; }

        public ShopUpgrade Upgrade(string component) =>
            Upgrades.FirstOrDefault(u => string.Equals(u.Component, component, StringComparison.OrdinalIgnoreCase));
    }

    internal enum FeatGroup
    {
        Landmark,
        Skill,
        Escape,
        Activity,
    }

    internal sealed class FeatLocation
    {
        public FeatLocation(string id, long location, string name, FeatGroup group, int level)
        {
            Id = id;
            Location = location;
            Name = name;
            Group = group;
            Level = level;
        }

        public string Id { get; }
        public long Location { get; }
        public string Name { get; }
        public FeatGroup Group { get; }
        public int Level { get; }
    }

    internal static partial class ApData
    {
        public static readonly string[] CharacterNames = { "Michael", "Franklin", "Trevor" };

        private static Dictionary<string, MissionInfo> _missionsByCode;
        private static Dictionary<long, MissionInfo> _missionsByItem;
        private static Dictionary<long, TrapInfo> _trapsByItem;
        private static Dictionary<long, MoneyInfo> _moneyByItem;
        private static Dictionary<string, FeatLocation> _featsById;
        private static Dictionary<long, FeatLocation> _featsByLocation;

        private static Dictionary<string, FeatLocation> FeatsById =>
            _featsById ?? (_featsById = Feats.ToDictionary(f => f.Id, StringComparer.OrdinalIgnoreCase));

        private static Dictionary<long, FeatLocation> FeatsByLocation =>
            _featsByLocation ?? (_featsByLocation = Feats.ToDictionary(f => f.Location));

        private static Dictionary<string, ShopWeapon> _shopById;

        public static ShopWeapon ShopWeaponById(string id)
        {
            if (_shopById == null) _shopById = ShopWeapons.ToDictionary(w => w.Id, StringComparer.OrdinalIgnoreCase);
            return id != null && _shopById.TryGetValue(id, out ShopWeapon weapon) ? weapon : null;
        }

        public static GameShopInfo GameShopByItem(long item) => GameShops.FirstOrDefault(s => s.Item == item);

        public static GameShopInfo GameShopByKey(string key) => GameShops.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));

        public static VehicleUnlock VehicleUnlockByItem(long item) => VehicleUnlocks.FirstOrDefault(v => v.Item == item);

        public static ShopWeapon ShopWeaponByItem(long item) => ShopWeapons.FirstOrDefault(w => w.Item == item);

        public static ShopWeapon ShopUpgradesByItem(long item) => item == 0 ? null : ShopWeapons.FirstOrDefault(w => w.UpgradesItem == item);

        public static FeatLocation FeatById(string id) => id != null && FeatsById.TryGetValue(id, out FeatLocation f) ? f : null;

        public static FeatLocation FeatByLocation(long location) => FeatsByLocation.TryGetValue(location, out FeatLocation f) ? f : null;

        public static bool IsFreeRoam(long location) =>
            FeatByLocation(location) != null || Collectibles.Any(c => c.FreeRoam && c.Contains(location));

        private static Dictionary<string, MissionInfo> MissionsByCode =>
            _missionsByCode ?? (_missionsByCode = Missions.ToDictionary(m => m.Code, StringComparer.OrdinalIgnoreCase));

        private static Dictionary<long, MissionInfo> MissionsByItem => _missionsByItem ?? (_missionsByItem = Missions.ToDictionary(m => m.Item));
        private static Dictionary<long, TrapInfo> TrapsByItem => _trapsByItem ?? (_trapsByItem = Traps.ToDictionary(t => t.Item));
        private static Dictionary<long, MoneyInfo> MoneyByItem => _moneyByItem ?? (_moneyByItem = Money.ToDictionary(m => m.Item));

        public static MissionInfo MissionByCode(string code) =>
            code != null && MissionsByCode.TryGetValue(code, out MissionInfo m) ? m : null;

        public static MissionInfo MissionByItem(long item) => MissionsByItem.TryGetValue(item, out MissionInfo m) ? m : null;

        public static TrapInfo TrapByItem(long item) => TrapsByItem.TryGetValue(item, out TrapInfo t) ? t : null;

        public static TrapInfo TrapById(string id) => Traps.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));

        public static MoneyInfo MoneyByItemId(long item) => MoneyByItem.TryGetValue(item, out MoneyInfo m) ? m : null;

        public static VehicleItem VehicleByItem(long item) => VehicleItems.FirstOrDefault(v => v.Item == item);

        public static int CharacterByItem(long item) => Array.IndexOf(CharacterItems, item);

        public static CollectibleSet CollectibleByKey(string key) =>
            Collectibles.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));

        public static string LocationName(long location)
        {
            foreach (MissionInfo mission in Missions)
            {
                if (mission.Location == location) return mission.Name;
                for (int b = 0; b < mission.Bonus.Length; b++)
                    if (mission.Bonus[b] == location) return $"{mission.Name} (Bonus {b + 1})";
            }
            if (location > GoldBase && location <= GoldBase + MaxGoldMedals) return "Gold Medal " + (location - GoldBase);
            if (location > ObjectiveBase && location <= ObjectiveBase + MaxObjectives) return "Objective " + (location - ObjectiveBase);
            foreach (CollectibleSet set in Collectibles)
            {
                if (location > set.BaseLocation && location <= set.BaseLocation + set.Total)
                    return set.Describe((int)(location - set.BaseLocation));
                for (int b = 1; b <= MaxCollectibleBonus; b++)
                {
                    long start = set.BaseLocation + b * 1000L;
                    if (location > start && location <= start + set.Total)
                        return $"{set.Describe((int)(location - start))} (Bonus {b})";
                }
            }
            FeatLocation feat = FeatByLocation(location);
            if (feat != null) return feat.Name;
            foreach (ShopWeapon weapon in ShopWeapons)
            {
                if (weapon.Location == location) return "Buy " + weapon.Name;
                foreach (ShopUpgrade upgrade in weapon.Upgrades)
                    if (upgrade.Location == location) return $"Buy {weapon.Name}: {upgrade.Name}";
            }
            foreach (GameShopInfo shop in GameShops)
                foreach (GameShopCategory category in shop.Categories)
                    if (category.Location == location) return $"{shop.Label}: {category.Name}";
            return "Location " + location;
        }

        public static string ItemName(long item)
        {
            int character = CharacterByItem(item);
            if (character >= 0) return CharacterNames[character];
            if (item == WeaponGiftItem) return "Weapon Gift";
            if (item == VehicleGiftItem) return "Vehicle Gift";
            if (item == CollectibleRadarItem) return "Collectible Radar";
            if (item == DriveBySmgItem) return "Drive-By SMG";
            VehicleItem vehicle = VehicleByItem(item);
            if (vehicle != null) return vehicle.Category;
            MissionInfo mission = MissionByItem(item);
            if (mission != null) return "Mission: " + mission.Name;
            TrapInfo trap = TrapByItem(item);
            if (trap != null) return "Trap: " + trap.Name;
            MoneyInfo money = MoneyByItemId(item);
            if (money != null) return money.Name;
            VehicleUnlock unlock = VehicleUnlockByItem(item);
            if (unlock != null) return "Vehicle: " + unlock.Name;
            ShopWeapon weapon = ShopWeaponByItem(item);
            if (weapon != null) return "Weapon: " + weapon.Name;
            weapon = ShopUpgradesByItem(item);
            if (weapon != null) return "Upgrades: " + weapon.Name;
            GameShopInfo shop = GameShopByItem(item);
            if (shop != null) return "Shop: " + shop.Label;
            return "Item " + item;
        }
    }
}
