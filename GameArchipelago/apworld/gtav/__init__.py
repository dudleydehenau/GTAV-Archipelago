import math
from typing import Any, Dict, List, Optional, TextIO, Tuple

from BaseClasses import CollectionState, Item, ItemClassification, Location, Region, Tutorial
from worlds.AutoWorld import WebWorld, World

from .data import (
    CHARACTERS, COLLECTIBLE_MINUTES, COLLECTIBLE_RADAR, GOAL_OVERHEAD, COLLECTIBLES, DRIVE_BY_SMG, FINALE_CODES, GAME, GUARANTEED_VEHICLES,
    HELICOPTER, ITEM_NAME_TO_ID, UNSTABLE_PREPS, LOCATION_NAME_TO_ID, MISSION_BY_CODE, MISSIONS, MONEY, TRAPS, UNDERWATER_VEHICLE,
    VEHICLE_GIFT, WEAPON_GIFT, Collectible, Mission, bonus_location, collectible_bonus_location, collectible_location,
    gold_location, mission_item, objective_location, trap_item,
)
from .options import GOAL_MISSION_CODES, CollectibleRadar, GTAVOptions, Goal, option_groups, option_presets


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
    optional objectives and every hidden collectible are checks. Michael, Franklin and Trevor, the mission unlocks,
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
        "Vehicles": set(GUARANTEED_VEHICLES),
        "Money": {name for name, _ in MONEY},
    }
    location_name_groups = {
        "Story Missions": {m.name for m in MISSIONS if not m.strangers and m.code not in UNSTABLE_PREPS},
        "Strangers and Freaks": {m.name for m in MISSIONS if m.strangers},
        "Collectibles": {name for name in LOCATION_NAME_TO_ID if any(name.startswith(c.name + ":") for c in COLLECTIBLES)},
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
        }
        self.mission_keys = [(mission_item(m), m.players, m.triggers) for m in self.missions]
        self.vehicles = bool(self.options.guaranteed_vehicles)
        self.goal_collectible: Optional[Tuple[Collectible, int]] = None
        if goal == Goal.option_one_collectible:
            kind = self.random.choices(COLLECTIBLES, [c.total for c in COLLECTIBLES])[0]
            self.goal_collectible = (kind, self.random.randint(1, kind.total))
        self.goal_location: Optional[str] = None
        self.underwater_logic = self.vehicles and (any(
            self.collectible_steps[c.key] > 0 for c in COLLECTIBLES if c.underwater)
            or (self.goal_collectible is not None and self.goal_collectible[0].underwater))

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
        city.add_locations(names, GTAVLocation)
        if self.options.goal.value == Goal.option_one_check:
            self.goal_location = self.random.choice(sorted(names))
        self.multiworld.regions += [menu, city]
        menu.connect(city)

    def create_item(self, name: str) -> GTAVItem:
        if name in CHARACTERS or name.startswith("Mission: "):
            classification = ItemClassification.progression
        elif name == UNDERWATER_VEHICLE and getattr(self, "underwater_logic", True):
            classification = ItemClassification.progression
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
        slots = len(self.multiworld.get_unfilled_locations(self.player))
        spare = max(8, math.ceil(len(self.missions) * 0.12))
        while len(required) > max(0, slots - spare):
            self.multiworld.push_precollected(self.create_item(required.pop()))
        pool = [self.create_item(name) for name in required]
        for _ in range(slots - len(pool)):
            pool.append(self.create_item(self.get_filler_item_name()))
        self.multiworld.itempool += pool
        room = self.first_sphere_size() - 4
        for name in (UNDERWATER_VEHICLE, HELICOPTER):
            if name in required and room > 0:
                self.multiworld.early_items[self.player][name] = 1
                room -= 1

    def first_sphere_size(self) -> int:
        size = len(self.starting_missions) * self.options.checks_per_mission.value
        pace = min(20, max(1, len(self.missions) // 2))
        for collectible in COLLECTIBLES:
            if collectible.underwater and self.underwater_logic:
                continue
            for count in self.collectible_counts(collectible):
                if count * pace // collectible.total == 0:
                    size += self.options.checks_per_collectible.value
        return size

    def get_filler_item_name(self) -> str:
        traps = sorted(self.options.enabled_traps.value)
        if traps and self.random.randint(1, 100) <= self.options.trap_percentage.value:
            trap_id = self.random.choice(traps)
            return trap_item(dict(TRAPS)[trap_id])
        weights = [self.options.money_weight.value, self.options.weapon_weight.value, self.options.vehicle_weight.value]
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
        for mission in self.missions:
            rule = self.mission_rule(mission)
            self.get_location(mission.name).access_rule = rule
            for b in range(1, self.options.checks_per_mission.value):
                self.get_location(bonus_location(mission, b)).access_rule = rule
        total = len(self.missions)
        for n in range(1, self.gold_checks + 1):
            needed = min(total, n * 2)
            self.get_location(gold_location(n)).access_rule = lambda state, k=needed: self.playable_missions(state) >= k
        for n in range(1, self.objective_checks + 1):
            needed = min(total, n)
            self.get_location(objective_location(n)).access_rule = \
                lambda state, k=needed: self.playable_missions(state) >= k
        player = self.player
        pace = min(20, max(1, len(self.missions) // 2))
        for collectible in COLLECTIBLES:
            underwater = self.underwater_logic and collectible.underwater
            for count in self.collectible_counts(collectible):
                needed = count * pace // collectible.total
                rule = self.collectible_rule(needed, underwater)
                self.get_location(collectible_location(collectible, count)).access_rule = rule
                for b in range(1, self.options.checks_per_collectible.value):
                    self.get_location(collectible_bonus_location(collectible, count, b)).access_rule = rule
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
        else:
            needed = self.goal_mission_count()
            self.multiworld.completion_condition[self.player] = lambda state: self.playable_missions(state) >= needed

    def collectible_rule(self, needed: int, underwater: bool):
        player = self.player
        if underwater:
            return lambda state: state.has(UNDERWATER_VEHICLE, player) and self.playable_missions(state) >= needed
        return lambda state: self.playable_missions(state) >= needed

    def goal_mission_count(self) -> int:
        if self.options.goal.value != Goal.option_mission_percentage:
            return 0
        return max(1, math.ceil(len(self.missions) * self.options.mission_goal_percentage.value / 100))

    def collectibles_in_world(self) -> int:
        return sum(c.total for c in COLLECTIBLES if self.collectible_steps[c.key] > 0)

    def estimated_minutes(self) -> Dict[str, int]:
        missions = sum(m.minutes for m in self.missions)
        collectibles = self.collectibles_in_world() * COLLECTIBLE_MINUTES
        medals = self.gold_checks * 6 + self.objective_checks * 2
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
        else:
            needed = max(2, len(self.missions) // 3)
        goal_minutes = int(needed * average * GOAL_OVERHEAD)
        if goal == Goal.option_one_collectible:
            goal_minutes += 20
        return {
            "missions": missions,
            "collectibles": collectibles,
            "medals": medals,
            "everything": missions + collectibles + medals,
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
            "missions": [m.code for m in self.missions],
            "checks_per_mission": self.options.checks_per_mission.value,
            "gold_medal_checks": self.gold_checks,
            "objective_checks": self.objective_checks,
            "collectible_steps": self.collectible_steps,
            "checks_per_collectible": self.options.checks_per_collectible.value,
            "collectible_radar": "item" if self.options.collectible_radar == CollectibleRadar.option_item else "start",
            "guaranteed_vehicles": self.vehicles,
            "mission_traps": bool(self.options.mission_traps),
            "mission_weapons": bool(self.options.mission_weapons),
            "post_goal_chaos": bool(self.options.post_goal_chaos),
            "money_multiplier": self.options.money_multiplier.value,
            "death_link": bool(self.options.death_link.value),
            "estimated_minutes": self.estimated_minutes()["everything"],
            "estimated_goal_minutes": self.estimated_minutes()["goal"],
        }
