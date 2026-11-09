import json
import pathlib
import sys

root = pathlib.Path(__file__).resolve().parent.parent
sys.path.insert(0, str(root / "apworld" / "gtav"))
import data

missions_json = json.loads((root / "tools" / "missions.json").read_text(encoding="utf-8-sig"))["Missions"]
points = {m["Code"]: m.get("Points", {}) for m in missions_json}


def s(text):
    return '"' + text.replace("\\", "\\\\").replace('"', '\\"') + '"'


def f(value):
    return f"{value:.2f}f"


lines = []
w = lines.append
w("namespace GameArchipelago.Core")
w("{")
w("    internal static partial class ApData")
w("    {")
w(f"        public const string Game = {s(data.GAME)};")
w(f"        public const long WeaponGiftItem = {data.ITEM_NAME_TO_ID[data.WEAPON_GIFT]};")
w(f"        public const long VehicleGiftItem = {data.ITEM_NAME_TO_ID[data.VEHICLE_GIFT]};")
w(f"        public const long DriveBySmgItem = {data.ITEM_NAME_TO_ID[data.DRIVE_BY_SMG]};")
w(f"        public const long CollectibleRadarItem = {data.ITEM_NAME_TO_ID[data.COLLECTIBLE_RADAR]};")
w(f"        public const int MaxCollectibleBonus = {data.MAX_COLLECTIBLE_BONUS};")
w(f"        public const int MaxBonus = {data.MAX_BONUS};")
w(f"        public const int MaxGoldMedals = {data.MAX_GOLD_MEDALS};")
w(f"        public const int MaxObjectives = {data.MAX_OBJECTIVES};")
w(f"        public const long GoldBase = {data.BASE_ID + 4000};")
w(f"        public const long ObjectiveBase = {data.BASE_ID + 5000};")
w("")
w("        public static readonly long[] CharacterItems =")
w("        {")
for c in data.CHARACTERS:
    w(f"            {data.ITEM_NAME_TO_ID[c]},")
w("        };")
w("")
w("        public static readonly long[] SpecialAbilityItems =")
w("        {")
for name in data.SPECIAL_ABILITIES:
    w(f"            {data.ITEM_NAME_TO_ID[name]},")
w("        };")
w("")
w("        public static readonly VehicleItem[] VehicleItems =")
w("        {")
for name in data.GUARANTEED_VEHICLES:
    w(f"            new VehicleItem({data.ITEM_NAME_TO_ID[name]}, {s(name)}),")
w("        };")
w("")
w("        public static readonly MissionInfo[] Missions =")
w("        {")
for m in data.MISSIONS:
    mask = sum(1 << data.CHARACTERS.index(t) for t in m.triggers)
    players = sum(1 << data.CHARACTERS.index(t) for t in m.players)
    loc = data.LOCATION_NAME_TO_ID[m.name]
    bonus = ", ".join(str(data.LOCATION_NAME_TO_ID[data.bonus_location(m, b)]) for b in range(1, data.MAX_BONUS + 1))
    item = data.ITEM_NAME_TO_ID[data.mission_item(m)]
    pts = points.get(m.code, {})
    coords = []
    for c in data.CHARACTERS:
        p = pts.get(c)
        coords.append(f"new MarkerPoint({f(p['X'])}, {f(p['Y'])}, {f(p['Z'])})" if p else "null")
    w(f"            new MissionInfo({s(m.code)}, {s(m.name)}, {str(m.strangers).lower()}, {mask}, {players}, {m.minutes}, {item}, {loc}, new long[] {{ {bonus} }}, {', '.join(coords)}),")
w("        };")
w("")
w("        public static readonly TrapInfo[] Traps =")
w("        {")
for trap_id, title in data.TRAPS:
    w(f"            new TrapInfo({data.ITEM_NAME_TO_ID[data.trap_item(title)]}, {s(trap_id)}, {s(title)}),")
w("        };")
w("")
w("        public static readonly MoneyInfo[] Money =")
w("        {")
for name, amount in data.MONEY:
    w(f"            new MoneyInfo({data.ITEM_NAME_TO_ID[name]}, {s(name)}, {amount}),")
w("        };")
w("")
w("        public static readonly CollectibleSet[] Collectibles =")
w("        {")
for k, c in enumerate(data.COLLECTIBLES):
    base = data.LOCATION_NAME_TO_ID[data.collectible_location(c, 1)] - 1
    free = str(c.key in data.FREE_ROAM_KEYS).lower()
    w(f"            new CollectibleSet({s(c.key)}, {s(c.name)}, {c.total}, {str(c.underwater).lower()}, {base}, {s(c.label)}, {free}),")
w("        };")
w("")
w("        public static readonly FeatLocation[] Feats =")
w("        {")
for key, name in data.LANDMARKS:
    w(f"            new FeatLocation({s(data.landmark_id(key))}, {data.LOCATION_NAME_TO_ID[data.landmark_location(name)]}, {s(data.landmark_location(name))}, FeatGroup.Landmark, 0),")
for character in data.CHARACTERS:
    for skill in data.SKILLS:
        for level in data.SKILL_LEVELS:
            w(f"            new FeatLocation({s(data.skill_id(character, skill, level))}, {data.LOCATION_NAME_TO_ID[data.skill_location(character, skill, level)]}, {s(data.skill_location(character, skill, level))}, FeatGroup.Skill, {level}),")
for activity in data.ACTIVITIES:
    w(f"            new FeatLocation({s(data.activity_id(activity))}, {data.LOCATION_NAME_TO_ID[data.activity_location(activity)]}, {s(data.activity_location(activity))}, FeatGroup.Activity, 0),")
for stars in range(1, data.MAX_ESCAPE_STARS + 1):
    w(f"            new FeatLocation({s(data.escape_id(stars))}, {data.LOCATION_NAME_TO_ID[data.escape_location(stars)]}, {s(data.escape_location(stars))}, FeatGroup.Escape, {stars}),")
w("        };")
w("")
w("        public static readonly VehicleUnlock[] VehicleUnlocks =")
w("        {")
for vehicle in data.VEHICLES:
    w(f"            new VehicleUnlock({data.ITEM_NAME_TO_ID[data.vehicle_item(vehicle)]}, {s(vehicle.model)}, {s(vehicle.name)}, {str(vehicle.water).lower()}),")
w("        };")
w("")
w("        public static readonly ShopWeapon[] ShopWeapons =")
w("        {")
for weapon in data.WEAPONS:
    upgrades = data.UPGRADES.get(weapon.id, ())
    up_item = data.ITEM_NAME_TO_ID[data.upgrades_item(weapon)] if upgrades else 0
    parts = ", ".join(f"new ShopUpgrade({s(u.component)}, {s(u.name)}, {data.LOCATION_NAME_TO_ID[data.upgrade_location(weapon, u)]})" for u in upgrades)
    w(f"            new ShopWeapon({s(weapon.id)}, {s(weapon.name)}, {data.ITEM_NAME_TO_ID[data.weapon_item(weapon)]}, {data.LOCATION_NAME_TO_ID[data.weapon_location(weapon)]}, {up_item}, new ShopUpgrade[] {{ {parts} }}),")
w("        };")
w("")
w("        public static readonly GameShopInfo[] GameShops =")
w("        {")
for shop in data.GAME_SHOPS:
    cats = ", ".join(f"new GameShopCategory({s(c)}, {data.LOCATION_NAME_TO_ID[data.game_shop_location(shop, c)]})" for c in shop.categories)
    w(f"            new GameShopInfo({s(shop.key)}, {s(shop.label)}, {data.ITEM_NAME_TO_ID[data.game_shop_item(shop)]}, new GameShopCategory[] {{ {cats} }}),")
w("        };")
w("    }")
w("}")
out = root / "src" / "Core" / "ApData.Generated.cs"
out.write_text("\n".join(lines) + "\n", encoding="utf-8")
print("wrote", out, len(data.MISSIONS), "missions")

shop = []
v = shop.append
v("namespace GameEnvironment.Core")
v("{")
v("    internal static partial class ShopCatalog")
v("    {")
v("        public static readonly ShopEntry[] Weapons =")
v("        {")
for weapon in data.WEAPONS:
    parts = ", ".join(f"new ShopPart({s(u.component)}, {s(u.name)}, {u.price})" for u in data.UPGRADES.get(weapon.id, ()))
    v(f"            new ShopEntry({s(weapon.id)}, {s(weapon.name)}, {s(weapon.category)}, {weapon.price}, new ShopPart[] {{ {parts} }}),")
v("        };")
v("    }")
v("}")
out = root.parent / "GameEnvironment" / "src" / "Core" / "ShopCatalog.Generated.cs"
out.write_text("\n".join(shop) + "\n", encoding="utf-8")
print("wrote", out, len(data.WEAPONS), "weapons")
