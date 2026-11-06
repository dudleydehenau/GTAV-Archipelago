import math
from typing import Any, Dict, List, Optional, TextIO, Tuple

from BaseClasses import CollectionState, Item, ItemClassification, Location, Region, Tutorial
from worlds.AutoWorld import WebWorld, World

from Options import OptionError

from .data import (
    BASE_ID, CHARACTERS, COLLECTIBLE_RADAR, ESCAPE_MINUTES, FREE_ROAM_KEYS, GOAL_OVERHEAD, COLLECTIBLES, DRIVE_BY_SMG, FINALE_CODES, GAME,
    GUARANTEED_VEHICLES, HELICOPTER, ITEM_NAME_TO_ID, LANDMARK_MINUTES, LANDMARKS, UNSTABLE_PREPS, LOCATION_NAME_TO_ID,
    MAX_ESCAPE_STARS, MISSION_BY_CODE, MISSIONS, MONEY, PICKUPS, SKILL_LEVELS, SKILL_MINUTES, SKILLS, TRAPS, UNDERWATER_VEHICLE,
    VEHICLE_GIFT, WEAPON_GIFT, Collectible, Mission, bonus_location, collectible_bonus_location, collectible_location,
    escape_location, gold_location, landmark_id, landmark_location, mission_item, objective_location, skill_location, trap_item,
    UPGRADE_WEAPONS, UPGRADES, WEAPONS, upgrade_location, upgrades_item, weapon_item, weapon_location,
    AIRCRAFT, SUBMARINES, VEHICLES, vehicle_item, ACTIVITIES, ACTIVITY_MINUTES, Activity, activity_id, activity_location,
    GAME_SHOPS, GAME_SHOP_PRICES, FREE_MONEY, game_shop_item, game_shop_location,
)

MONEY_NAMES = {name for name, _ in MONEY}
from .options import GOAL_MISSION_CODES, CollectibleRadar, GTAVOptions, Goal, WeaponShop, WeaponUpgrades, option_groups, option_presets


class GTAVItem(Item):
    game = GAME


class GTAVLocation(Location):
    game = GAME


class GTAVWeb(WebWorld):
    theme = "partyTime"
    rich_text_options_doc = True
    option_groups = option_groups
    options_presets = option_presets
    tutorials = [
        Tutorial(
            "Multiworld Setup Guide",
            "How to install the mods and connect GTA V Legacy to an Archipelago multiworld.",
            "English",
            "setup_en.md",
            "setup/en",
            ["GTA V Archipelago"],
        )
    ]


class GTAVWorld(World):
    """
    Grand Theft Auto V story mode, rebuilt as a sterile open world. Missions, Strangers and Freaks, gold medals,
    optional objectives, every hidden collectible, stunt jumps, bridges, landmarks, skills and police escapes are
    checks. Michael, Franklin and Trevor, the mission unlocks,
    money, weapons, vehicles and more than fifty traps are items.
    """

    game = GAME
    web = GTAVWeb()
    options_dataclass = GTAVOptions
    options: GTAVOptions
    topology_present = False
    item_name_to_id = ITEM_NAME_TO_ID
    location_name_to_id = LOCATION_NAME_TO_ID
    item_name_groups = {
        "Characters": set(CHARACTERS),
        "Missions": {mission_item(m) for m in MISSIONS if m.code not in UNSTABLE_PREPS},
        "Story Missions": {mission_item(m) for m in MISSIONS if not m.strangers and m.code not in UNSTABLE_PREPS},
        "Strangers and Freaks": {mission_item(m) for m in MISSIONS if m.strangers},
        "Traps": {trap_item(title) for _, title in TRAPS},
        "Gifts": {WEAPON_GIFT, VEHICLE_GIFT},
        "Vehicles": set(GUARANTEED_VEHICLES) | {vehicle_item(v) for v in VEHICLES},
        "Money": {name for name, _ in MONEY},
        "Weapons": {weapon_item(w) for w in WEAPONS},
        "Weapon Upgrades": {upgrades_item(w) for w in UPGRADE_WEAPONS},
        "Shops": {game_shop_item(shop) for shop in GAME_SHOPS},
    }
    location_name_groups = {
        "Story Missions": {m.name for m in MISSIONS if not m.strangers and m.code not in UNSTABLE_PREPS},
        "Strangers and Freaks": {m.name for m in MISSIONS if m.strangers},
        "Collectibles": {name for name in LOCATION_NAME_TO_ID if any(name.startswith(c.name + ":") for c in PICKUPS)},
        "Free Roam": {name for name in LOCATION_NAME_TO_ID
                      if any(name.startswith(c.name + ":") for c in COLLECTIBLES if c.key in FREE_ROAM_KEYS)
                      or LOCATION_NAME_TO_ID[name] >= BASE_ID + 9000},
        "Landmarks": {landmark_location(name) for _, name in LANDMARKS},
        "Skills": {skill_location(c, s, level) for c in CHARACTERS for s in SKILLS for level in SKILL_LEVELS},
        "Activities": {activity_location(a) for a in ACTIVITIES},
        "Ammu-Nation": {weapon_location(w) for w in WEAPONS} | {upgrade_location(w, u) for w in UPGRADE_WEAPONS for u in UPGRADES[w.id]},
        "Shops": {game_shop_location(shop, c) for shop in GAME_SHOPS for c in shop.categories},
    }

    def generate_early(self) -> None:
        self.start_character = CHARACTERS[self.options.starting_character.value]
        pool: List[Mission] = [m for m in MISSIONS if (self.options.strangers_and_freaks or not m.strangers)
                               and m.code not in UNSTABLE_PREPS]
        goal = self.options.goal.value
        self.goal_mission: Optional[Mission] = None
        if goal == Goal.option_finale:
            forced = [MISSION_BY_CODE[code] for code in FINALE_CODES]
        elif goal == Goal.option_the_third_way:
            forced = [MISSION_BY_CODE["FINC1"]]
        elif goal == Goal.option_one_mission:
            if self.options.goal_mission.value > 0:
                self.goal_mission = MISSION_BY_CODE[GOAL_MISSION_CODES[self.options.goal_mission.value - 1]]
                if self.goal_mission not in pool:
                    pool.append(self.goal_mission)
            else:
                candidates = [m for m in pool if self.needs_other_character(m)]
                self.goal_mission = self.random.choice(candidates)
            forced = [self.goal_mission]
        else:
            forced = []
        own = [m for m in pool if self.solo_mission(m) and m not in forced]
        self.random.shuffle(own)
        self.starting_missions: List[Mission] = own[:self.options.starting_missions.value]
        kept = set(forced) | set(self.starting_missions)
        others = [m for m in pool if m not in kept]
        self.random.shuffle(others)
        kept |= set(others[:max(0, self.options.mission_count.value - len(kept))])
        self.missions: List[Mission] = [m for m in pool if m in kept]
        self.gold_checks = min(self.options.gold_medal_checks.value, len(self.missions) * 3 // 4)
        self.objective_checks = min(self.options.objective_checks.value, len(self.missions) * 3 // 2)
        self.collectible_steps: Dict[str, int] = {
            "SpaceshipPart": self.options.spaceship_parts_per_check.value,
            "LetterScrap": self.options.letter_scraps_per_check.value,
            "NuclearWaste": self.options.nuclear_waste_per_check.value,
            "SubmarinePart": self.options.submarine_parts_per_check.value,
            "MonkeyMosaic": self.options.monkey_mosaics_per_check.value,
            "PeyotePlant": self.options.peyote_plants_per_check.value,
            "EpsilonTract": self.options.epsilon_tracts_per_check.value,
            "StuntJump": self.options.stunt_jumps_per_check.value,
            "UnderBridge": self.options.bridges_per_check.value,
            "KnifeFlight": self.options.knife_flights_per_check.value,
        }
        self.landmarks: List[Tuple[str, str]] = list(LANDMARKS) if self.options.landmarks else []
        self.skill_step = self.options.skill_checks.value
        self.escapes = bool(self.options.police_escapes)
        self.activities: List[Activity] = [a for a in ACTIVITIES if self.options.activities
                                           and a.group != "trevor"]
        self.weapon_shop = self.options.weapon_shop.value
        self.weapon_upgrades = self.options.weapon_upgrades.value
        self.money_needs: Dict[str, int] = {}
        self.money_pool = 0
        self.money_rules = any(cost > FREE_MONEY for cost in self.purchase_costs().values())
        self.mission_keys = [(mission_item(m), m.players, m.triggers) for m in self.missions]
        self.vehicle_unlocks = bool(self.options.vehicle_unlocks)
        self.vehicles = bool(self.options.guaranteed_vehicles) and not self.vehicle_unlocks
        if self.vehicle_unlocks:
            self.air_items = tuple(vehicle_item(v) for v in AIRCRAFT)
            self.sub_items = tuple(vehicle_item(v) for v in SUBMARINES)
        else:
            self.air_items = (HELICOPTER,)
            self.sub_items = (UNDERWATER_VEHICLE,)
        vehicle_logic = self.vehicles or self.vehicle_unlocks
        self.goal_collectible: Optional[Tuple[Collectible, int]] = None
        if goal == Goal.option_one_collectible:
            kind = self.random.choices(PICKUPS, [c.total for c in PICKUPS])[0]
            self.goal_collectible = (kind, self.random.randint(1, kind.total))
        self.goal_location: Optional[str] = None
        self.underwater_logic = vehicle_logic and (any(
            self.collectible_steps[c.key] > 0 for c in COLLECTIBLES if c.underwater)
            or (self.goal_collectible is not None and self.goal_collectible[0].underwater))
        self.aircraft_logic = vehicle_logic and any(self.collectible_steps[c.key] > 0 for c in COLLECTIBLES if c.aircraft)
        flight_school = any(a.group == "flight" for a in self.activities)
        self.flying_logic = vehicle_logic and self.skill_step > 0 and not flight_school
        self.goal_count = 0
        if goal == Goal.option_free_roam:
            available = len(self.free_roam_names())
            if available == 0:
                raise OptionError(f"{self.player_name}: the free_roam goal needs free-roam locations (landmarks, stunt jumps, "
                                  "bridges, skills, police escapes, mosaics, peyote plants or Epsilon tracts).")
            self.goal_count = min(available, self.options.goal_free_roam_checks.value)

    def needs_other_character(self, mission: Mission) -> bool:
        if self.start_character not in mission.triggers:
            return True
        return any(p != self.start_character for p in mission.players)

    def solo_mission(self, mission: Mission) -> bool:
        return not mission.strangers and self.start_character in mission.triggers and \
            all(p == self.start_character for p in mission.players)

    def collectible_counts(self, collectible) -> List[int]:
        step = self.collectible_steps[collectible.key]
        if step <= 0:
            return []
        counts = list(range(step, collectible.total + 1, step))
        if not counts or counts[-1] != collectible.total:
            counts.append(collectible.total)
        return counts

    def skill_levels(self) -> List[int]:
        if self.skill_step <= 0:
            return []
        return [level for level in SKILL_LEVELS if level % self.skill_step == 0]

    def feat_names(self) -> List[str]:
        names = [landmark_location(name) for _, name in self.landmarks]
        for character in CHARACTERS:
            for skill in SKILLS:
                for level in self.skill_levels():
                    names.append(skill_location(character, skill, level))
        if self.escapes:
            names += [escape_location(stars) for stars in range(1, MAX_ESCAPE_STARS + 1)]
        names += [activity_location(a) for a in self.activities]
        return names

    def shop_names(self) -> List[str]:
        names = []
        if self.weapon_shop == WeaponShop.option_shopsanity:
            names += [weapon_location(w) for w in WEAPONS]
        if self.weapon_upgrades == WeaponUpgrades.option_shopsanity:
            names += [upgrade_location(w, u) for w in UPGRADE_WEAPONS for u in UPGRADES[w.id]]
        if self.options.game_shops:
            names += [game_shop_location(shop, c) for shop in GAME_SHOPS for c in shop.categories]
        return names

    def free_roam_names(self) -> List[str]:
        names = list(self.feat_names())
        for collectible in COLLECTIBLES:
            if collectible.key in FREE_ROAM_KEYS:
                names += self.collectible_names(collectible)
        return names

    def collectible_names(self, collectible) -> List[str]:
        names = []
        for count in self.collectible_counts(collectible):
            names.append(collectible_location(collectible, count))
            for b in range(1, self.options.checks_per_collectible.value):
                names.append(collectible_bonus_location(collectible, count, b))
        return names

    def create_regions(self) -> None:
        menu = Region("Menu", self.player, self.multiworld)
        city = Region("Los Santos", self.player, self.multiworld)
        names: Dict[str, int] = {}
        for mission in self.missions:
            names[mission.name] = LOCATION_NAME_TO_ID[mission.name]
            for b in range(1, self.options.checks_per_mission.value):
                name = bonus_location(mission, b)
                names[name] = LOCATION_NAME_TO_ID[name]
        for n in range(1, self.gold_checks + 1):
            names[gold_location(n)] = LOCATION_NAME_TO_ID[gold_location(n)]
        for n in range(1, self.objective_checks + 1):
            names[objective_location(n)] = LOCATION_NAME_TO_ID[objective_location(n)]
        for collectible in COLLECTIBLES:
            for name in self.collectible_names(collectible):
                names[name] = LOCATION_NAME_TO_ID[name]
        for name in self.feat_names() + self.shop_names():
            names[name] = LOCATION_NAME_TO_ID[name]
        city.add_locations(names, GTAVLocation)
        if self.options.goal.value == Goal.option_check_percentage:
            self.goal_count = max(1, math.ceil(len(names) * self.options.goal_check_percentage.value / 100))
        if self.options.goal.value == Goal.option_one_check:
            self.goal_location = self.random.choice(sorted(names))
        self.multiworld.regions += [menu, city]
        menu.connect(city)

    def create_item(self, name: str) -> GTAVItem:
        if name in CHARACTERS or name.startswith("Mission: "):
            classification = ItemClassification.progression
        elif name in getattr(self, "sub_items", (UNDERWATER_VEHICLE,)) and getattr(self, "underwater_logic", True):
            classification = ItemClassification.progression
        elif name in getattr(self, "air_items", (HELICOPTER,)) and \
                (getattr(self, "aircraft_logic", True) or getattr(self, "flying_logic", False)):
            classification = ItemClassification.progression
        elif name.startswith("Vehicle: "):
            classification = ItemClassification.useful
        elif name.startswith("Weapon: "):
            classification = ItemClassification.progression \
                if getattr(self, "weapon_shop", 2) == WeaponShop.option_shopsanity or getattr(self, "weapon_upgrades", 2) == WeaponUpgrades.option_shopsanity \
                else ItemClassification.useful
        elif name.startswith("Upgrades: "):
            classification = ItemClassification.progression \
                if getattr(self, "weapon_upgrades", 2) == WeaponUpgrades.option_shopsanity else ItemClassification.useful
        elif name.startswith("Shop: "):
            classification = ItemClassification.progression
        elif name in MONEY_NAMES and getattr(self, "money_rules", False):
            classification = ItemClassification.progression_skip_balancing
        elif name.startswith("Trap: "):
            classification = ItemClassification.trap
        elif name in (WEAPON_GIFT, VEHICLE_GIFT, COLLECTIBLE_RADAR, DRIVE_BY_SMG) or name in GUARANTEED_VEHICLES:
            classification = ItemClassification.useful
        else:
            classification = ItemClassification.filler
        return GTAVItem(name, classification, ITEM_NAME_TO_ID[name], self.player)

    def create_items(self) -> None:
        self.multiworld.push_precollected(self.create_item(self.start_character))
        if self.options.starting_drive_by_weapon:
            self.multiworld.push_precollected(self.create_item(DRIVE_BY_SMG))
        for mission in self.starting_missions:
            self.multiworld.push_precollected(self.create_item(mission_item(mission)))
        starting = {m.code for m in self.starting_missions}
        missions = [mission_item(m) for m in self.missions if m.code not in starting]
        self.random.shuffle(missions)
        required = [c for c in CHARACTERS if c != self.start_character]
        if self.vehicles:
            required += list(GUARANTEED_VEHICLES)
        if self.options.collectible_radar == CollectibleRadar.option_item:
            required.append(COLLECTIBLE_RADAR)
        required += missions
        if self.weapon_shop != WeaponShop.option_gifts:
            required += [weapon_item(w) for w in WEAPONS]
        if self.weapon_upgrades != WeaponUpgrades.option_off:
            required += [upgrades_item(w) for w in UPGRADE_WEAPONS]
        if self.vehicle_unlocks:
            required += [vehicle_item(v) for v in VEHICLES]
        if self.options.game_shops:
            required += [game_shop_item(shop) for shop in GAME_SHOPS]
        slots = len(self.multiworld.get_unfilled_locations(self.player))
        spare = max(8, math.ceil(len(self.missions) * 0.12))
        if self.money_rules:
            big_name, big_amount = MONEY[-1]
            big = big_amount * self.options.money_multiplier.value // 100
            costs = [max(0, cost - FREE_MONEY) for cost in self.purchase_costs().values()]
            room = max(0, (slots - spare - len(required)) // 2)
            count = min(room, math.ceil(sum(costs) / big)) if big > 0 else 0
            required += [big_name] * count
            self.money_pool = count * big
        while len(required) > max(0, slots - spare):
            self.multiworld.push_precollected(self.create_item(required.pop()))
        pool = [self.create_item(name) for name in required]
        for _ in range(slots - len(pool)):
            pool.append(self.create_item(self.get_filler_item_name()))
        self.multiworld.itempool += pool
        room = self.first_sphere_size() - 4
        if self.vehicle_unlocks and self.options.early_vehicles:
            small = [vehicle_item(v) for v in VEHICLES if v.model in ("dodo", "stunt", "duster", "cuban800", "mammatus", "velum2", "howard")]
            early = [self.random.choice(small), self.random.choice(self.sub_items)]
            room = max(room, 2)
        elif self.vehicle_unlocks:
            early = []
            if self.underwater_logic:
                early.append(self.random.choice(self.sub_items))
            if self.aircraft_logic:
                early.append(self.random.choice([vehicle_item(v) for v in AIRCRAFT if v.kind == "heli"]))
        else:
            early = [UNDERWATER_VEHICLE, HELICOPTER]
        for name in early:
            if name in required and room > 0:
                self.multiworld.early_items[self.player][name] = 1
                room -= 1

    def purchase_costs(self) -> Dict[str, int]:
        costs: Dict[str, int] = {}
        multiplier = self.options.weapon_price_multiplier.value
        if self.weapon_shop == WeaponShop.option_shopsanity:
            for weapon in WEAPONS:
                costs[weapon_location(weapon)] = weapon.price * multiplier // 100
        if self.weapon_upgrades == WeaponUpgrades.option_shopsanity:
            for weapon in UPGRADE_WEAPONS:
                for upgrade in UPGRADES[weapon.id]:
                    costs[upgrade_location(weapon, upgrade)] = (weapon.price + upgrade.price) * multiplier // 100
        if self.options.game_shops:
            for shop in GAME_SHOPS:
                for category in shop.categories:
                    costs[game_shop_location(shop, category)] = GAME_SHOP_PRICES.get((shop.key, category), 500)
        return costs

    def money_value(self, state: CollectionState) -> int:
        total = sum(state.count(name, self.player) * amount for name, amount in MONEY)
        return total * self.options.money_multiplier.value // 100

    def first_sphere_size(self) -> int:
        size = len(self.starting_missions) * self.options.checks_per_mission.value
        pace = min(20, max(1, len(self.missions) // 2))
        for collectible in COLLECTIBLES:
            if collectible.underwater and self.underwater_logic:
                continue
            if collectible.aircraft and self.aircraft_logic:
                continue
            for count in self.collectible_counts(collectible):
                if count * pace // collectible.total == 0:
                    size += self.options.checks_per_collectible.value
        shop_entries = {304, 305, 306, 307} if self.options.game_shops else set()
        size += len(self.landmarks) + sum(1 for a in self.activities if a.entry not in shop_entries and not a.character)
        if self.escapes:
            size += MAX_ESCAPE_STARS
        return size

    def get_filler_item_name(self) -> str:
        traps = sorted(self.options.enabled_traps.value)
        if traps and self.random.randint(1, 100) <= self.options.trap_percentage.value:
            trap_id = self.random.choice(traps)
            return trap_item(dict(TRAPS)[trap_id])
        weapon_weight = self.options.weapon_weight.value if self.weapon_shop == WeaponShop.option_gifts else 0
        vehicle_weight = 0 if self.vehicle_unlocks else self.options.vehicle_weight.value
        weights = [self.options.money_weight.value, weapon_weight, vehicle_weight]
        if sum(weights) == 0:
            weights = [1, 0, 0]
        kind = self.random.choices(("money", "weapon", "vehicle"), weights)[0]
        if kind == "weapon":
            return WEAPON_GIFT
        if kind == "vehicle":
            return VEHICLE_GIFT
        return self.random.choices([name for name, _ in MONEY], [6, 3, 1])[0]

    def mission_rule(self, mission: Mission):
        item = mission_item(mission)
        players = mission.players
        triggers = mission.triggers
        player = self.player
        return lambda state: state.has(item, player) and state.has_all(players, player) and \
            state.has_any(triggers, player)

    def playable_missions(self, state: CollectionState) -> int:
        player = self.player
        return sum(1 for item, players, triggers in self.mission_keys
                   if state.has(item, player) and state.has_all(players, player) and state.has_any(triggers, player))

    def set_rules(self) -> None:
        self.needs: Dict[str, Tuple[int, Optional[str], tuple, tuple]] = {}
        for mission in self.missions:
            rule = self.mission_rule(mission)
            self.get_location(mission.name).access_rule = rule
            self.needs[mission.name] = (0, mission_item(mission), mission.players, mission.triggers)
            for b in range(1, self.options.checks_per_mission.value):
                self.get_location(bonus_location(mission, b)).access_rule = rule
                self.needs[bonus_location(mission, b)] = self.needs[mission.name]
        total = len(self.missions)
        for n in range(1, self.gold_checks + 1):
            needed = min(total, n * 2)
            self.get_location(gold_location(n)).access_rule = lambda state, k=needed: self.playable_missions(state) >= k
            self.needs[gold_location(n)] = (needed, None, (), ())
        for n in range(1, self.objective_checks + 1):
            needed = min(total, n)
            self.get_location(objective_location(n)).access_rule = \
                lambda state, k=needed: self.playable_missions(state) >= k
            self.needs[objective_location(n)] = (needed, None, (), ())
        player = self.player
        pace = min(20, max(1, len(self.missions) // 2))
        for collectible in COLLECTIBLES:
            underwater = self.underwater_logic and collectible.underwater
            aircraft = self.aircraft_logic and collectible.aircraft
            for count in self.collectible_counts(collectible):
                needed = count * pace // collectible.total
                rule = self.collectible_rule(needed, underwater, aircraft)
                vehicles = self.sub_items if underwater else self.air_items if aircraft else ()
                self.get_location(collectible_location(collectible, count)).access_rule = rule
                self.needs[collectible_location(collectible, count)] = (needed, None, (), vehicles)
                for b in range(1, self.options.checks_per_collectible.value):
                    self.get_location(collectible_bonus_location(collectible, count, b)).access_rule = rule
                    self.needs[collectible_bonus_location(collectible, count, b)] = (needed, None, (), vehicles)
        for character in CHARACTERS:
            for skill in SKILLS:
                for level in self.skill_levels():
                    needed = level * pace // 100
                    air = self.air_items if skill == "Flying" and self.flying_logic else ()
                    self.get_location(skill_location(character, skill, level)).access_rule = \
                        lambda state, c=character, k=needed, a=air: state.has(c, player) and \
                        self.playable_missions(state) >= k and (not a or state.has_any(a, player))
                    self.needs[skill_location(character, skill, level)] = (needed, character, (), air)
        if self.escapes:
            for stars in range(4, MAX_ESCAPE_STARS + 1):
                needed = min(pace, stars - 2)
                self.get_location(escape_location(stars)).access_rule = \
                    lambda state, k=needed: self.playable_missions(state) >= k
                self.needs[escape_location(stars)] = (needed, None, (), ())
        if self.weapon_shop == WeaponShop.option_shopsanity:
            for weapon in WEAPONS:
                item = weapon_item(weapon)
                self.get_location(weapon_location(weapon)).access_rule = lambda state, i=item: state.has(i, player)
                self.needs[weapon_location(weapon)] = (0, item, (), ())
        if self.weapon_upgrades == WeaponUpgrades.option_shopsanity:
            for weapon in UPGRADE_WEAPONS:
                upgrades = upgrades_item(weapon)
                required_items = (weapon_item(weapon),) if self.weapon_shop != WeaponShop.option_gifts else ()
                for upgrade in UPGRADES[weapon.id]:
                    self.get_location(upgrade_location(weapon, upgrade)).access_rule = \
                        lambda state, u=upgrades, r=required_items: state.has(u, player) and state.has_all(r, player)
                    self.needs[upgrade_location(weapon, upgrade)] = (0, upgrades, required_items, ())
        if self.options.game_shops:
            for shop in GAME_SHOPS:
                item = game_shop_item(shop)
                for category in shop.categories:
                    location = game_shop_location(shop, category)
                    self.get_location(location).access_rule = lambda state, i=item: state.has(i, player)
                    self.needs[location] = (0, item, (), ())
        if self.money_rules:
            for location, cost in self.purchase_costs().items():
                need = min(max(0, cost - FREE_MONEY), self.money_pool)
                if need <= 0:
                    continue
                self.money_needs[location] = need
                target = self.get_location(location)
                base = target.access_rule
                target.access_rule = lambda state, b=base, n=need: b(state) and self.money_value(state) >= n
        shop_activities = {304: "Clothes", 305: "Customs", 306: "Barber", 307: "Tattoo"}
        for activity in self.activities:
            shop = shop_activities.get(activity.entry) if self.options.game_shops else None
            if shop:
                item = game_shop_item(next(s for s in GAME_SHOPS if s.key == shop))
                self.get_location(activity_location(activity)).access_rule = lambda state, i=item: state.has(i, player)
                self.needs[activity_location(activity)] = (0, item, (), ())
            elif activity.character:
                self.get_location(activity_location(activity)).access_rule = \
                    lambda state, c=activity.character: state.has(c, player)
                self.needs[activity_location(activity)] = (0, activity.character, (), ())
        goal = self.options.goal.value
        if goal == Goal.option_the_third_way:
            self.multiworld.completion_condition[self.player] = self.mission_rule(MISSION_BY_CODE["FINC1"])
        elif goal == Goal.option_finale:
            rules = [self.mission_rule(MISSION_BY_CODE[code]) for code in FINALE_CODES]
            self.multiworld.completion_condition[self.player] = lambda state: any(r(state) for r in rules)
        elif goal == Goal.option_one_mission:
            self.multiworld.completion_condition[self.player] = self.mission_rule(self.goal_mission)
        elif goal == Goal.option_one_collectible:
            self.multiworld.completion_condition[self.player] = self.collectible_rule(
                max(1, pace // 2), self.underwater_logic and self.goal_collectible[0].underwater)
        elif goal == Goal.option_one_check:
            location = self.goal_location
            self.multiworld.completion_condition[self.player] = \
                lambda state: state.can_reach_location(location, player)
        elif goal == Goal.option_check_percentage:
            names = [location.name for location in self.multiworld.get_locations(self.player)]
            needed = self.goal_count
            self.multiworld.completion_condition[self.player] = lambda state: self.reachable_count(state, names) >= needed
        elif goal == Goal.option_free_roam:
            names = self.free_roam_names()
            needed = self.goal_count
            self.multiworld.completion_condition[self.player] = lambda state: self.reachable_count(state, names) >= needed
        else:
            needed = self.goal_mission_count()
            self.multiworld.completion_condition[self.player] = lambda state: self.playable_missions(state) >= needed

    def reachable_count(self, state: CollectionState, names: List[str]) -> int:
        player = self.player
        playable = self.playable_missions(state)
        owned: Dict[str, bool] = {}

        def has(item: str) -> bool:
            if item not in owned:
                owned[item] = state.has(item, player)
            return owned[item]

        count = 0
        money = self.money_value(state) if self.money_needs else 0
        for name in names:
            if self.money_needs.get(name, 0) > money:
                continue
            needed, item, players, triggers = self.needs.get(name, (0, None, (), ()))
            if needed > playable:
                continue
            if item is not None and not has(item):
                continue
            if players and not all(has(p) for p in players):
                continue
            if triggers and not any(has(t) for t in triggers):
                continue
            count += 1
        return count

    def collectible_rule(self, needed: int, underwater: bool, aircraft: bool = False):
        player = self.player
        if underwater:
            items = self.sub_items
            return lambda state: state.has_any(items, player) and self.playable_missions(state) >= needed
        if aircraft:
            items = self.air_items
            return lambda state: state.has_any(items, player) and self.playable_missions(state) >= needed
        return lambda state: self.playable_missions(state) >= needed

    def goal_mission_count(self) -> int:
        if self.options.goal.value != Goal.option_mission_percentage:
            return 0
        return max(1, math.ceil(len(self.missions) * self.options.mission_goal_percentage.value / 100))

    def collectibles_in_world(self) -> int:
        return sum(c.total for c in PICKUPS if self.collectible_steps[c.key] > 0)

    def estimated_minutes(self) -> Dict[str, int]:
        missions = sum(m.minutes for m in self.missions)
        collectibles = sum(c.total * c.minutes for c in PICKUPS if self.collectible_steps[c.key] > 0)
        free_roam = sum(c.total * c.minutes for c in COLLECTIBLES if c.feat and self.collectible_steps[c.key] > 0)
        free_roam += len(self.activities) * ACTIVITY_MINUTES
        free_roam += len(self.landmarks) * LANDMARK_MINUTES + (MAX_ESCAPE_STARS * ESCAPE_MINUTES if self.escapes else 0)
        free_roam += len(CHARACTERS) * len(SKILLS) * len(self.skill_levels()) * SKILL_MINUTES
        medals = self.gold_checks * 6 + self.objective_checks * 2
        shop = len(self.shop_names())
        average = missions / max(1, len(self.missions))
        goal = self.options.goal.value
        if goal == Goal.option_mission_percentage:
            needed = self.goal_mission_count()
        elif goal == Goal.option_finale or goal == Goal.option_the_third_way:
            needed = max(3, len(self.missions) * 3 // 5)
        elif goal == Goal.option_one_mission:
            needed = max(2, len(self.missions) * 2 // 5)
        elif goal == Goal.option_one_collectible:
            needed = 2
        elif goal in (Goal.option_check_percentage, Goal.option_free_roam):
            needed = 0
        else:
            needed = max(2, len(self.missions) // 3)
        goal_minutes = int(needed * average * GOAL_OVERHEAD)
        if goal == Goal.option_one_collectible:
            goal_minutes += 20
        everything = missions + collectibles + medals + free_roam + shop
        total_locations = max(1, len(self.multiworld.get_locations(self.player))) if self.multiworld.regions else 1
        if goal == Goal.option_check_percentage:
            goal_minutes = int(everything * self.goal_count / total_locations * GOAL_OVERHEAD)
        elif goal == Goal.option_free_roam:
            per_check = free_roam / max(1, len(self.free_roam_names()))
            goal_minutes = int(self.goal_count * per_check * GOAL_OVERHEAD)
        return {
            "missions": missions,
            "collectibles": collectibles,
            "medals": medals,
            "free_roam": free_roam,
            "everything": everything,
            "goal": goal_minutes,
        }

    @staticmethod
    def format_minutes(minutes: int) -> str:
        return f"{minutes // 60}h{minutes % 60:02d}"

    def write_spoiler_header(self, spoiler_handle: TextIO) -> None:
        estimate = self.estimated_minutes()
        spoiler_handle.write(f"Missions in the world:           {len(self.missions)}\n")
        if self.goal_mission is not None:
            spoiler_handle.write(f"Goal mission:                    {self.goal_mission.name}\n")
        if self.goal_collectible is not None:
            kind, number = self.goal_collectible
            spoiler_handle.write(f"Goal collectible:                {kind.name} #{number}\n")
        if self.goal_location is not None:
            spoiler_handle.write(f"Goal location:                   {self.goal_location}\n")
        spoiler_handle.write(f"Estimated time to the goal:      {self.format_minutes(estimate['goal'])}\n")
        spoiler_handle.write(f"Estimated time, missions:        {self.format_minutes(estimate['missions'])}\n")
        spoiler_handle.write(f"Estimated time, collectibles:    {self.format_minutes(estimate['collectibles'])}\n")
        spoiler_handle.write(f"Estimated time, medals:          {self.format_minutes(estimate['medals'])}\n")
        spoiler_handle.write(f"Estimated time, free roam:       {self.format_minutes(estimate['free_roam'])}\n")
        spoiler_handle.write(f"Estimated time, every location:  {self.format_minutes(estimate['everything'])} (upper bound)\n")

    def fill_slot_data(self) -> Dict[str, Any]:
        return {
            "version": 2,
            "goal": self.options.goal.value,
            "goal_missions": self.goal_mission_count(),
            "goal_mission": self.goal_mission.code if self.goal_mission is not None else "",
            "goal_collectible": f"{self.goal_collectible[0].key}_{self.goal_collectible[1]:02d}" if self.goal_collectible else "",
            "goal_location": LOCATION_NAME_TO_ID[self.goal_location] if self.goal_location else 0,
            "starting_character": self.start_character,
            "start_position": self.options.start_position.current_key,
            "missions": [m.code for m in self.missions],
            "checks_per_mission": self.options.checks_per_mission.value,
            "gold_medal_checks": self.gold_checks,
            "objective_checks": self.objective_checks,
            "collectible_steps": self.collectible_steps,
            "checks_per_collectible": self.options.checks_per_collectible.value,
            "landmarks": [landmark_id(key) for key, _ in self.landmarks],
            "skill_step": self.skill_step,
            "police_escapes": self.escapes,
            "activities": [activity_id(a) for a in self.activities],
            "goal_count": self.goal_count,
            "weapon_shop": self.weapon_shop,
            "game_shops": int(bool(self.options.game_shops)),
            "weapon_upgrades": self.weapon_upgrades,
            "weapon_price_multiplier": self.options.weapon_price_multiplier.value,
            "collectible_radar": "item" if self.options.collectible_radar == CollectibleRadar.option_item else "start",
            "guaranteed_vehicles": self.vehicles,
            "vehicle_unlocks": self.vehicle_unlocks,
            "mission_traps": bool(self.options.mission_traps),
            "mission_weapons": bool(self.options.mission_weapons),
            "post_goal_chaos": bool(self.options.post_goal_chaos),
            "money_multiplier": self.options.money_multiplier.value,
            "death_link": bool(self.options.death_link.value),
            "estimated_minutes": self.estimated_minutes()["everything"],
            "estimated_goal_minutes": self.estimated_minutes()["goal"],
        }
