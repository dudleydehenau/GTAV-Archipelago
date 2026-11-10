import re
from dataclasses import dataclass

from Options import Choice, DeathLink, DefaultOnToggle, Toggle, OptionGroup, OptionSet, PerGameCommonOptions, Range

from .data import ALTERNATE_APPROACHES, MISSION_BY_CODE, MISSIONS, TRAPS, UNSTABLE_PREPS


class Goal(Choice):
    """
    What you need to do to finish your game. When it is done, the server can release your remaining items to the
    others (depending on the room's release setting).

    - **finale**: pass one of the three final missions (Something Sensible, The Time's Come or The Third Way).
    - **the_third_way**: pass The Third Way, the ending where the three of them survive.
    - **mission_percentage**: pass a share of all the missions of your world (set the share with Mission Goal
      Percentage, for example 70 % of 30 missions = 21 missions, any of them).
    - **one_mission**: pass a single mission, the one you pick in Goal Mission (or a random one).
    - **one_collectible**: pick up one random collectible (a spaceship part, a letter scrap, a nuclear waste barrel
      or a submarine part). The client tells you which one and can route you to it. It can be very short!
    - **one_check**: one random location of your world (a mission, a medal counter, a collectible milestone...).
      The client tells you which one. Silly and random on purpose.
    - **check_percentage**: check a share of all the locations of your world, whatever they are (set the share with
      Goal Check Percentage).
    - **free_roam**: check a number of free-roam locations (landmarks, stunt jumps, bridges, knife flights, skills,
      police escapes, mosaics, peyote plants, Epsilon tracts), set with Goal Free-Roam Checks.
    """
    display_name = "Goal"
    option_finale = 0
    option_the_third_way = 1
    option_mission_percentage = 2
    option_one_mission = 3
    option_one_collectible = 4
    option_one_check = 5
    option_check_percentage = 6
    option_free_roam = 7
    alias_missions = 2
    alias_random_mission = 3
    default = 0


def _slug(text: str) -> str:
    return re.sub(r"[^a-z0-9]+", "_", text.lower()).strip("_")


GOAL_MISSION_CODES = [m.code for m in MISSIONS if m.code not in UNSTABLE_PREPS and m.code not in ALTERNATE_APPROACHES]
_goal_mission_attributes = {
    "__doc__": """
    With the one_mission goal: the mission you need to pass. "surprise" picks one that needs a character you do not
    start with. The mission you pick is always in your world, even if it is a Strangers and Freaks mission and they
    are off.
    """,
    "__module__": __name__,
    "display_name": "Goal Mission",
    "auto_display_name": True,
    "option_surprise": 0,
    "default": 0,
}
for _index, _code in enumerate(GOAL_MISSION_CODES):
    _goal_mission_attributes["option_" + _slug(MISSION_BY_CODE[_code].name)] = _index + 1
for _alias, _code in (("the_big_score_subtle", "FH2A"), ("the_big_score_obvious", "FH2A"),
                      ("the_merryweather_heist_offshore", "DH2A"), ("the_merryweather_heist_freighter", "DH2A"),
                      ("the_bureau_raid_fire_crew", "AH3a"), ("the_bureau_raid_roof_entry", "AH3a")):
    _goal_mission_attributes["alias_" + _alias] = GOAL_MISSION_CODES.index(_code) + 1
GoalMission = type("GoalMission", (Choice,), _goal_mission_attributes)


class MissionCount(Range):
    """
    How many missions are in your world (67 story missions, plus 44 Strangers and Freaks if they are on; the
    free-roam preparations that steal a vehicle or an item are never used, they do not work as replays). The missions are
    picked at random, the ones your goal needs are always kept. A story mission takes about 10 minutes (20 for a
    heist), a Strangers and Freaks mission about 6. The spoiler log and the client show the estimated time to your
    goal and for every location.
    """
    display_name = "Mission Count"
    range_start = 10
    range_end = 111
    default = 30


class MissionGoalPercentage(Range):
    """
    With the mission_percentage goal, the share of the missions of your world you need to pass (any of them).
    """
    display_name = "Mission Goal Percentage"
    range_start = 10
    range_end = 100
    default = 70


class GoalCheckPercentage(Range):
    """
    With the check_percentage goal, the share of all the locations of your world you need to check.
    """
    display_name = "Goal Check Percentage"
    range_start = 10
    range_end = 100
    default = 60


class GoalFreeRoamChecks(Range):
    """
    With the free_roam goal, how many free-roam locations you need to check. Capped at the number of free-roam
    locations in your world.
    """
    display_name = "Goal Free-Roam Checks"
    range_start = 5
    range_end = 500
    default = 50


class StartingCharacter(Choice):
    """
    The character you start with. The two others are locked and are items in the multiworld.
    Michael, Franklin and Trevor each trigger different missions.
    """
    display_name = "Starting Character"
    option_michael = 0
    option_franklin = 1
    option_trevor = 2
    default = "random"


class StartPosition(Choice):
    """
    Where a new game puts you.
    home: the house of your starting character.
    city: Legion Square, in the middle of downtown Los Santos.
    anywhere: a random place anywhere on the map.
    """
    display_name = "Start Position"
    option_home = 0
    option_city = 1
    option_anywhere = 2
    default = 2


class StartingMissions(Range):
    """
    How many missions of your starting character are unlocked from the start.
    """
    display_name = "Starting Missions"
    range_start = 1
    range_end = 10
    default = 3


class StrangersAndFreaks(DefaultOnToggle):
    """
    Adds the replayable Strangers and Freaks missions (Barry, Epsilon, Dom, Mary-Ann, Hao, Cletus, Josh, the
    Minutemen, Nigel, Beverly, the rampages, the Last One and Tonya) as items and locations.
    """
    display_name = "Strangers and Freaks"


class ChecksPerMission(Range):
    """
    How many locations each mission is worth. With 2 or 3, passing a mission also sends its bonus locations.
    Raise it to get more room for items without adding new tasks.
    """
    display_name = "Checks per Mission"
    range_start = 1
    range_end = 3
    default = 1


class GoldMedalChecks(Range):
    """
    Number of "Gold Medal" locations. Gold Medal N is sent when you have finished N different missions with a gold
    medal (all optional objectives passed). Capped at three quarters of the missions in your world.
    """
    display_name = "Gold Medal Checks"
    range_start = 0
    range_end = 92
    default = 5


class ObjectiveChecks(Range):
    """
    Number of "Objective" locations. Objective N is sent when you have passed N different optional mission
    objectives (accuracy, time, headshots...), even without a gold medal. Capped at one and a half per mission in
    your world.
    """
    display_name = "Objective Checks"
    range_start = 0
    range_end = 184
    default = 20


class SpaceshipPartsPerCheck(Range):
    """
    How many spaceship parts (50 in the world) you need to pick up for each check. 0 removes them from the
    locations. 1 makes every part a check, 5 makes 10 checks ("at 5 collected", "at 10 collected"...).
    """
    display_name = "Spaceship Parts per Check"
    range_start = 0
    range_end = 50
    default = 1


class LetterScrapsPerCheck(Range):
    """
    How many letter scraps (50 in the world) you need to pick up for each check. 0 removes them from the locations.
    """
    display_name = "Letter Scraps per Check"
    range_start = 0
    range_end = 50
    default = 1


class NuclearWastePerCheck(Range):
    """
    How many nuclear waste barrels (30, underwater) you need to pick up for each check. 0 removes them from the
    locations. With Guaranteed Vehicles on, they need the Underwater Vehicle item in logic.
    """
    display_name = "Nuclear Waste per Check"
    range_start = 0
    range_end = 30
    default = 1


class SubmarinePartsPerCheck(Range):
    """
    How many submarine parts (30, underwater) you need to pick up for each check. 0 removes them from the
    locations. With Guaranteed Vehicles on, they need the Underwater Vehicle item in logic.
    """
    display_name = "Submarine Parts per Check"
    range_start = 0
    range_end = 30
    default = 1


class MonkeyMosaicsPerCheck(Range):
    """
    How many monkey mosaics (50 street art pieces on walls all over the map) you need to find for each check. The
    object is placed on the ground in front of the mosaic. 0 removes them from the locations.
    """
    display_name = "Monkey Mosaics per Check"
    range_start = 0
    range_end = 50
    default = 1


class PeyotePlantsPerCheck(Range):
    """
    How many peyote plants (27, six of them underwater) you need to pick up for each check. Picking one up does not
    turn you into an animal. 0 removes them from the locations. With Guaranteed Vehicles on, they need the
    Underwater Vehicle item in logic.
    """
    display_name = "Peyote Plants per Check"
    range_start = 0
    range_end = 27
    default = 1


class EpsilonTractsPerCheck(Range):
    """
    How many Epsilon tracts (10, some of them very high or underwater) you need to pick up for each check. 0
    removes them from the locations.
    """
    display_name = "Epsilon Tracts per Check"
    range_start = 0
    range_end = 10
    default = 1


class StuntJumpsPerCheck(Range):
    """
    How many of the 50 unique stunt jumps you need to complete for each check. Any vehicle works. 0 removes them from
    the locations.
    """
    display_name = "Stunt Jumps per Check"
    range_start = 0
    range_end = 50
    default = 1


class BridgesPerCheck(Range):
    """
    How many of the 50 "Under the Bridge" flights you need for each check (fly a plane or a helicopter under a
    bridge). With Guaranteed Vehicles on, they need the Helicopter item in logic. 0 removes them from the locations.
    """
    display_name = "Under the Bridge per Check"
    range_start = 0
    range_end = 50
    default = 1


class KnifeFlightsPerCheck(Range):
    """
    How many of the 15 knife flights (fly between two buildings on your side) you need for each check. With
    Guaranteed Vehicles on, they need the Helicopter item in logic. 0 removes them from the locations.
    """
    display_name = "Knife Flights per Check"
    range_start = 0
    range_end = 15
    default = 1


class Landmarks(DefaultOnToggle):
    """
    Adds 44 landmarks to visit (Mount Chiliad summit, the Maze Bank Tower roof, the Vinewood sign, Fort Zancudo,
    Humane Labs...). Each one is a check. The ones left are shown with a purple marker on the map.
    """
    display_name = "Landmarks"


class SkillChecks(Choice):
    """
    The skills of the three characters (stamina, shooting, strength, stealth, flying, driving, lung capacity) as
    checks. every_10 makes a check at 10 %, 20 %... 100 % of each skill (210 checks), every_20 at 20 %, 40 %...,
    every_50 at 50 % and 100 %, only_100 at 100 % only. A character's skills need that character in logic.
    """
    display_name = "Skill Checks"
    option_off = 0
    option_every_10 = 10
    option_every_20 = 20
    option_every_50 = 50
    option_only_100 = 100
    default = 10


class PoliceEscapes(DefaultOnToggle):
    """
    Adds five checks for losing the police: escape a 1-star, 2-star... 5-star wanted level (the stars must flash
    and go away by themselves, being busted or wasted does not count).
    """
    display_name = "Police Escapes"


class Activities(DefaultOnToggle):
    """
    The activities of Los Santos as checks, each one when the game counts it as done for 100 %: the six off-road
    races, the street and sea races, the three triathlons, the twelve Flight School lessons, the stunt plane time
    trials, the six shooting range categories, tennis, golf, darts, yoga, base jumping and skydiving, and smaller
    things (a movie, the cable car, a car wash, clothes, a haircut, a tattoo, modifying a car, TV, holding up a
    store). Every activity is opened from the start.
    """
    display_name = "Activities"


class TrevorJobs(Toggle):
    """
    Not available in this version, it has no effect. Trevor's bail bonds, trafficking runs and hunting are started by
    story scripts that the empty world does not run, so they would be checks you cannot do.
    """
    display_name = "Trevor's Jobs"


class WeaponShop(Choice):
    """
    How you get your weapons.
    gifts: like before, Weapon Gifts give random weapons and Ammu-Nation sells everything.
    unlocks: each weapon is an item ("Weapon: Pistol") that lets you buy it at Ammu-Nation. Buying it is the only way
    to get it. No Weapon Gifts.
    shopsanity: the same, and buying each weapon for the first time is also a check ("Buy Pistol"). Buying it again
    after losing it gives nothing.
    Your starting weapon and the Drive-By SMG stay free gifts.
    """
    display_name = "Weapon Shop"
    option_gifts = 0
    option_unlocks = 1
    option_shopsanity = 2
    default = 2


class WeaponUpgrades(Choice):
    """
    The weapon upgrades sold at Ammu-Nation (extended clips, suppressors, scopes, grips, special rounds...).
    off: they can all be bought.
    unlocks: each weapon has an upgrade item ("Upgrades: Pistol") that lets you buy its upgrades.
    shopsanity: the same, and buying each upgrade is a check (267 checks).
    """
    display_name = "Weapon Upgrades"
    option_off = 0
    option_unlocks = 1
    option_shopsanity = 2
    default = 0


class GameShops(Toggle):
    """
    Shopsanity for the game's own shops. The first purchase of each category is a check:
    Barber (haircut, beard), Clothes Store (tops, pants, shoes, hats, glasses, outfits, masks),
    Tattoo Parlor (one per body zone) and Los Santos Customs (engine, brakes, wheels, respray...).
    Each kind of shop stays closed until you get its item (for example "Shop: Los Santos Customs").
    """
    display_name = "Game Shops"
    default = 1


class WeaponPriceMultiplier(Range):
    """
    Prices of weapons, upgrades and ammo at Ammu-Nation, in percent. Raise it to make money matter.
    """
    display_name = "Weapon Price Multiplier"
    range_start = 10
    range_end = 500
    default = 100


class ChecksPerCollectible(Range):
    """
    How many locations each collectible, stunt jump, bridge and knife flight check is worth. Finding things on such a
    big map is slow, so each find can count for more.
    """
    display_name = "Checks per Collectible"
    range_start = 1
    range_end = 3
    default = 2


class CollectibleRadar(Choice):
    """
    The "Route to the nearest collectible" command in the F6 menu.
    start: you have it from the start.
    item: it is an item ("Collectible Radar") somewhere in the multiworld.
    """
    display_name = "Collectible Radar"
    option_start = 0
    option_item = 1
    default = 1


class SpecialAbilities(Choice):
    """
    Michael's bullet time, Franklin's driving focus and Trevor's rage.
    start: the three special abilities work from the start.
    item: each one is an item ("Special Ability: Michael", "Special Ability: Franklin", "Special Ability: Trevor")
    somewhere in the multiworld, and stays locked until you get it.
    """
    display_name = "Special Abilities"
    option_start = 0
    option_item = 1
    default = 1


class GuaranteedVehicles(DefaultOnToggle):
    """
    Adds six vehicle items to the pool: a sports car, a motorcycle, a helicopter, an underwater vehicle, a boat and
    the Scramjet. The helicopter and the underwater vehicle are placed early. With this on, the underwater
    collectibles need the underwater vehicle in logic. Only used with Vehicle Unlocks off.
    """
    display_name = "Guaranteed Vehicles"


class VehicleUnlocks(DefaultOnToggle):
    """
    Every vehicle of the list (62: Deluxo, Oppressor Mk II, Stromberg, tanks, helicopters, planes, the gold Luxor, the
    airliner, boats, submarines...) is its own item ("Vehicle: Imponte Deluxo"), received once and kept in your
    garage. This replaces the Vehicle Gifts and the six guaranteed vehicles: the bridges then need any plane or
    helicopter in logic, the underwater checks any submarine (one of each is placed early).
    """
    display_name = "Vehicle Unlocks"


class EarlyVehicles(DefaultOnToggle):
    """
    With Vehicle Unlocks on, a small plane (Dodo, Mallard, Duster, Cuban 800, Mammatus, Velum or Howard, good for
    knife flights and bridges) and a submarine are placed early in the multiworld, so you get them soon in the run.
    """
    display_name = "Early Vehicles"


class MissionTraps(DefaultOnToggle):
    """
    Traps you receive during a mission can trigger in the mission (only the ones that cannot break it). Gifts received
    during a mission are always given right away.
    """
    display_name = "Traps in Missions"


class StartingDriveByWeapon(DefaultOnToggle):
    """
    Start with a Micro SMG and plenty of ammo for the three characters: it can be fired from a car, which many
    missions need (chases, drive-bys). Off: you depend on your Weapon Gifts.
    """
    display_name = "Starting Drive-By Weapon"


class MissionWeapons(Toggle):
    """
    Off by default: you fight with the weapons you received, and a mission you cannot do yet waits for more Weapon
    Gifts. On: each mission starts with a basic kit (pistol, SMG, carbine rifle, shotgun, sniper rifle and a few
    grenades, sticky bombs for some assassinations), taken back after the mission.
    """
    display_name = "Mission Weapons"


class PostGoalChaos(DefaultOnToggle):
    """
    After your goal, keep playing for fun: passed missions come back at random on the map, and the checks of the other
    players trigger random traps and gifts in your game. Nothing is sent to the multiworld any more.
    """
    display_name = "Post-Goal Chaos"


class TrapPercentage(Range):
    """
    Percentage of the filler items replaced by traps.
    """
    display_name = "Trap Percentage"
    range_start = 0
    range_end = 100
    default = 30


class EnabledTraps(OptionSet):
    """
    The traps that can be placed in your world. Use the names in the list below.
    "Irresistible" can show nightclub dancers: remove it if you stream.
    """
    display_name = "Enabled Traps"
    valid_keys = frozenset(trap_id for trap_id, _ in TRAPS)
    default = frozenset(trap_id for trap_id, _ in TRAPS)


class MoneyWeight(Range):
    """
    Weight of money among the non-trap filler items.
    """
    display_name = "Money Weight"
    range_start = 0
    range_end = 10
    default = 3


class WeaponWeight(Range):
    """
    Weight of Weapon Gifts among the non-trap filler items (only with Weapon Shop: gifts). Each one gives a random weapon (online weapons included)
    from the list in the client's weapons.json, with its ammo, to the three characters. Missions need weapons, so
    this is the most common filler by default.
    """
    display_name = "Weapon Weight"
    range_start = 0
    range_end = 10
    default = 6


class VehicleWeight(Range):
    """
    Weight of Vehicle Gifts among the non-trap filler items. Each one gives a random vehicle from the list in the
    client's vehicles.json (Deluxo, Oppressor Mk II, Stromberg, helicopters, submarine...). Vehicles are kept in your
    garage and can be delivered next to you from the F6 menu.
    """
    display_name = "Vehicle Weight"
    range_start = 0
    range_end = 10
    default = 2


class MoneyMultiplier(Range):
    """
    Multiplies every money item, in percent. 100 gives $5,000, $20,000 and $100,000.
    """
    display_name = "Money Multiplier"
    range_start = 10
    range_end = 500
    default = 100


@dataclass
class GTAVOptions(PerGameCommonOptions):
    goal: Goal
    goal_mission: GoalMission
    mission_goal_percentage: MissionGoalPercentage
    goal_check_percentage: GoalCheckPercentage
    goal_free_roam_checks: GoalFreeRoamChecks
    mission_count: MissionCount
    starting_character: StartingCharacter
    start_position: StartPosition
    starting_missions: StartingMissions
    strangers_and_freaks: StrangersAndFreaks
    checks_per_mission: ChecksPerMission
    gold_medal_checks: GoldMedalChecks
    objective_checks: ObjectiveChecks
    spaceship_parts_per_check: SpaceshipPartsPerCheck
    letter_scraps_per_check: LetterScrapsPerCheck
    nuclear_waste_per_check: NuclearWastePerCheck
    submarine_parts_per_check: SubmarinePartsPerCheck
    monkey_mosaics_per_check: MonkeyMosaicsPerCheck
    peyote_plants_per_check: PeyotePlantsPerCheck
    epsilon_tracts_per_check: EpsilonTractsPerCheck
    stunt_jumps_per_check: StuntJumpsPerCheck
    bridges_per_check: BridgesPerCheck
    knife_flights_per_check: KnifeFlightsPerCheck
    landmarks: Landmarks
    skill_checks: SkillChecks
    police_escapes: PoliceEscapes
    activities: Activities
    trevor_jobs: TrevorJobs
    weapon_shop: WeaponShop
    weapon_upgrades: WeaponUpgrades
    weapon_price_multiplier: WeaponPriceMultiplier
    game_shops: GameShops
    checks_per_collectible: ChecksPerCollectible
    collectible_radar: CollectibleRadar
    special_abilities: SpecialAbilities
    guaranteed_vehicles: GuaranteedVehicles
    vehicle_unlocks: VehicleUnlocks
    early_vehicles: EarlyVehicles
    mission_traps: MissionTraps
    mission_weapons: MissionWeapons
    starting_drive_by_weapon: StartingDriveByWeapon
    post_goal_chaos: PostGoalChaos
    trap_percentage: TrapPercentage
    enabled_traps: EnabledTraps
    money_weight: MoneyWeight
    weapon_weight: WeaponWeight
    vehicle_weight: VehicleWeight
    money_multiplier: MoneyMultiplier
    death_link: DeathLink


option_groups = [
    OptionGroup("Goal and Start", [Goal, GoalMission, MissionGoalPercentage, GoalCheckPercentage, GoalFreeRoamChecks,
                                   MissionCount, StartingCharacter, StartPosition, StartingMissions]),
    OptionGroup("Locations", [StrangersAndFreaks, ChecksPerMission, GoldMedalChecks, ObjectiveChecks,
                              SpaceshipPartsPerCheck, LetterScrapsPerCheck, NuclearWastePerCheck,
                              SubmarinePartsPerCheck, MonkeyMosaicsPerCheck, PeyotePlantsPerCheck,
                              EpsilonTractsPerCheck, ChecksPerCollectible]),
    OptionGroup("Free Roam", [StuntJumpsPerCheck, BridgesPerCheck, KnifeFlightsPerCheck, Landmarks, SkillChecks,
                              PoliceEscapes, Activities, TrevorJobs]),
    OptionGroup("Shops", [WeaponShop, WeaponUpgrades, WeaponPriceMultiplier, GameShops]),
    OptionGroup("Items", [CollectibleRadar, SpecialAbilities, VehicleUnlocks, EarlyVehicles, GuaranteedVehicles, TrapPercentage, EnabledTraps, MoneyWeight,
                          WeaponWeight, VehicleWeight, MoneyMultiplier]),
    OptionGroup("Gameplay", [StartingDriveByWeapon, MissionWeapons, MissionTraps, PostGoalChaos]),
]

option_presets = {
    "Short": {
        "goal": Goal.option_mission_percentage,
        "mission_goal_percentage": 60,
        "mission_count": 20,
        "strangers_and_freaks": False,
        "gold_medal_checks": 0,
        "objective_checks": 10,
        "spaceship_parts_per_check": 5,
        "letter_scraps_per_check": 0,
        "nuclear_waste_per_check": 0,
        "submarine_parts_per_check": 0,
        "monkey_mosaics_per_check": 0,
        "peyote_plants_per_check": 0,
        "epsilon_tracts_per_check": 0,
        "stunt_jumps_per_check": 10,
        "bridges_per_check": 0,
        "knife_flights_per_check": 0,
        "landmarks": True,
        "skill_checks": SkillChecks.option_only_100,
        "police_escapes": True,
        "checks_per_collectible": 3,
    },
    "Ten Hours": {
        "goal": Goal.option_finale,
        "mission_count": 80,
        "strangers_and_freaks": True,
        "gold_medal_checks": 10,
        "objective_checks": 40,
        "spaceship_parts_per_check": 2,
        "letter_scraps_per_check": 2,
        "nuclear_waste_per_check": 3,
        "submarine_parts_per_check": 3,
        "monkey_mosaics_per_check": 2,
        "peyote_plants_per_check": 3,
        "epsilon_tracts_per_check": 2,
        "stunt_jumps_per_check": 2,
        "bridges_per_check": 2,
        "knife_flights_per_check": 3,
        "skill_checks": SkillChecks.option_every_20,
        "checks_per_collectible": 2,
    },
    "One Mission": {
        "goal": Goal.option_one_mission,
        "mission_count": 50,
        "gold_medal_checks": 5,
        "objective_checks": 20,
        "spaceship_parts_per_check": 5,
        "letter_scraps_per_check": 5,
        "nuclear_waste_per_check": 5,
        "submarine_parts_per_check": 5,
        "monkey_mosaics_per_check": 5,
        "peyote_plants_per_check": 9,
        "epsilon_tracts_per_check": 5,
        "stunt_jumps_per_check": 5,
        "bridges_per_check": 5,
        "knife_flights_per_check": 5,
        "skill_checks": SkillChecks.option_every_50,
    },
    "Everything": {
        "mission_count": 111,
        "strangers_and_freaks": True,
        "checks_per_mission": 2,
        "gold_medal_checks": 40,
        "objective_checks": 120,
        "spaceship_parts_per_check": 1,
        "letter_scraps_per_check": 1,
        "nuclear_waste_per_check": 1,
        "submarine_parts_per_check": 1,
        "monkey_mosaics_per_check": 1,
        "peyote_plants_per_check": 1,
        "epsilon_tracts_per_check": 1,
        "stunt_jumps_per_check": 1,
        "bridges_per_check": 1,
        "knife_flights_per_check": 1,
        "landmarks": True,
        "skill_checks": SkillChecks.option_every_10,
        "police_escapes": True,
        "weapon_shop": WeaponShop.option_shopsanity,
        "weapon_upgrades": WeaponUpgrades.option_shopsanity,
        "checks_per_collectible": 1,
    },
    "Free Roam": {
        "goal": Goal.option_free_roam,
        "goal_free_roam_checks": 120,
        "mission_count": 20,
        "strangers_and_freaks": False,
        "gold_medal_checks": 0,
        "objective_checks": 0,
        "stunt_jumps_per_check": 1,
        "bridges_per_check": 1,
        "knife_flights_per_check": 1,
        "landmarks": True,
        "skill_checks": SkillChecks.option_every_20,
        "police_escapes": True,
        "checks_per_collectible": 1,
    },
    "Stream Safe": {
        "enabled_traps": [trap_id for trap_id, _ in TRAPS if trap_id != "Irresistible"],
    },
}
