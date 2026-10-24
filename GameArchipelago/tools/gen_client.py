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
    w(f"            new CollectibleSet({s(c.key)}, {s(c.name)}, {c.total}, {str(c.underwater).lower()}, {base}),")
w("        };")
w("    }")
w("}")
out = root / "src" / "Core" / "ApData.Generated.cs"
out.write_text("\n".join(lines) + "\n", encoding="utf-8")
print("wrote", out, len(data.MISSIONS), "missions")
