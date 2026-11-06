from ..data import BASE_ID, HELICOPTER, LANDMARKS, LOCATION_NAME_TO_ID, landmark_location, skill_location
from . import GTAVTestBase


class TestFreeRoamDefaults(GTAVTestBase):
    options = {
        "starting_character": "michael",
        "early_vehicles": False,
    }

    def test_landmarks_are_locations(self) -> None:
        names = {location.name for location in self.multiworld.get_locations(self.player)}
        for _, name in LANDMARKS:
            self.assertIn(landmark_location(name), names)

    def test_skills_every_ten_percent(self) -> None:
        names = {location.name for location in self.multiworld.get_locations(self.player)}
        self.assertIn(skill_location("Trevor", "Lung Capacity", 10), names)
        self.assertIn(skill_location("Franklin", "Driving", 100), names)

    def test_trevor_skills_need_trevor(self) -> None:
        self.assertFalse(self.can_reach_location(skill_location("Trevor", "Stamina", 10)))
        self.collect_by_name("Trevor")
        self.assertTrue(self.can_reach_location(skill_location("Trevor", "Stamina", 10)))

    def test_bridges_need_the_helicopter(self) -> None:
        self.assertFalse(self.can_reach_location("Under the Bridge: 1 flown"))
        self.collect_by_name("Vehicle: Mammoth Dodo")
        self.assertTrue(self.can_reach_location("Under the Bridge: 1 flown"))

    def test_location_ids_do_not_overlap(self) -> None:
        self.assertEqual(len(set(LOCATION_NAME_TO_ID.values())), len(LOCATION_NAME_TO_ID))
        self.assertTrue(all(BASE_ID < value < BASE_ID + 20000 for value in LOCATION_NAME_TO_ID.values()))


class TestSkillSteps(GTAVTestBase):
    options = {
        "skill_checks": "every_50",
    }

    def test_only_fifty_and_hundred(self) -> None:
        names = {location.name for location in self.multiworld.get_locations(self.player)}
        self.assertIn(skill_location("Michael", "Shooting", 50), names)
        self.assertIn(skill_location("Michael", "Shooting", 100), names)
        self.assertNotIn(skill_location("Michael", "Shooting", 40), names)


class TestFreeRoamGoal(GTAVTestBase):
    options = {
        "goal": "free_roam",
        "goal_free_roam_checks": 80,
    }

    def test_goal_count(self) -> None:
        self.assertEqual(self.world.goal_count, 80)
        self.assertEqual(self.world.fill_slot_data()["goal_count"], 80)


class TestCheckPercentageGoal(GTAVTestBase):
    options = {
        "goal": "check_percentage",
        "goal_check_percentage": 40,
    }

    def test_goal_count(self) -> None:
        total = len(self.multiworld.get_locations(self.player))
        self.assertTrue(0 < self.world.goal_count <= total)


class TestFreeRoamOnly(GTAVTestBase):
    options = {
        "goal": "free_roam",
        "goal_free_roam_checks": 500,
        "mission_count": 10,
        "strangers_and_freaks": False,
        "gold_medal_checks": 0,
        "objective_checks": 0,
        "guaranteed_vehicles": False,
        "skill_checks": "only_100",
        "checks_per_collectible": 1,
    }

    def test_goal_capped(self) -> None:
        self.assertEqual(self.world.goal_count, len(self.world.free_roam_names()))


class TestShopsanity(GTAVTestBase):
    options = {
        "weapon_shop": "shopsanity",
        "weapon_upgrades": "shopsanity",
    }

    def test_buying_needs_the_weapon(self) -> None:
        self.assertFalse(self.can_reach_location("Buy Pistol"))
        self.collect_by_name("Weapon: Pistol")
        self.assertTrue(self.can_reach_location("Buy Pistol"))

    def test_upgrade_needs_both(self) -> None:
        self.collect_by_name("Upgrades: Pistol")
        self.assertFalse(self.can_reach_location("Buy Pistol: Suppressor"))
        self.collect_by_name("Weapon: Pistol")
        self.collect_by_name("$5,000")
        self.assertTrue(self.can_reach_location("Buy Pistol: Suppressor"))


class TestWeaponGifts(GTAVTestBase):
    options = {
        "weapon_shop": "gifts",
    }

    def test_no_shop_locations(self) -> None:
        names = {location.name for location in self.multiworld.get_locations(self.player)}
        self.assertNotIn("Buy Pistol", names)


class TestActivities(GTAVTestBase):
    options = {
        "trevor_jobs": True,
        "starting_character": "michael",
    }

    def test_activities_without_trevor_jobs(self) -> None:
        self.assertTrue(self.can_reach_location("Activity: Golf"))
        names = {location.name for location in self.multiworld.get_locations(self.player)}
        self.assertNotIn("Activity: Bail Bond 1", names)
        self.assertNotIn("Activity: Hunting", names)

    def test_early_vehicles(self) -> None:
        early = self.multiworld.early_items[self.player]
        self.assertTrue(any(name.startswith("Vehicle: ") for name in early))
        start = {item.name for item in self.multiworld.precollected_items[self.player]}
        self.assertFalse(any(name.startswith("Vehicle: ") for name in start))
