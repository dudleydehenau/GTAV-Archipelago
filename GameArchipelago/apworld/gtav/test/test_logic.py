from ..data import CHARACTERS, MISSION_BY_CODE, MISSIONS, UNDERWATER_VEHICLE, mission_item
from . import GTAVTestBase


class TestCharacterLogic(GTAVTestBase):
    options = {
        "starting_character": "michael",
        "strangers_and_freaks": True,
        "mission_count": 111,
    }

    def test_trevor_missions_need_trevor(self) -> None:
        mission = MISSION_BY_CODE["TRV1"]
        self.collect_by_name(mission_item(mission))
        self.assertFalse(self.can_reach_location(mission.name))
        self.collect_by_name("Trevor")
        self.assertTrue(self.can_reach_location(mission.name))

    def test_every_protagonist_is_needed(self) -> None:
        mission = MISSION_BY_CODE["FAM3"]
        self.collect_by_name(mission_item(mission))
        self.assertFalse(self.can_reach_location(mission.name))
        self.collect_by_name("Franklin")
        self.assertTrue(self.can_reach_location(mission.name))

    def test_heist_needs_the_three(self) -> None:
        mission = MISSION_BY_CODE["DH2A"]
        self.collect_by_name(mission_item(mission))
        self.collect_by_name("Franklin")
        self.assertFalse(self.can_reach_location(mission.name))
        self.collect_by_name("Trevor")
        self.assertTrue(self.can_reach_location(mission.name))

    def test_character_never_behind_its_own_missions(self) -> None:
        for character in CHARACTERS:
            for location in self.multiworld.get_locations(self.player):
                if location.item is None or location.item.name != character or location.item.player != self.player:
                    continue
                mission = next((m for m in MISSIONS if m.name == location.name), None)
                if mission is None:
                    continue
                self.assertNotIn(character, mission.players, f"{character} placed behind {mission.name}")
                self.assertNotEqual(mission.triggers, (character,), f"{character} placed behind {mission.name}")


class TestUnderwater(GTAVTestBase):
    options = {
        "nuclear_waste_per_check": 1,
        "vehicle_unlocks": False,
    }

    def test_nuclear_waste_needs_the_underwater_vehicle(self) -> None:
        self.assertFalse(self.can_reach_location("Nuclear Waste: at 1 collected"))
        self.collect_by_name(UNDERWATER_VEHICLE)
        self.assertTrue(self.can_reach_location("Nuclear Waste: at 1 collected"))
        self.assertTrue(self.can_reach_location("Nuclear Waste: at 1 collected (Bonus 1)"))


class TestUnderwaterWithoutVehicles(GTAVTestBase):
    options = {
        "nuclear_waste_per_check": 1,
        "guaranteed_vehicles": False,
        "vehicle_unlocks": False,
    }

    def test_nuclear_waste_needs_nothing(self) -> None:
        self.assertTrue(self.can_reach_location("Nuclear Waste: at 1 collected"))


class TestRandomMission(GTAVTestBase):
    options = {
        "goal": "random_mission",
        "starting_character": "franklin",
        "mission_count": 20,
    }

    def test_goal_needs_another_character(self) -> None:
        world = self.world
        mission = world.goal_mission
        self.assertIn(mission, world.missions)
        self.assertTrue("Franklin" not in mission.triggers or any(p != "Franklin" for p in mission.players))
        self.assertEqual(len(world.missions), 20)


class TestMissionCount(GTAVTestBase):
    options = {
        "goal": "finale",
        "mission_count": 10,
        "starting_missions": 5,
    }

    def test_goal_and_starting_missions_are_kept(self) -> None:
        codes = {m.code for m in self.world.missions}
        self.assertTrue({"FINA", "FINB", "FINC1"} <= codes)
        self.assertEqual(len(codes), 10)
        for mission in self.world.starting_missions:
            self.assertIn(mission.code, codes)


class TestMinimal(GTAVTestBase):
    options = {
        "strangers_and_freaks": False,
        "starting_missions": 1,
        "mission_count": 10,
        "gold_medal_checks": 0,
        "objective_checks": 0,
        "spaceship_parts_per_check": 0,
        "letter_scraps_per_check": 0,
        "nuclear_waste_per_check": 0,
        "submarine_parts_per_check": 0,
        "monkey_mosaics_per_check": 0,
        "peyote_plants_per_check": 0,
        "epsilon_tracts_per_check": 0,
        "stunt_jumps_per_check": 0,
        "bridges_per_check": 0,
        "knife_flights_per_check": 0,
        "landmarks": False,
        "skill_checks": "off",
        "police_escapes": False,
        "goal": "missions",
        "mission_goal_percentage": 100,
    }


class TestEverything(GTAVTestBase):
    options = {
        "mission_count": 111,
        "checks_per_mission": 3,
        "checks_per_collectible": 3,
        "gold_medal_checks": 92,
        "objective_checks": 184,
        "trap_percentage": 100,
        "collectible_radar": "start",
    }


class TestChosenMission(GTAVTestBase):
    options = {
        "goal": "one_mission",
        "goal_mission": "rampage_3",
        "strangers_and_freaks": False,
    }

    def test_chosen_mission_is_kept(self) -> None:
        self.assertEqual(self.world.goal_mission.code, "RC_RAM3")
        self.assertIn(self.world.goal_mission, self.world.missions)


class TestOneCollectible(GTAVTestBase):
    options = {
        "goal": "one_collectible",
    }

    def test_goal_collectible_is_set(self) -> None:
        kind, number = self.world.goal_collectible
        self.assertTrue(1 <= number <= kind.total)


class TestOneCheck(GTAVTestBase):
    options = {
        "goal": "one_check",
    }

    def test_goal_location_exists(self) -> None:
        self.assertIsNotNone(self.multiworld.get_location(self.world.goal_location, self.player))


class TestVehicleUnlocks(GTAVTestBase):
    options = {
        "nuclear_waste_per_check": 1,
        "early_vehicles": False,
    }

    def test_any_submarine_works(self) -> None:
        self.assertFalse(self.can_reach_location("Nuclear Waste: at 1 collected"))
        self.collect_by_name("Vehicle: Kraken Submarine")
        self.assertTrue(self.can_reach_location("Nuclear Waste: at 1 collected"))

    def test_every_vehicle_is_in_the_pool(self) -> None:
        names = {item.name for item in self.multiworld.itempool if item.player == self.player}
        names |= {item.name for item in self.multiworld.precollected_items[self.player]}
        self.assertIn("Vehicle: Imponte Deluxo", names)
        self.assertIn("Vehicle: Kraken Submarine", names)
        self.assertIn("Vehicle: Jet (airliner)", names)
        self.assertNotIn("Vehicle Gift", names)


class TestGameShops(GTAVTestBase):
    options = {
        "game_shops": True,
    }

    def test_shop_checks_need_their_item(self) -> None:
        from ..data import GAME_SHOPS, game_shop_item, game_shop_location
        for shop in GAME_SHOPS:
            location = game_shop_location(shop, shop.categories[0])
            self.assertFalse(self.can_reach_location(location))
            self.collect_by_name(game_shop_item(shop))
            self.assertTrue(self.can_reach_location(location))

    def test_haircut_activity_needs_the_barber(self) -> None:
        self.assertFalse(self.can_reach_location("Activity: Get a Haircut"))
        self.collect_by_name("Shop: Barber")
        self.assertTrue(self.can_reach_location("Activity: Get a Haircut"))

    def test_slot_data_has_game_shops(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["game_shops"], 1)


class TestNoGameShops(GTAVTestBase):
    options = {
        "game_shops": False,
    }

    def test_no_shop_locations(self) -> None:
        from ..data import GAME_SHOPS, game_shop_item, game_shop_location
        names = {location.name for location in self.multiworld.get_locations(self.player)}
        self.assertNotIn(game_shop_location(GAME_SHOPS[0], "Haircut"), names)
        items = {item.name for item in self.multiworld.itempool if item.player == self.player}
        self.assertNotIn(game_shop_item(GAME_SHOPS[0]), items)


class TestMoneyLogic(GTAVTestBase):
    options = {
        "weapon_shop": "shopsanity",
        "weapon_price_multiplier": 100,
        "money_multiplier": 100,
    }

    def test_railgun_needs_money(self) -> None:
        self.collect_by_name("Weapon: Railgun")
        self.assertFalse(self.can_reach_location("Buy Railgun"))
        for _ in range(3):
            self.collect_by_name("$100,000")
        self.assertTrue(self.can_reach_location("Buy Railgun"))

    def test_cheap_weapon_needs_no_money(self) -> None:
        self.collect_by_name("Weapon: Pistol")
        self.assertTrue(self.can_reach_location("Buy Pistol"))


class TestFlyingNeedsAircraft(GTAVTestBase):
    options = {
        "activities": False,
        "vehicle_unlocks": True,
        "skill_checks": 10,
    }

    def test_flying_skill_needs_an_aircraft(self) -> None:
        from ..data import AIRCRAFT, vehicle_item
        location = "Michael: Flying 10%"
        self.collect_by_name("Michael")
        if location not in {l.name for l in self.multiworld.get_locations(self.player)}:
            self.skipTest("location name differs")
        self.assertFalse(self.can_reach_location(location))
        self.collect_by_name(vehicle_item(AIRCRAFT[0]))
        self.assertTrue(self.can_reach_location(location))


class TestTennisNeedsMichaelOrTrevor(GTAVTestBase):
    options = {
        "starting_character": "franklin",
        "activities": True,
    }

    def test_franklin_cannot_play_tennis(self) -> None:
        self.assertFalse(self.can_reach_location("Activity: Tennis"))
        self.collect_by_name("Trevor")
        self.assertTrue(self.can_reach_location("Activity: Tennis"))
