# Changelog

## 1.8.0

- Free-roam checks (APWorld 1.9.0, GameCollectibles 2.0.0): stunt jumps, Under the Bridge, knife flights, 44 landmarks
  with purple map markers, skill levels of the three characters and police escapes.
- New collectibles: 50 monkey mosaics, 27 peyote plants and 10 Epsilon tracts.
- Ammu-Nation (APWorld options `weapon_shop`, `weapon_upgrades`, `weapon_price_multiplier`): each weapon can be an
  item that lets you buy it ("Weapon: Pistol"), and buying it the first time a check ("Buy Pistol"). The same for the
  upgrades of each weapon ("Upgrades: Pistol", 267 upgrade checks, off by default). With the shop on, there are no
  Weapon Gifts.
- Game shops (APWorld option `game_shops`, on by default): 29 checks for the first purchase of each category.
  Barber: haircut, beard. Clothes Store: tops, pants, shoes, hats, glasses, outfits, masks. Tattoo Parlor: head,
  torso, each arm, each leg. Los Santos Customs: engine, brakes, transmission, suspension, armor, turbo, horn,
  headlights, wheels, tires, respray, windows, plate, body kit. Each kind of shop is closed until its item
  ("Shop: Barber", "Shop: Clothes Store", "Shop: Tattoo Parlor", "Shop: Los Santos Customs"). F6 > Free Roam lists
  what is left.
- Money in logic: a purchase that costs more than $2,000 (weapons, upgrades, Los Santos Customs, after the price
  multiplier) needs at least its price in money items found. Enough money items are added to the pool to pay for every
  purchase check, as far as the pool has room. "Get a Haircut", "Buy Clothes", "Get a Tattoo" and "Modify a Car" need
  their shop item. Flying skill checks need an aircraft when Flight School is not in the world.
- Finished activities get their own tick (each race, not only when the whole group is done).
- Vehicles one by one (APWorld option `vehicle_unlocks`, on by default): each of the 62 vehicles is its own item
  ("Vehicle: Imponte Deluxo"), received once and kept in the garage. Bridges then need any plane or helicopter in
  logic, underwater checks any submarine. Vehicle Gifts (with `vehicle_unlocks` off) no longer give a vehicle you
  already have until you own the whole list.
- F6 > Vehicles > Put your vehicle in the garage: stores the closest of your vehicles right away.
- Activities (APWorld option `activities`): 52 checks. The 6 off-road races, 5 street races (Franklin), 4 sea races,
  3 triathlons, 12 Flight School lessons, the stunt plane time trials, 6 shooting range categories, tennis, golf,
  darts, yoga (Michael), base jumping, skydiving, and small things (a movie, the cable car, the car wash, clothes, a
  car mod, a haircut, a tattoo, TV, a store hold-up). All are opened from the start by GameEnvironment. F6 > Free
  Roam lists the ones left. `trevor_jobs` has no effect in this version: Trevor's bail bonds, trafficking runs and
  hunting need story scripts the empty world does not run.
- `early_vehicles`: with `vehicle_unlocks`, a small plane and a submarine are placed early in the run.
- Street races need Franklin and yoga needs Michael in logic: the game only lets them do these.
- `start_position` (`home`, `city`, `anywhere`, default `anywhere`): where a new game puts you. `city` is Legion
  Square, downtown Los Santos.
- F6 > Map: show or hide each collectible kind, the landmarks to visit and the game's icons for each activity and
  shop (golf, tennis, races, Flight School, barbers, LS Customs, Ammu-Nation...), on the map and the minimap.
- Finished activities disappear from the map. F6 > Map > Show finished places keeps them, with a tick. Groups with
  several checks (races, the shooting range, Flight School) disappear once all of them are done.
- Ammu-Nation's rules are linked even before you connect: nothing can be bought while you are not connected, so no
  check is ever lost.
- New goals: `check_percentage` (a share of all your locations) and `free_roam` (a number of free-roam checks).
- 23 planes added to the Vehicle Gifts (`vehicles.json`): the Mallard stunt plane, the Velum, the Duster, the Cuban
  800, the Ultralight, the two blimps, the LAZER, the gold Luxor Deluxe, business jets, the airliner, the cargo plane,
  the Titan...
- F6 > Free Roam: progress of every free-roam check and a route to the nearest landmark.
- Worlds made with an older APWorld keep working: the new checks are simply not in them.
- A connection to archipelago.gg that times out now says to open the room page (it wakes the room up) and to check
  the port, which changes when the room restarts.

## 1.7.5

- New trap item: Trap: Boing Boing (APWorld 1.8.0).
- Each part of the client runs on its own: an error in one of them (markers, sync, death watch...) no longer stops
  the messages from showing. Errors name the part that failed in the log and in the history.
- When item messages have been waiting for 30 s, the log says why (loading screen, replay, character switch...).
- With `VerboseLog`, every message shown is written to the log.
- `TrapQueueLimit` is now 10: up to 10 traps can wait in line before new ones are skipped (was 5).

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
