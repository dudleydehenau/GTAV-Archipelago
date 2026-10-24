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
        public CollectibleSet(string key, string name, int total, bool underwater, long baseLocation)
        {
            Key = key;
            Name = name;
            Total = total;
            Underwater = underwater;
            BaseLocation = baseLocation;
        }

        public string Key { get; }
        public string Name { get; }
        public int Total { get; }
        public bool Underwater { get; }
        public long BaseLocation { get; }

        public long LocationFor(int count) => BaseLocation + count;

        public long LocationFor(int count, int bonus) => BaseLocation + bonus * 1000L + count;
    }

    internal static partial class ApData
    {
        public static readonly string[] CharacterNames = { "Michael", "Franklin", "Trevor" };

        private static Dictionary<string, MissionInfo> _missionsByCode;
        private static Dictionary<long, MissionInfo> _missionsByItem;
        private static Dictionary<long, TrapInfo> _trapsByItem;
        private static Dictionary<long, MoneyInfo> _moneyByItem;

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
                    return $"{set.Name}: at {location - set.BaseLocation} collected";
                for (int b = 1; b <= MaxCollectibleBonus; b++)
                {
                    long start = set.BaseLocation + b * 1000L;
                    if (location > start && location <= start + set.Total)
                        return $"{set.Name}: at {location - start} collected (Bonus {b})";
                }
            }
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
            return "Item " + item;
        }
    }
}
