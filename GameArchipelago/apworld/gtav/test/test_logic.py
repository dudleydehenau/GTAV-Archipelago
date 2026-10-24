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
