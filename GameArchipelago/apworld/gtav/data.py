from typing import NamedTuple

try:
    from .shop import UPGRADES, WEAPONS, Upgrade, Weapon
except ImportError:
    from shop import UPGRADES, WEAPONS, Upgrade, Weapon

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
    ("Bouncy", "Boing Boing"),
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


class Vehicle(NamedTuple):
    model: str
    name: str
    water: bool
    kind: str


VEHICLES = (
    Vehicle("deluxo", "Imponte Deluxo", False, ""),
    Vehicle("oppressor2", "Pegassi Oppressor Mk II", False, ""),
    Vehicle("oppressor", "Pegassi Oppressor", False, ""),
    Vehicle("ztype", "Grotti Z-Type", False, ""),
    Vehicle("jb700", "Dewbauchee JB 700", False, ""),
    Vehicle("stromberg", "Ocelot Stromberg", False, "sub"),
    Vehicle("toreador", "Pegassi Toreador", False, "sub"),
    Vehicle("shotaro", "Nagasaki Shotaro", False, ""),
    Vehicle("vigilante", "Grotti Vigilante", False, ""),
    Vehicle("scramjet", "Declasse Scramjet", False, ""),
    Vehicle("ruiner2", "Imponte Ruiner 2000", False, ""),
    Vehicle("voltic2", "Coil Rocket Voltic", False, ""),
    Vehicle("dune4", "BF Ramp Buggy", False, ""),
    Vehicle("blazer5", "Nagasaki Blazer Aqua", False, ""),
    Vehicle("apc", "HVY APC", False, ""),
    Vehicle("khanjali", "Khanjali", False, ""),
    Vehicle("rhino", "Rhino Tank", False, ""),
    Vehicle("thruster", "Mammoth Thruster", False, ""),
    Vehicle("zentorno", "Pegassi Zentorno", False, ""),
    Vehicle("adder", "Truffade Adder", False, ""),
    Vehicle("t20", "Progen T20", False, ""),
    Vehicle("pariah", "Ocelot Pariah", False, ""),
    Vehicle("bati", "Pegassi Bati 801", False, ""),
    Vehicle("hakuchou2", "Shitzu Hakuchou Drag", False, ""),
    Vehicle("faggio2", "Western Faggio", False, ""),
    Vehicle("monster", "Monster Truck", False, ""),
    Vehicle("insurgent2", "HVY Insurgent", False, ""),
    Vehicle("seasparrow", "Buckingham Sea Sparrow", False, "heli"),
    Vehicle("akula", "Buckingham Akula", False, "heli"),
    Vehicle("buzzard", "Buzzard Attack Chopper", False, "heli"),
    Vehicle("maverick", "Buckingham Maverick", False, "heli"),
    Vehicle("hydra", "Mammoth Hydra", False, "plane"),
    Vehicle("dodo", "Mammoth Dodo", False, "plane"),
    Vehicle("submersible2", "Kraken Submarine", True, "sub"),
    Vehicle("submersible", "Submersible", True, "sub"),
    Vehicle("seashark", "Speedophile Seashark", True, "boat"),
    Vehicle("speeder", "Pegassi Speeder", True, "boat"),
    Vehicle("longfin", "Shitzu Longfin", True, "boat"),
    Vehicle("toro", "Lampadati Toro", True, "boat"),
    Vehicle("stunt", "Mallard Stunt Plane", False, "plane"),
    Vehicle("velum2", "JoBuilt Velum", False, "plane"),
    Vehicle("mammatus", "JoBuilt Mammatus", False, "plane"),
    Vehicle("duster", "Western Duster", False, "plane"),
    Vehicle("cuban800", "Cuban 800", False, "plane"),
    Vehicle("microlight", "Ultralight", False, "plane"),
    Vehicle("seabreeze", "Western Seabreeze", False, "plane"),
    Vehicle("howard", "Buckingham Howard NX-25", False, "plane"),
    Vehicle("blimp", "Atomic Blimp", False, ""),
    Vehicle("blimp2", "Xero Blimp", False, ""),
    Vehicle("lazer", "Mammoth P-996 LAZER", False, "plane"),
    Vehicle("pyro", "Buckingham Pyro", False, "plane"),
    Vehicle("besra", "Mammoth Besra", False, "plane"),
    Vehicle("luxor2", "Buckingham Luxor Deluxe (gold)", False, "plane"),
    Vehicle("luxor", "Buckingham Luxor", False, "plane"),
    Vehicle("shamal", "Buckingham Shamal", False, "plane"),
    Vehicle("vestra", "Buckingham Vestra", False, "plane"),
    Vehicle("miljet", "Buckingham Miljet", False, "plane"),
    Vehicle("nimbus", "Buckingham Nimbus", False, "plane"),
    Vehicle("alphaz1", "Buckingham Alpha-Z1", False, "plane"),
    Vehicle("jet", "Jet (airliner)", False, "plane"),
    Vehicle("cargoplane", "Cargo Plane", False, "plane"),
    Vehicle("titan", "Eckhart Titan", False, "plane"),
)
AIRCRAFT = tuple(v for v in VEHICLES if v.kind in ("heli", "plane"))
SUBMARINES = tuple(v for v in VEHICLES if v.kind == "sub")


class Activity(NamedTuple):
    entry: int
    name: str
    group: str
    character: str


ACTIVITIES = (
    Activity(112, "Off-Road Race 1", "races", ""),
    Activity(113, "Off-Road Race 2", "races", ""),
    Activity(114, "Off-Road Race 3", "races", ""),
    Activity(115, "Off-Road Race 4", "races", ""),
    Activity(116, "Off-Road Race 5", "races", ""),
    Activity(117, "Off-Road Race 6", "races", ""),
    Activity(119, "Shooting Range: Handguns", "range", ""),
    Activity(120, "Shooting Range: SMGs", "range", ""),
    Activity(121, "Shooting Range: Rifles", "range", ""),
    Activity(122, "Shooting Range: Machine Guns", "range", ""),
    Activity(123, "Shooting Range: Heavy Weapons", "range", ""),
    Activity(124, "Shooting Range: Shotguns", "range", ""),
    Activity(125, "Stunt Plane Time Trials", "flight", ""),
    Activity(126, "Tennis", "sports", ""),
    Activity(127, "Golf", "sports", ""),
    Activity(130, "Darts", "sports", ""),
    Activity(300, "Yoga", "sports", "Michael"),
    Activity(128, "Base Jumping", "parachute", ""),
    Activity(129, "Skydiving", "parachute", ""),
    Activity(141, "Triathlon 1", "races", ""),
    Activity(142, "Triathlon 2", "races", ""),
    Activity(143, "Triathlon 3", "races", ""),
    Activity(132, "Sea Race 1", "races", ""),
    Activity(133, "Sea Race 2", "races", ""),
    Activity(134, "Sea Race 3", "races", ""),
    Activity(135, "Sea Race 4", "races", ""),
    Activity(136, "Street Race: City Circuit", "races", "Franklin"),
    Activity(137, "Street Race: Airport", "races", "Franklin"),
    Activity(138, "Street Race: Freeway", "races", "Franklin"),
    Activity(139, "Street Race: Vespucci Canals", "races", "Franklin"),
    Activity(140, "Street Race: South Los Santos", "races", "Franklin"),
    Activity(181, "Flight School Lesson 1", "flight", ""),
    Activity(182, "Flight School Lesson 2", "flight", ""),
    Activity(183, "Flight School Lesson 3", "flight", ""),
    Activity(184, "Flight School Lesson 4", "flight", ""),
    Activity(185, "Flight School Lesson 5", "flight", ""),
    Activity(186, "Flight School Lesson 6", "flight", ""),
    Activity(187, "Flight School Lesson 7", "flight", ""),
    Activity(188, "Flight School Lesson 8", "flight", ""),
    Activity(189, "Flight School Lesson 9", "flight", ""),
    Activity(190, "Flight School Lesson 10", "flight", ""),
    Activity(191, "Flight School Lesson 11", "flight", ""),
    Activity(192, "Flight School Lesson 12", "flight", ""),
    Activity(288, "Watch a Movie", "leisure", ""),
    Activity(301, "Ride the Cable Car", "leisure", ""),
    Activity(302, "Car Wash", "leisure", ""),
    Activity(304, "Buy Clothes", "leisure", ""),
    Activity(305, "Modify a Car", "leisure", ""),
    Activity(306, "Get a Haircut", "leisure", ""),
    Activity(307, "Get a Tattoo", "leisure", ""),
    Activity(309, "Watch TV", "leisure", ""),
    Activity(297, "Hold Up a Store", "leisure", ""),
    Activity(149, "Bail Bond 1", "trevor", "Trevor"),
    Activity(150, "Bail Bond 2", "trevor", "Trevor"),
    Activity(151, "Bail Bond 3", "trevor", "Trevor"),
    Activity(152, "Bail Bond 4", "trevor", "Trevor"),
    Activity(153, "Air Trafficking 1", "trevor", "Trevor"),
    Activity(154, "Air Trafficking 2", "trevor", "Trevor"),
    Activity(155, "Air Trafficking 3", "trevor", "Trevor"),
    Activity(156, "Air Trafficking 4", "trevor", "Trevor"),
    Activity(157, "Air Trafficking 5", "trevor", "Trevor"),
    Activity(158, "Ground Trafficking 1", "trevor", "Trevor"),
    Activity(159, "Ground Trafficking 2", "trevor", "Trevor"),
    Activity(160, "Ground Trafficking 3", "trevor", "Trevor"),
    Activity(161, "Ground Trafficking 4", "trevor", "Trevor"),
    Activity(162, "Ground Trafficking 5", "trevor", "Trevor"),
    Activity(180, "Hunting", "trevor", "Trevor"),
)


class Collectible(NamedTuple):
    key: str
    name: str
    total: int
    underwater: bool
    label: str = "at {n} collected"
    aircraft: bool = False
    feat: bool = False
    minutes: int = 2


COLLECTIBLES = (
    Collectible("SpaceshipPart", "Spaceship Parts", 50, False),
    Collectible("LetterScrap", "Letter Scraps", 50, False),
    Collectible("NuclearWaste", "Nuclear Waste", 30, True),
    Collectible("SubmarinePart", "Submarine Parts", 30, True),
    Collectible("MonkeyMosaic", "Monkey Mosaics", 50, False),
    Collectible("PeyotePlant", "Peyote Plants", 27, True),
    Collectible("EpsilonTract", "Epsilon Tracts", 10, False, minutes=4),
    Collectible("StuntJump", "Stunt Jumps", 50, False, "{n} completed", feat=True, minutes=3),
    Collectible("UnderBridge", "Under the Bridge", 50, False, "{n} flown", True, True, 2),
    Collectible("KnifeFlight", "Knife Flights", 15, False, "{n} flown", True, True, 3),
)
PICKUPS = tuple(c for c in COLLECTIBLES if not c.feat)

LANDMARKS = (
    ("MountChiliad", "Mount Chiliad Summit"),
    ("MazeBankTower", "Maze Bank Tower Roof"),
    ("FibBuilding", "FIB Building Roof"),
    ("IaaBuilding", "IAA Building Roof"),
    ("VinewoodSign", "Vinewood Sign"),
    ("Observatory", "Galileo Observatory"),
    ("LandActDam", "Land Act Dam"),
    ("DelPerroPier", "Del Perro Pier"),
    ("Airport", "Los Santos International Airport"),
    ("SandyShoresAirfield", "Sandy Shores Airfield"),
    ("Prison", "Bolingbroke Penitentiary"),
    ("HumaneLabs", "Humane Labs"),
    ("FortZancudo", "Fort Zancudo"),
    ("PaletoSheriff", "Paleto Bay Sheriff's Office"),
    ("Grapeseed", "Grapeseed"),
    ("StabCity", "Stab City"),
    ("TrevorsTrailer", "Trevor's Trailer"),
    ("AltruistCamp", "Altruist Camp"),
    ("KortzCenter", "Kortz Center"),
    ("MichaelsHouse", "Michael's House"),
    ("FranklinsHouse", "Franklin's House"),
    ("GroveStreet", "Grove Street"),
    ("LestersHouse", "Lester's House"),
    ("FloydsApartment", "Floyd's Apartment"),
    ("MazeBankArena", "Maze Bank Arena"),
    ("PillboxMedical", "Pillbox Hill Medical Center"),
    ("VespucciBeach", "Vespucci Beach"),
    ("ChumashPier", "Chumash Pier"),
    ("ElGordoLighthouse", "El Gordo Lighthouse"),
    ("WindFarm", "RON Alternates Wind Farm"),
    ("SatelliteArray", "Satellite Dishes"),
    ("Sawmill", "Paleto Forest Sawmill"),
    ("AlamoSea", "Alamo Sea"),
    ("PacificStandard", "Pacific Standard Bank"),
    ("GolfClub", "Los Santos Golf Club"),
    ("BahamaMamas", "Bahama Mamas"),
    ("TequiLaLa", "Tequi-la-la"),
    ("CluckinBell", "Cluckin' Bell Farms"),
    ("Hookies", "Hookies"),
    ("MirrorPark", "Mirror Park"),
    ("Port", "Port of Los Santos"),
    ("PaletoBank", "Blaine County Savings Bank"),
    ("LosSantosCustoms", "Los Santos Customs"),
    ("MadrazoRanch", "Madrazo's Ranch"),
)

SKILLS = (
    "Stamina",
    "Shooting",
    "Strength",
    "Stealth",
    "Flying",
    "Driving",
    "Lung Capacity",
)
SKILL_LEVELS = tuple(range(10, 101, 10))
MAX_ESCAPE_STARS = 5
LANDMARK_MINUTES = 3
ACTIVITY_MINUTES = 8
SKILL_MINUTES = 1
ESCAPE_MINUTES = 3

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
    return f"{collectible.name}: {collectible.label.format(n=count)}"


def collectible_bonus_location(collectible: Collectible, count: int, index: int) -> str:
    return f"{collectible_location(collectible, count)} (Bonus {index})"


def landmark_id(key: str) -> str:
    return "Landmark_" + key


def landmark_location(name: str) -> str:
    return "Visit " + name


def skill_key(skill: str) -> str:
    return skill.replace(" ", "")


def skill_id(character: str, skill: str, level: int) -> str:
    return f"Skill_{character}_{skill_key(skill)}_{level}"


def skill_location(character: str, skill: str, level: int) -> str:
    return f"{character}: {skill} {level}%"


def activity_id(activity: "Activity") -> str:
    return f"Activity_{activity.entry}"


def activity_location(activity: "Activity") -> str:
    return "Activity: " + activity.name


def escape_id(stars: int) -> str:
    return f"Escape_{stars}"


def escape_location(stars: int) -> str:
    return f"Escape a {stars}-star wanted level"


def vehicle_item(vehicle: Vehicle) -> str:
    return "Vehicle: " + vehicle.name


def weapon_item(weapon: Weapon) -> str:
    return "Weapon: " + weapon.name


def upgrades_item(weapon: Weapon) -> str:
    return "Upgrades: " + weapon.name


def weapon_location(weapon: Weapon) -> str:
    return "Buy " + weapon.name


def upgrade_location(weapon: Weapon, upgrade: Upgrade) -> str:
    return f"Buy {weapon.name}: {upgrade.name}"


UPGRADE_WEAPONS = tuple(w for w in WEAPONS if w.id in UPGRADES)


class GameShop(NamedTuple):
    key: str
    label: str
    categories: tuple


GAME_SHOPS = (
    GameShop("Barber", "Barber", ("Haircut", "Beard")),
    GameShop("Clothes", "Clothes Store", ("Tops", "Pants", "Shoes", "Hats", "Glasses", "Outfits", "Masks")),
    GameShop("Tattoo", "Tattoo Parlor", ("Head", "Torso", "Left Arm", "Right Arm", "Left Leg", "Right Leg")),
    GameShop("Customs", "Los Santos Customs", (
        "Engine", "Brakes", "Transmission", "Suspension", "Armor", "Turbo", "Horn", "Headlights",
        "Wheels", "Tires", "Respray", "Windows", "Plate", "Body Kit",
    )),
)


GAME_SHOP_PRICES = {
    ("Customs", "Engine"): 2000,
    ("Customs", "Brakes"): 2000,
    ("Customs", "Transmission"): 3000,
    ("Customs", "Suspension"): 2000,
    ("Customs", "Armor"): 3000,
    ("Customs", "Turbo"): 12500,
    ("Customs", "Wheels"): 2000,
    ("Customs", "Body Kit"): 2000,
}

FREE_MONEY = 2000


def game_shop_item(shop: GameShop) -> str:
    return "Shop: " + shop.label


def game_shop_location(shop: GameShop, category: str) -> str:
    return f"{shop.label}: {category}"


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
    for i, vehicle in enumerate(VEHICLES):
        table[vehicle_item(vehicle)] = BASE_ID + 500 + i
    for i, weapon in enumerate(WEAPONS):
        table[weapon_item(weapon)] = BASE_ID + 300 + i
    for i, weapon in enumerate(WEAPONS):
        if weapon.id in UPGRADES:
            table[upgrades_item(weapon)] = BASE_ID + 400 + i
    for i, mission in enumerate(MISSIONS):
        table[mission_item(mission)] = BASE_ID + 1000 + i
    for i, shop in enumerate(GAME_SHOPS):
        table[game_shop_item(shop)] = BASE_ID + 600 + i
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
    for c, character in enumerate(CHARACTERS):
        for s, skill in enumerate(SKILLS):
            for level in SKILL_LEVELS:
                table[skill_location(character, skill, level)] = BASE_ID + 9000 + c * 100 + s * 10 + level // 10
    for i, (_, name) in enumerate(LANDMARKS):
        table[landmark_location(name)] = BASE_ID + 9500 + i
    for stars in range(1, MAX_ESCAPE_STARS + 1):
        table[escape_location(stars)] = BASE_ID + 9700 + stars
    for i, activity in enumerate(ACTIVITIES):
        table[activity_location(activity)] = BASE_ID + 9600 + i
    for i, weapon in enumerate(WEAPONS):
        table[weapon_location(weapon)] = BASE_ID + 9800 + i
        for j, upgrade in enumerate(UPGRADES.get(weapon.id, ())):
            table[upgrade_location(weapon, upgrade)] = BASE_ID + 10000 + i * 20 + j
    for k, shop in enumerate(GAME_SHOPS):
        for c, category in enumerate(shop.categories):
            table[game_shop_location(shop, category)] = BASE_ID + 9900 + k * 20 + c
    return table


def skill_location_id(character: str, skill: str, level: int) -> int:
    return BASE_ID + 9000 + CHARACTERS.index(character) * 100 + SKILLS.index(skill) * 10 + level // 10


FREE_ROAM_KEYS = ("MonkeyMosaic", "PeyotePlant", "EpsilonTract", "StuntJump", "UnderBridge", "KnifeFlight")


ITEM_NAME_TO_ID = build_item_table()
LOCATION_NAME_TO_ID = build_location_table()
