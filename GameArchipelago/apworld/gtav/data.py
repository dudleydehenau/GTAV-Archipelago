from typing import NamedTuple

GAME = "Grand Theft Auto V"
BASE_ID = 7_354_000

MICHAEL = "Michael"
FRANKLIN = "Franklin"
TREVOR = "Trevor"
CHARACTERS = (MICHAEL, FRANKLIN, TREVOR)


class Mission(NamedTuple):
    code: str
    name: str
    triggers: tuple
    strangers: bool
    players: tuple
    minutes: int


M, F, T = MICHAEL, FRANKLIN, TREVOR

STORY = (
    ("ARM1", "Franklin and Lamar", (F,)),
    ("ARM2", "Repossession", (F,)),
    ("ARM3", "Complications", (F,)),
    ("FRA0", "Chop", (F,)),
    ("FAM1", "Father/Son", (M,)),
    ("FAM3", "Marriage Counseling", (M, F)),
    ("FAM2", "Daddy's Little Girl", (M,)),
    ("LS1", "Friend Request", (M,)),
    ("JH1", "Casing the Jewel Store", (M,)),
    ("JHP1A", "Carbine Rifles", (M,)),
    ("JHP1B", "Bugstars Equipment", (M,)),
    ("JHP2A", "BZ Gas Grenades", (M,)),
    ("JH2", "The Jewel Store Job", (M,)),
    ("LM1", "The Long Stretch", (F,)),
    ("TRV1", "Mr. Philips", (T,)),
    ("CHN1", "Trevor Philips Industries", (T,)),
    ("TRV2", "Nervous Ron", (T,)),
    ("CHN2", "Crystal Maze", (T,)),
    ("TRV3", "Friends Reunited", (T,)),
    ("FAM4", "Fame or Shame", (M, T)),
    ("FIB1", "Dead Man Walking", (M,)),
    ("FIB2", "Three's Company", (M, F)),
    ("FRA1", "Hood Safari", (F, T)),
    ("DH1", "Scouting the Port", (T,)),
    ("DHP1", "Minisub", (M, F, T)),
    ("DHP2B", "Cargobob", (T,)),
    ("DH2A", "The Merryweather Heist (Offshore)", (M, F, T)),
    ("DH2B", "The Merryweather Heist (Freighter)", (M, F, T)),
    ("FB4P1", "Blitz Play Prep: Garbage Truck", (M, F, T)),
    ("FB4P2", "Blitz Play Prep: Tow Truck", (M, F, T)),
    ("FB4P4", "Blitz Play Prep: Masks", (M, F, T)),
    ("FB4P5", "Blitz Play Prep: Boiler Suits", (M, F, T)),
    ("FIB4", "Blitz Play", (M, F, T)),
    ("CAR1", "I Fought the Law...", (M, F, T)),
    ("CAR2", "Eye in the Sky", (F, T)),
    ("SOL1", "Mr. Richards", (M, F)),
    ("MAR1", "Caida Libre", (M, T)),
    ("CAR3", "Deep Inside", (F,)),
    ("CAR4", "Pack Man", (F, T)),
    ("FAM5", "Did Somebody Say Yoga?", (M,)),
    ("FIB3", "By the Book", (T,)),
    ("FIB5", "Monkey Business", (M, F, T)),
    ("TRV4", "Hang Ten", (T,)),
    ("FH1", "Surveying the Score", (M,)),
    ("MIC1", "Bury the Hatchet", (M, T)),
    ("MIC2", "Fresh Meat", (F,)),
    ("SOL2", "The Ballad of Rocco", (M,)),
    ("AH1", "Cleaning out the Bureau", (M,)),
    ("EXL1", "Minor Turbulence", (M, F, T)),
    ("EXL2", "Predator", (M, F)),
    ("EXL3", "Derailed", (M, T)),
    ("RH1", "Paleto Score Setup", (M, T)),
    ("RHP1", "Military Hardware", (M, T)),
    ("RH2", "The Paleto Score", (M, F, T)),
    ("FAM6", "Reuniting the Family", (M,)),
    ("AH2", "Architect's Plans", (F,)),
    ("AHP1", "The Bureau Raid Prep: Fire Truck", (M, F, T)),
    ("AH3a", "The Bureau Raid (Fire Crew)", (M,)),
    ("AH3b", "The Bureau Raid (Roof Entry)", (M,)),
    ("MIC3", "The Wrap Up", (M,)),
    ("FRA2", "Lamar Down", (M, F, T)),
    ("MIC4", "Meltdown", (M,)),
    ("SOL3", "Legal Trouble", (T,)),
    ("FHPRA", "The Big Score Prep: Getaway Vehicle", (M,)),
    ("FHPRB", "The Big Score Prep: Driller", (M,)),
    ("FHPC1", "The Big Score Prep: Gauntlet (Pillbox Hill)", (M,)),
    ("FHPC2", "The Big Score Prep: Gauntlet (Rockford Hills)", (M,)),
    ("FHPC3", "The Big Score Prep: Gauntlet (Mission Row)", (M,)),
    ("FHPD", "The Big Score Prep: Train", (M,)),
    ("FH2A", "The Big Score (Subtle)", (M,)),
    ("FH2B", "The Big Score (Obvious)", (M,)),
    ("FINA", "Something Sensible", (M, F, T)),
    ("FINB", "The Time's Come", (M, F, T)),
    ("FINC1", "The Third Way", (M, F, T)),
    ("ASS1", "The Hotel Assassination", (F,)),
    ("ASS2", "The Multi Target Assassination", (F,)),
    ("ASS3", "The Vice Assassination", (F,)),
    ("ASS4", "The Bus Assassination", (F,)),
    ("ASS5", "The Construction Assassination", (F,)),
)

STRANGERS = (
    ("RC_BA1", "Grass Roots - Michael", (M,)),
    ("RC_BA2", "Grass Roots - Trevor", (T,)),
    ("RC_BA3A", "Grass Roots - The Pickup", (F,)),
    ("RC_BA3C", "Grass Roots - The Drag", (F,)),
    ("RC_DRE1", "Dreyfuss", (F,)),
    ("RC_EPS4", "Chasing the Truth", (M,)),
    ("RC_EPS6", "Delivering the Truth", (M,)),
    ("RC_EPS8", "Unknowing the Truth", (M,)),
    ("RC_EXT1", "Risk Assessment", (F,)),
    ("RC_EXT2", "Liquidity Risk", (F,)),
    ("RC_EXT3", "Targeted Risk", (F,)),
    ("RC_EXT4", "Uncalculated Risk", (F,)),
    ("RC_FAN1", "Exercising Demons - Michael", (M,)),
    ("RC_FAN2", "Exercising Demons - Trevor", (T,)),
    ("RC_FAN3", "Exercising Demons - Franklin", (F,)),
    ("RC_HAO1", "Shift Work", (F,)),
    ("RC_HUN1", "Target Practice", (T,)),
    ("RC_HUN2", "Fair Game", (T,)),
    ("RC_JOS2", "Closing the Deal", (T,)),
    ("RC_JOS3", "Surreal Estate", (T,)),
    ("RC_JOS4", "Breach of Contract", (T,)),
    ("RC_MIN1", "The Civil Border Patrol", (T,)),
    ("RC_MIN2", "An American Welcome", (T,)),
    ("RC_MIN3", "Minute Man Blues", (T,)),
    ("RC_NI1A", "Vinewood Souvenirs 1", (T,)),
    ("RC_NI1B", "Vinewood Souvenirs 2", (T,)),
    ("RC_NI1C", "Vinewood Souvenirs 3", (T,)),
    ("RC_NI1D", "Vinewood Souvenirs 4", (T,)),
    ("RC_NI2", "Vinewood Souvenirs 5", (T,)),
    ("RC_NI3", "Vinewood Souvenirs - The Last Act", (T,)),
    ("RC_PA1", "Paparazzo", (F,)),
    ("RC_PA2", "Paparazzo - The Sex Tape", (F,)),
    ("RC_PA3A", "Paparazzo 3A", (F,)),
    ("RC_PA3B", "Paparazzo 3B", (F,)),
    ("RC_PA4", "Paparazzo - The Partner", (F,)),
    ("RC_RAM1", "Rampage 1", (T,)),
    ("RC_RAM2", "Rampage 2", (T,)),
    ("RC_RAM3", "Rampage 3", (T,)),
    ("RC_RAM4", "Rampage 4", (T,)),
    ("RC_RAM5", "Rampage 5", (T,)),
    ("RC_SAS1", "The Last One", (F,)),
    ("RC_TON1", "Pulling Favors", (F,)),
    ("RC_TON2", "Pulling Another Favor", (F,)),
    ("RC_TON5", "Pulling One Last Favor", (F,)),
)

ALL3 = (M, F, T)
PLAYERS = {
    "FAM1": (M, F), "FAM3": (M, F), "JH2": (M, F), "FAM4": (M, T), "FIB2": ALL3, "FRA1": (F, T), "DH2A": ALL3,
    "DH2B": ALL3, "FIB4": ALL3, "CAR1": (F, T), "CAR2": (F, T), "SOL1": (M, F), "MAR1": (M, T), "CAR4": (F, T),
    "FIB3": (M, T), "FIB5": ALL3, "FH1": ALL3, "MIC1": (M, T), "MIC2": (M, F), "SOL2": (M, F), "EXL1": ALL3,
    "EXL2": ALL3, "EXL3": ALL3, "RH1": (M, T), "RHP1": (M, T), "RH2": ALL3, "AH2": (M, F), "AH3a": ALL3, "AH3b": ALL3,
    "MIC3": ALL3, "FRA2": ALL3, "SOL3": (M, T), "FHPD": ALL3, "FH2A": ALL3, "FH2B": ALL3, "FINA": (F,), "FINB": (F,),
    "FINC1": ALL3,
}
UNSTABLE_PREPS = {"DHP1", "FB4P1", "FB4P2", "FB4P4", "FB4P5", "AHP1", "FHPRA", "FHPRB", "FHPC1", "FHPC2", "FHPC3"}
FREE_CHOICE = {"DHP1", "FB4P1", "FB4P2", "FB4P4", "FB4P5", "AHP1"}
LONG_MISSIONS = {"FIB4", "DH2A", "DH2B", "RH2", "AH3a", "AH3b", "FH2A", "FH2B", "FINC1", "MIC3", "FRA2", "EXL3", "FIB2"}


def players_of(code: str, triggers: tuple, strangers: bool) -> tuple:
    if strangers or code in FREE_CHOICE:
        return ()
    if code in PLAYERS:
        return PLAYERS[code]
    return triggers if len(triggers) == 1 else ()


def minutes_of(code: str, strangers: bool) -> int:
    if strangers:
        return 6
    return 20 if code in LONG_MISSIONS else 10


MISSIONS = tuple(Mission(c, n, t, False, players_of(c, t, False), minutes_of(c, False)) for c, n, t in STORY) + \
    tuple(Mission(c, n, t, True, players_of(c, t, True), minutes_of(c, True)) for c, n, t in STRANGERS)
MISSION_BY_CODE = {m.code: m for m in MISSIONS}
FINALE_CODES = ("FINA", "FINB", "FINC1")

TRAPS = (
    ("ScooterBrother", "Scooter Brothers"),
    ("Ramp", "Ramp"),
    ("AnimalHunt", "Hunting Season"),
    ("AnimalTransform", "Wild Side"),
    ("FallingPlanes", "Mayday"),
    ("TurboTraffic", "Nitro for Everyone"),
    ("AlienAbduction", "Close Encounter"),
    ("Irresistible", "Irresistible"),
    ("SecondComing", "Second Coming"),
    ("Wanted", "Wanted"),
    ("Drunk", "Last Round"),
    ("FlatTires", "Flat Tires"),
    ("Disarmed", "Butterfingers"),
    ("LesterTax", "Lester's Cut"),
    ("Paparazzi", "Paparazzi"),
    ("Clowns", "Send in the Clowns"),
    ("MoonGravity", "Moon Gravity"),
    ("Blizzard", "North Yankton Weather"),
    ("Nightfall", "Nightfall"),
    ("Ragdoll", "Banana Peel"),
    ("LiftOff", "Lift-off"),
    ("PhantomCar", "Phantom Car"),
    ("PhantomWedge", "Phantom Wedge"),
    ("RedLight", "Red Light, Green Light"),
    ("Speed", "Speed"),
    ("TimeMachine", "Back to the Future"),
    ("Purge", "The Purge"),
    ("Terminator", "Terminator"),
    ("Matrix", "The Matrix"),
    ("MadMax", "Fury Road"),
    ("Jaws", "Jaws"),
    ("MacheteGranny", "Florida Grannies"),
    ("HereWeGoAgain", "Here We Go Again"),
    ("Bowling", "Let's Go Bowling"),
    ("ViceCity", "Welcome to the 80s"),
    ("BigSmoke", "Big Smoke's Order"),
    ("BattleRoyale", "Battle Royale"),
    ("MarioKart", "Rainbow Road"),
    ("TopDown", "Grand Theft Auto 1997"),
    ("UpsideDown", "Upside Down"),
    ("Clones", "Attack of the Clones"),
    ("Kidnap", "Taxi"),
    ("ObjectRain", "Cloudy with a Chance of Toilets"),
    ("GiantBalls", "Indiana Jones"),
    ("FailedHeroes", "Failed Heroes"),
    ("Mariachi", "Mariachi"),
    ("FloatingCars", "Flying Cars"),
    ("Kamikaze", "Kamikaze Pedestrians"),
    ("FlashMob", "Flash Mob"),
    ("InitialD", "Initial D"),
    ("ForrestGump", "Run, Forrest, Run"),
    ("WalkingDead", "The Walking Dead"),
)
STREAM_RISKY_TRAPS = ("Irresistible",)

MONEY = (
    ("$5,000", 5000),
    ("$20,000", 20000),
    ("$100,000", 100000),
)

WEAPON_GIFT = "Weapon Gift"
VEHICLE_GIFT = "Vehicle Gift"
COLLECTIBLE_RADAR = "Collectible Radar"
DRIVE_BY_SMG = "Drive-By SMG"
SPORTS_CAR = "Sports Car"
MOTORCYCLE = "Motorcycle"
HELICOPTER = "Helicopter"
UNDERWATER_VEHICLE = "Underwater Vehicle"
BOAT = "Boat"
SCRAMJET = "Scramjet"
GUARANTEED_VEHICLES = (SPORTS_CAR, MOTORCYCLE, HELICOPTER, UNDERWATER_VEHICLE, BOAT, SCRAMJET)


class Collectible(NamedTuple):
    key: str
    name: str
    total: int
    underwater: bool


COLLECTIBLES = (
    Collectible("SpaceshipPart", "Spaceship Parts", 50, False),
    Collectible("LetterScrap", "Letter Scraps", 50, False),
    Collectible("NuclearWaste", "Nuclear Waste", 30, True),
    Collectible("SubmarinePart", "Submarine Parts", 30, True),
)

MAX_GOLD_MEDALS = 123
MAX_OBJECTIVES = 250
MAX_BONUS = 2
MAX_COLLECTIBLE_BONUS = 2
COLLECTIBLE_MINUTES = 2
GOAL_OVERHEAD = 1.3


def mission_item(mission: Mission) -> str:
    return "Mission: " + mission.name


def trap_item(title: str) -> str:
    return "Trap: " + title


def bonus_location(mission: Mission, index: int) -> str:
    return f"{mission.name} (Bonus {index})"


def gold_location(index: int) -> str:
    return f"Gold Medal {index}"


def objective_location(index: int) -> str:
    return f"Objective {index}"


def collectible_location(collectible: Collectible, count: int) -> str:
    return f"{collectible.name}: at {count} collected"


def collectible_bonus_location(collectible: Collectible, count: int, index: int) -> str:
    return f"{collectible.name}: at {count} collected (Bonus {index})"


def build_item_table() -> dict:
    table = {}
    for i, character in enumerate(CHARACTERS):
        table[character] = BASE_ID + 1 + i
    for i, (name, _) in enumerate(MONEY):
        table[name] = BASE_ID + 30 + i
    table[WEAPON_GIFT] = BASE_ID + 50
    table[DRIVE_BY_SMG] = BASE_ID + 52
    table[COLLECTIBLE_RADAR] = BASE_ID + 60
    table[VEHICLE_GIFT] = BASE_ID + 80
    for i, name in enumerate(GUARANTEED_VEHICLES):
        table[name] = BASE_ID + 90 + i
    for i, (_, title) in enumerate(TRAPS):
        table[trap_item(title)] = BASE_ID + 200 + i
    for i, mission in enumerate(MISSIONS):
        table[mission_item(mission)] = BASE_ID + 1000 + i
    return table


def build_location_table() -> dict:
    table = {}
    for i, mission in enumerate(MISSIONS):
        table[mission.name] = BASE_ID + 1000 + i
        for b in range(1, MAX_BONUS + 1):
            table[bonus_location(mission, b)] = BASE_ID + 1000 + b * 1000 + i
    for n in range(1, MAX_GOLD_MEDALS + 1):
        table[gold_location(n)] = BASE_ID + 4000 + n
    for n in range(1, MAX_OBJECTIVES + 1):
        table[objective_location(n)] = BASE_ID + 5000 + n
    for k, collectible in enumerate(COLLECTIBLES):
        for n in range(1, collectible.total + 1):
            table[collectible_location(collectible, n)] = BASE_ID + 6000 + k * 100 + n
            for b in range(1, MAX_COLLECTIBLE_BONUS + 1):
                table[collectible_bonus_location(collectible, n, b)] = BASE_ID + 6000 + b * 1000 + k * 100 + n
    return table


ITEM_NAME_TO_ID = build_item_table()
LOCATION_NAME_TO_ID = build_location_table()
