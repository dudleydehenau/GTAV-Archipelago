# Changelog

## 1.7.2

- Father/Son is left out of new worlds: its scripted switch from Michael to Franklin fails in the sterile game (you
  keep Michael's model with Franklin's voice, and a second Franklin stands outside the car), with every mod idle.

## 1.7.1

- Received items are announced once you are back in the game: not during a loading screen, a fade, the mission
  results, or the end of a replay (the scripts reload there and the messages were lost). Nothing is skipped: the
  messages wait and show 2 s after the game is back.

## 1.7.0

- F6 > Archipelago log > Hints: every hint about your world or your items, newest first, found or still to find.
- Collectible locations are renamed "Nuclear Waste: at 6 collected" (it was "Nuclear Waste 6/30", which looked like the
  number of one object): the check is sent when you have picked up that many, whichever ones. New seeds only.
- Vehicles received during a mission are no longer dropped next to you; like every vehicle they wait in the garage.

## 1.6.4

- Passed missions without a gold medal stay on the map as replay markers (with a tick) while gold medal or objective
  checks are left, so there is always something to do on the map.
- When every mission you own is done, a message says so and what to do next (collectibles, gold medals, waiting for
  items).

## 1.6.3

- Mission markers are checked against MissionLauncher every second and set again if they were lost (they could stay
  hidden after a mission). Every change in the number of markers shown is written to the history (`MARKERS`).

## 1.6.2

- New preset "Ten Hours" (finale, 80 missions with Strangers and Freaks, about 10 hours to the goal), and the preset
  YAML files in `presets`.

## 1.6.1

- Time estimates recalibrated (10 minutes per story mission, 20 per heist, 6 per Strangers and Freaks mission) and
  split in two: F6 > Progress shows the time to the goal and, separately, the upper bound for every location
  (missions, medals and the 160 collectibles). 9 missions out of 30 now show about 1 h 50 instead of 12 hours.

## 1.6.0

- Starting Drive-By Weapon (option, on by default): a Micro SMG with 600 rounds for the three characters in your
  start inventory, so the chases and drive-bys are always possible. The random starter weapon is now always a
  pistol or an SMG, which can be fired from a car.
- F6 > Vehicles: "Repair the vehicle you are in" (garage vehicles and boats, free), and repairing a destroyed vehicle
  delivers it right away.

## 1.5.0

- Each GTA V game belongs to one Archipelago slot: connecting with another seed or slot clears the money, weapons and
  vehicles of the previous one, then gives this slot's items again (a vehicle from an older game no longer shows up
  in your garage).

## 1.4.1

- Weapon Gifts go to the three characters, and they are now the most common filler (`weapon_weight` 6,
  `money_weight` 3). Mission Weapons is off by default: you play with what you received.

## 1.4.0

- Mission Weapons (option, on by default): each mission starts with a basic kit (pistol, SMG, carbine rifle,
  shotgun, sniper rifle, grenades; sticky bombs for some assassinations), taken back after the mission. The story
  expects you to own weapons by then. The kit is set in `config.json` (`MissionKit`, `MissionExtras`).
- The save is written at most every 2 s (bursts of messages from a busy multiworld used to write it every frame).
- Saves get the starter ammo once more.

## 1.3.1

- The Cayo Perico getaway trap is gone (52 traps): the island does not load properly in story mode.
- Saves whose starter weapon lost its ammo get the starter ammo once more.
- Objectives without a real target no longer show "(at least 0.0s)".

## 1.3.0

- Goals reworked and explained in the options: `finale`, `the_third_way`, `mission_percentage` (was `missions`),
  `one_mission` (was `random_mission`, with the new **Goal Mission** drop-down to pick the mission, or `surprise`),
  and two silly ones: `one_collectible` (one random collectible ends your game, F6 > Collectibles routes you to it)
  and `one_check` (one random location of your world ends it). The old names still work in YAML files.
- The early helicopter and underwater vehicle no longer take the only first locations of a very small world (it could
  make the generation fail).

## 1.2.0

- The free-roam preparations (Blitz Play garbage truck, tow truck, masks and boiler suits, Bureau Raid fire truck,
  Big Score getaway vehicle, driller and Gauntlets, Minisub) are no longer in the worlds: as replays they wait for a
  vehicle the story never places, and nothing happens.
- Gold medals: when a replay starts, a message lists the medal objectives (and which ones are already counted);
  F6 > Replay for gold shows the objectives to do and the ones done.
- F6 > Archipelago log: every item received (with its state), your items sent and received, every check of the
  whole multiworld, and the notifications. F6 > Progress > Items received opens the full list.
- Messages: the contact shown is the slot that sent the item.
- Starter weapon: a random weapon with 1,000 rounds for every character at the start of each new game.
- Playing a locked character switches you to an unlocked one automatically.
- "Cancel waiting traps" is gone.

## 1.1.0

- A mission with several protagonists needs all of them unlocked (logic and in game): no more duplicate Michael.
- New options: Mission Count (with a time estimate in the spoiler log and in F6 > Progress), the "random_mission"
  goal (always needs a character you do not start with), Checks per Collectible, Collectible Radar (the route to the
  nearest collectible becomes an item), Guaranteed Vehicles (sports car, motorcycle, helicopter, underwater vehicle,
  boat and Scramjet; the helicopter and the underwater vehicle come early), Traps in Missions and Post-Goal Chaos.
- High collectible milestones are paced by mission progress in logic, so important items are not stuck behind
  "Spaceship Parts 40/50".
- Every received item shows a phone message from the "Archipelago" contact with the slot that sent it. Bursts are
  summed up instead of lost; everything stays in F6 > Message log.
- Gifts received during a mission are given at once (the weapon in your hands, the vehicle next to you), and again
  for good after the mission.
- After a release, the collectibles whose checks are all done are removed from the map; the kinds that are not checks
  in your world are hidden. F6 > Replay for gold is empty once every gold and objective check is done.
- Boats and submarines go back to port when you leave them more than 350 m away.
- F6 > Settings only keeps "Stream safe" and "Create a bug report": DeathLink and the other gameplay settings come
  from the seed. F6 > Commands: the countdown is gone and "Hint a location" is explained.
- After the goal (Post-Goal Chaos): passed missions come back on the map at random, and the other players' checks can
  set off traps and gifts in your game.

## 1.0.0

- First release: Archipelago client for GTA V story mode and the `gtav` APWorld.
- Items: characters, mission unlocks, Kraken Submarine, money, weapons, vehicles, 52 traps.
- Locations: missions (story and Strangers and Freaks), bonus checks, gold medals, objectives, collectible milestones.
- F6 menu: connection, progress, missions with waypoints, recent traps, message log, commands, settings.
- Notifications with the sender of every item; traps are never named.
- Offline progress, automatic reconnection, DeathLink, goal reporting, GTA Online guard.
- Vehicles and collectibles in the F6 menu; the other mods' menus are off by default.
- Event history per multiworld and bug reports (F6 > Settings).
- Weapon Gifts and Vehicle Gifts: random picks from `weapons.json` and `vehicles.json` (online weapons, fun vehicles,
  boats and submarines in F6 > Boats); the Kraken Submarine item and its logic are gone.
- Passed missions lose their marker; F6 > Replay for gold lists the missions still missing a gold medal.
