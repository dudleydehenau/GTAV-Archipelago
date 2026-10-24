# GTA V Archipelago

Play **Grand Theft Auto V** story mode in an [Archipelago](https://archipelago.gg) multiworld randomizer.

Los Santos starts empty: no story, one character, no money. Every mission, Strangers and Freaks mission, gold medal,
optional objective and hidden collectible is a check. Michael, Franklin and Trevor, the mission unlocks, money,
random weapons (online ones included), random fun vehicles (Deluxo, Oppressor Mk II, Stromberg...) and more
than fifty traps are items, shuffled with the other games of the multiworld.

This folder holds the **Archipelago client** (`GameArchipelago`, a ScriptHookVDotNet script) and the
**APWorld** (`gtav.apworld`, for the Archipelago launcher and website).

> [!WARNING]
> **Streamers: turn on stream safe mode before you go live.** One trap (`Irresistible`) turns people into strip club
> dancers who give private dances, and some of these models can show nudity. In game: **F6 > Settings > Stream safe**.
> Even safer: remove `Irresistible` from `enabled_traps` in your options (the "Stream Safe" preset does it).
> The traps are never named in the notifications, so you cannot see one coming.

> [!CAUTION]
> **GTA Online and BattlEye.** These mods are for story mode only. They need BattlEye turned off, and with BattlEye
> off GTA Online refuses to start. The client also closes the game at once if an online session ever begins with
> the mods loaded (`OnlineGuard`). To play GTA Online again, use **Disable mods** in the setup, then turn BattlEye
> back on. Never load mods in GTA Online.

## The mods

| Mod | Role |
|---|---|
| [GameEnvironment](https://github.com/dudleydehenau/GTAV-Archipelago/tree/main/GameEnvironment) | Sterile open world: new game without the story, locked characters, money, weapons, vehicles. |
| [MissionLauncher](https://github.com/dudleydehenau/GTAV-Archipelago/tree/main/MissionLauncher) | Starts any mission with the game's replay system, mission markers, results and medals. |
| [GameTraps](https://github.com/dudleydehenau/GTAV-Archipelago/tree/main/GameTraps) | The 52 traps. |
| [GameCollectibles](https://github.com/dudleydehenau/GTAV-Archipelago/tree/main/GameCollectibles) | The 160 hidden collectibles, available from the start. |
| **GameArchipelago** (this folder) | Connects to the multiworld, gives the items, sends the checks, in-game menu. |
| [GTA V Archipelago Setup](https://github.com/dudleydehenau/GTAV-Archipelago/tree/main/GTAVArchipelagoSetup) | Downloads and installs everything. |

## Requirements

- **GTA V Legacy**, version 1.0.3725 (the Enhanced edition is not supported), on Steam, Epic or the Rockstar Games
  Launcher.
- [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/) for 1.0.3725 (cannot be redistributed: the setup helps you
  install it).
- [Archipelago](https://github.com/ArchipelagoMW/Archipelago/releases) 0.6.4 or newer to generate a game (or a room
  hosted by someone else).
- Windows 10 or 11 with .NET Framework 4.8.

## Quick start

1. **Install the mods.** Download `GTAVArchipelagoSetup` from the
   [releases](https://github.com/dudleydehenau/GTAV-Archipelago/releases), run `GTAVArchipelagoSetup.exe`, check
   that it found your GTA V folder, click **Install / Update**. It downloads ScriptHookVDotNet, LemonUI and the five mods into a new `Archipelago` folder
   of your game, and tells ScriptHookVDotNet to load the scripts from there.
2. **Install ScriptHookV** if the setup says it is missing: download the zip from dev-c.com, click **ScriptHookV...**
   and select it.
3. **Turn BattlEye off.** Rockstar Games Launcher: Settings > Grand Theft Auto V Legacy > uncheck *Enable BattlEye*.
   Steam or Epic: add `-nobattleye` to the launch options.
4. **Get a game.** Put `gtav.apworld` in your Archipelago launcher (the setup copies it to `custom_worlds` when it
   finds the launcher, or double-click the file), write your options file (see [Options](#options)) and generate,
   or join a room someone else generated.
5. **Start GTA V**, load any save, open the Archipelago menu with **F6** > Connection, type the server
   (`archipelago.gg:12345`), your slot name and the password, and **Connect**.
6. **Start a New Game** from the pause menu (Game > New Game). GameEnvironment builds the sterile world with the
   character of your slot. Save it in its own slot.
7. Play. Walk into the mission markers on the map, pick up the glowing collectibles, and press **F6** whenever you
   want to see your progress.

The connection is remembered: next time, the client reconnects by itself (`AutoConnect`).

## How it plays

- **Characters.** You start with one character (`starting_character`). The two others are items. Locked characters
  are greyed out in the character wheel.
- **Missions.** Each mission is an item ("Mission: Hood Safari"). Only the missions you have unlocked, with a
  character who starts them, have a marker on the map, in that character's colour. Walk into it to play the mission
  through the game's own replay system. F6 > Missions tells you what is missing for the others. Passed missions lose
  their marker.
- **Checks.** Passing a mission sends its location (and its bonus locations with `checks_per_mission`). Gold medals
  and optional objectives count across all your missions: "Gold Medal 3" is sent at your third gold, "Objective 10"
  at your tenth objective passed. Collectibles send a check every N pickups of their kind, worth up to three
  locations (`checks_per_collectible`). The kinds that are not checks in your world are removed from the map.
- **Protagonists.** A mission is open only when you have its item, a character who can start it, and every character
  it is played with: a heist with Michael, Franklin and Trevor needs the three of them.
- **Underwater collectibles** need the Underwater Vehicle item in logic (`guaranteed_vehicles`), placed early.
- **Money and gifts.** Money goes to the character you play, weapons to the three characters. A *Weapon Gift* is a random weapon from `weapons.json`
  (online weapons included; a weapon you already have gets ammo). A *Vehicle Gift* is a random vehicle from
  `vehicles.json`: land vehicles and aircraft go to your garage (F6 > Vehicles) and are delivered on the nearest road when you ask; boats
  and submarines go to F6 > Boats and are called in the nearest deep water. A boat left more than 350 m away goes
  back to port by itself. During a mission, a weapon goes straight into your hands (and for good afterwards); a vehicle
  waits in the garage.
- **Messages.** Every item you receive shows a phone-style message whose contact is the slot that sent it. Everything
  stays in F6 > Archipelago log.
- **One game per slot.** Your GTA V game is tied to the slot you connect with. Connecting with another seed or slot
  clears the money, weapons and vehicles of the previous one and gives this slot's items again.
- **Start.** Every character gets the same random starter weapon with 1,000 rounds (`StarterWeapons`), again in
  every new GTA V game. If you play a character that is locked in your Archipelago game, you are switched to an
  unlocked one as soon as you are on foot.
- **Gold medals.** A passed mission keeps a replay marker (with a tick) while gold medal or objective checks are
  left, and loses it afterwards. When a replay starts, a message lists its medal objectives (the
  ones already counted are marked). F6 > Replay for gold lists the passed missions without a gold medal, with the
  objectives still to do, and starts the replay directly.
- **Traps.** Queued and started as soon as the game allows it. With `mission_traps`, the traps that cannot break a
  mission (Mayday, Matrix, Terminator, Walking Dead, Phantom car...) also start during missions. The
  notification only says who sent a trap, the banner says "Trap!" when it starts. F6 > Recent traps lists them
  precisely.
- **Goal.** Pass a final mission, The Third Way, a share of your missions, one chosen (or surprise) mission, pick up
  one random collectible (F6 > Collectibles > Route to your goal), or check one random location (`goal`). F6 >
  Progress shows which one. The goal is sent automatically, and the room's release setting does the rest.
- **After the goal** (`post_goal_chaos`): passed missions come back on the map at random every 10 minutes, and the
  other players' checks can set off traps and gifts in your game.
- **Offline.** If the connection drops, keep playing: your checks are saved and sent when the client reconnects.

## Keys

| Key | Menu |
|---|---|
| **F6** | Archipelago: everything you need. |

The menus of the other mods (F7 missions, F8 environment, F9 traps, F10 collectibles) are turned off, so that only
the Archipelago menu is shown. Each can be turned back on with `"EnableMenu": true` in its `config.json`.

## The Archipelago menu (F6)

| Entry | What it does |
|---|---|
| Status | Connected or offline, last error, seed. |
| Connection | Server, slot name, password, Connect / Disconnect. Kept in `connection.json`. |
| Progress | Goal, estimated length, missions passed (story and Strangers and Freaks), missions available, characters, gold medals, objectives, collectibles per kind (and checks), locations checked, items received (Enter opens the list), checks waiting to be sent. |
| Missions | Every mission of your world: available, passed or locked (and why). Select one to set a waypoint to its start. |
| Recent traps | The last 30 traps with their real name, who sent them, when, and whether they are waiting, running or played. |
| Archipelago log | **Items received**: every item, newest first, with sender, location, time and state (a mission says if it is playable, passed or which character is missing). **My items**: what you sent and received, and to or from whom. **Hints**: every hint about your world or your items, found or still to find. **Whole multiworld**: every check of every player, hints and chat. **Notifications**: the last notifications. |
| Commands | Send a chat message or any command, `!hint`, `!hint_location` (which item is at one of your locations), `!remaining`, `!players`, `!status`, `!release`, `!collect`. |
| Replay for gold | Passed missions without a gold medal, with the objectives to do and the ones done; select one to replay it now. Empty once every gold and objective check is done. |
| Vehicles | Repair the vehicle you are in (free), have one delivered next to you, or pay to repair a destroyed one (it is delivered at once). |
| Boats | Boats and submarines from Vehicle Gifts: call one in the nearest deep water. |
| Collectibles | Count per kind, map markers (none, nearby, all) and a route to the nearest one (needs the Collectible Radar with `collectible_radar: item`). |
| Settings | Stream safe and **Create a bug report**. The rest (DeathLink, traps in missions...) is set in your options file. |

## Items

| Item | Count | Effect |
|---|---|---|
| Michael, Franklin, Trevor | 1 each (one is your start) | Unlocks the character. |
| Mission: *name* | 1 per mission | Unlocks the mission (67 story, 44 Strangers and Freaks). |
| $5,000, $20,000, $100,000 | filler | Money (`money_multiplier`). |
| Weapon Gift | filler | A random weapon from `weapons.json` (72 by default, online weapons included: railgun, Up-n-Atomizer, Widowmaker, Heavy Rifle, Mk II weapons...). |
| Sports Car, Motorcycle, Helicopter, Underwater Vehicle, Boat, Scramjet | 1 each with `guaranteed_vehicles` | A vehicle of that `Category` in `vehicles.json`. The helicopter and the underwater vehicle are placed early. |
| Drive-By SMG | start inventory with `starting_drive_by_weapon` | A Micro SMG with 600 rounds for the three characters. |
| Collectible Radar | 1 with `collectible_radar: item` | Unlocks F6 > Collectibles > Route to the nearest one. |
| Vehicle Gift | filler | A random vehicle from `vehicles.json` (39 by default: Deluxo, Oppressor Mk II, Z-Type, JB 700, Stromberg, Toreador, Shotaro, Vigilante, Scramjet, Ruiner 2000, Rocket Voltic, tanks, Sea Sparrow, Akula, Hydra, Kraken submarine, Seashark...). |
| Trap: *name* | filler | One of the 52 [traps](https://github.com/dudleydehenau/GTAV-Archipelago/tree/main/GameTraps#the-traps). |

## Locations

| Location | Count | Sent when |
|---|---|---|
| *Mission name* | 67 story + 44 Strangers and Freaks | You pass the mission. The free-roam preparations (Blitz Play masks, boiler suits, garbage and tow trucks, Bureau Raid fire truck, Big Score getaway vehicle, driller and Gauntlets, Minisub) are left out: they wait for a vehicle the story places in the world and do nothing as a replay. Father/Son is left out too: its switch to Franklin fails in the sterile game. |
| *Mission name* (Bonus 1), (Bonus 2) | with `checks_per_mission` 2 or 3 | Same time as the mission. |
| Gold Medal N | `gold_medal_checks` | Your Nth different mission with a gold medal. |
| Objective N | `objective_checks` | Your Nth different optional objective passed. |
| Spaceship Parts: at N collected, Letter Scraps: at N collected | up to 50 each | You have picked up N of them, whichever ones (the number is a count, not the number of one object). |
| Nuclear Waste: at N collected, Submarine Parts: at N collected | up to 30 each | Same, underwater (Underwater Vehicle in logic). |
| *Collectible milestone* (Bonus 1), (Bonus 2) | with `checks_per_collectible` 2 or 3 | Same time as the milestone. |

## Options

| Option | Default | Values | Description |
|---|---|---|---|
| `goal` | `finale` | `finale`, `the_third_way`, `mission_percentage`, `one_mission`, `one_collectible`, `one_check` | Pass a final mission; pass The Third Way; pass a share of your missions; pass the mission chosen in `goal_mission`; pick up one random collectible; check one random location of your world. |
| `goal_mission` | `surprise` | `surprise` or a mission name (`the_paleto_score`, `rampage_3`...) | The mission of the `one_mission` goal. `surprise` picks one that needs a character you do not start with. |
| `mission_goal_percentage` | `70` | 10 to 100 | Percentage of missions for the `missions` goal. |
| `mission_count` | `30` | 10 to 111 | Missions in your world, picked at random (the goal missions are always kept). |
| `starting_character` | `random` | `michael`, `franklin`, `trevor`, `random` | Your first character. |
| `starting_missions` | `3` | 1 to 10 | Missions of your starting character unlocked from the start. |
| `strangers_and_freaks` | `true` | `true`, `false` | Adds the 44 replayable Strangers and Freaks missions. |
| `checks_per_mission` | `1` | 1 to 3 | Locations per mission (more room for items without more tasks). |
| `gold_medal_checks` | `5` | 0 to 92 | Number of Gold Medal locations (at most 3/4 of your missions). |
| `objective_checks` | `20` | 0 to 184 | Number of Objective locations (at most 1.5 per mission). |
| `spaceship_parts_per_check` | `1` | 0 to 50 | Pickups per check (0 removes them). 5 means 10 checks: at 5 collected, at 10 collected... |
| `letter_scraps_per_check` | `1` | 0 to 50 | Same for letter scraps. |
| `nuclear_waste_per_check` | `1` | 0 to 30 | Same for nuclear waste (underwater). |
| `submarine_parts_per_check` | `1` | 0 to 30 | Same for submarine parts (underwater). |
| `checks_per_collectible` | `2` | 1 to 3 | Locations each collectible milestone is worth. |
| `collectible_radar` | `item` | `start`, `item` | Whether the route to the nearest collectible is given at the start or is an item. |
| `guaranteed_vehicles` | `true` | `true`, `false` | Adds the six guaranteed vehicles; the underwater collectibles then need the Underwater Vehicle. |
| `starting_drive_by_weapon` | `true` | `true`, `false` | Start with a Micro SMG (600 rounds, all characters) that can be fired from a car. |
| `mission_weapons` | `false` | `true`, `false` | Each mission starts with a basic weapon kit, taken back afterwards. Off by default: you play with the weapons you received. |
| `mission_traps` | `true` | `true`, `false` | Traps that cannot break a mission can start during one. |
| `post_goal_chaos` | `true` | `true`, `false` | After the goal: missions come back at random, the others' checks set off traps and gifts. |
| `trap_percentage` | `30` | 0 to 100 | Share of the filler items that are traps. |
| `enabled_traps` | all | trap ids | The traps that can appear. |
| `money_weight`, `weapon_weight`, `vehicle_weight` | `3`, `6`, `2` | 0 to 10 | Mix of the other filler items. |
| `money_multiplier` | `100` | 10 to 500 | Percentage applied to every money item. |
| `death_link` | `false` | `true`, `false` | When you die, everyone with DeathLink dies, and the other way round. |

Presets (website: "Preset" list at the top of the player options page; launcher: "Generate Template Options" writes
them to `Players/Templates/Presets`; also in the `presets` folder here): **Ten Hours** (finale, 80
missions, about 10 hours to the goal), **Short** (20 missions, spaceship parts only, about 6 h 30), **One Mission** (50 missions
and a surprise mission as the goal), **Everything** (every mission, two checks per mission, 40 gold medals, 120 objectives,
every collectible, about 40 hours), **Stream Safe** (no `Irresistible`).

Length: count about 10 minutes per story mission (20 for a heist), 6 per Strangers and Freaks mission and 2 per
collectible. F6 > Progress and the spoiler log show two estimates: the time to the goal (the missions it needs, plus
30 % for unlocks and driving) and the upper bound for every location. With the default options (finale, 30 missions) the goal
takes about 4 hours and every location about 11 h 30; 30 % of 30 missions takes under 2 hours.

Balancing tips: the host can make a slot lighter or heavier without touching the others. Collectibles "worth half
a check" (2 pickups per check) or missions "worth two checks" (`checks_per_mission: 2`) both work. With few
locations, extra mission unlocks go to your starting inventory instead of failing.

```yaml
name: YourName
game: Grand Theft Auto V
Grand Theft Auto V:
  goal: finale
  mission_count: 30
  starting_character: random
  starting_missions: 3
  strangers_and_freaks: true
  checks_per_mission: 1
  gold_medal_checks: 5
  objective_checks: 20
  spaceship_parts_per_check: 1
  letter_scraps_per_check: 1
  nuclear_waste_per_check: 2
  submarine_parts_per_check: 2
  checks_per_collectible: 2
  collectible_radar: item
  guaranteed_vehicles: true
  mission_traps: true
  post_goal_chaos: true
  trap_percentage: 30
  money_multiplier: 100
  death_link: false
```

## Client configuration

`Archipelago/GameArchipelago/config.json`:

| Setting | Default | Description |
|---|---|---|
| `EnableMenu` | `true` | Shows the F6 menu. |
| `MenuKey` | `"F6"` | Key of the menu ([key names](https://learn.microsoft.com/dotnet/api/system.windows.forms.keys)). |
| `VerboseLog` | `false` | More details in `GameArchipelago.log`. |
| `AutoConnect` | `true` | Connects with the last settings when the game loads, and reconnects after a drop. |
| `ShowReceived` | `true` | Notifications for received items (traps are never named). |
| `ShowSent` | `true` | Notifications for the items you send. |
| `ShowChat` | `true` | Chat messages as notifications. |
| `ShowHints` | `true` | Hints about your world as notifications. |
| `NotificationDelay` | `900` | Milliseconds between two notifications. |
| `LogSize` | `80` | Lines kept in the message log. |
| `OnlineGuard` | `true` | Closes the game if a GTA Online session starts with the mods loaded. |
| `StarterWeapons` | 10 pistols and SMGs | The starter weapon is picked from this list (same pick for a given seed and slot). |
| `StarterAmmo` | `1000` | Rounds given with the starter weapon. |
| `MissionKit` | 6 weapons | Weapons given at the start of every mission attempt with Mission Weapons on (`"WEAPON_NAME:ammo"`); the ones you did not own are taken back after the mission. |
| `MissionExtras` | sticky bombs for ASS1, ASS2, ASS5 | Extra weapons for one mission, by mission code. |
| `FeedSize` | `1500` | Lines kept in F6 > Archipelago log > My items and Whole multiworld (saved with your game). |

## Files

| File | Content |
|---|---|
| `GameArchipelago/config.json` | Settings above. |
| `GameArchipelago/vehicles.json` | Vehicles given by Vehicle Gifts: `Name`, `Model` (game model name), `Weight` (chance), `Water` (`true` for boats and submarines), `Category` (`Sports Car`, `Motorcycle`, `Helicopter`, `Underwater Vehicle`, `Boat`, `Scramjet` for the guaranteed vehicle items, empty otherwise). |
| `GameArchipelago/weapons.json` | Weapons given by Weapon Gifts: `Name`, `Weapon` (`WEAPON_...` name, or `ARMOR`), `Ammo`, `Weight`. |
| `GameArchipelago/connection.json` | Server, slot name and password (stored as plain text) and the last game. |
| `GameArchipelago/saves/<seed>_<slot>.json` | Your progress for one multiworld: items, checks, medals, objectives, collectibles, trap history. Keep it until the multiworld is over. |
| `GameArchipelago/history/<seed>_<slot>.log` | Event history of that multiworld (see below). |
| `GameArchipelago/reports/` | Bug reports. |
| `GameArchipelago/GameArchipelago.log` | Technical log. |

Each multiworld has its own save, so you can play several seeds. Starting a new GTA V game for the same seed is
fine: the client notices it and gives your money, weapons and vehicles again.

## Event history and bug reports

Every event of a multiworld is written, one line each, to `GameArchipelago/history/<seed>_<slot>.log`:

| Event | Fields |
|---|---|
| `SESSION` | The save loaded, its counters and the version of every mod. |
| `CONNECT`, `DISCONNECT` | Server, slot, seed, every slot option, checks the server already has, checks waiting. |
| `ITEM` | Index, item id and name, sender, the location it came from. |
| `GIFT`, `GIFT_PICK`, `MISSION_GIFT` | Money, weapon or vehicle given (or the error), the pick behind a gift, gifts given during a mission. |
| `REPLAY_ON`, `REPLAY_OFF` | A replay started or ended (items and gifts at that moment). |
| `BOAT`, `BOAT_HOME` | A boat called, a boat sent back to port. |
| `COLLECT_ALL` | The collectibles of a kind marked as collected because all its checks are done (after a release). |
| `CHAOS_MISSIONS`, `CHAOS_TRAP`, `CHAOS_GIFT` | Post-goal chaos: missions put back on the map, traps and gifts from other players' checks. |
| `TRAP`, `TRAP_START`, `TRAP_END` | Trap received (id, sender, queued or skipped), started, finished or interrupted. |
| `REPLAY`, `MISSION` | Mission started; every attempt with its outcome, medal, completion, time, objectives and fail reason. |
| `CHECK` | Location ids and names sent, and whether the client was online. |
| `COLLECT` | Collectible picked up. |
| `ROSTER`, `GAME`, `SWITCH` | Characters unlocked in the game, new GTA V game detected, automatic character switch. |
| `MISSION_KIT`, `MISSION_KIT_BACK` | Mission weapon kit given and taken back. |
| `NEW_GAME` | The game was handed over to another slot (previous items cleared). |
| `STARTER`, `OBJECTIVES` | Starter weapon given, medal objectives shown at the start of a replay. |
| `DEATH_IN`, `DEATH_OUT`, `GOAL`, `SAY`, `ERROR` | DeathLink, goal, commands sent, errors. |

**F6 > Settings > Create a bug report** zips that history, your Archipelago save, the settings, state files and logs of
every mod, and the ScriptHookV / ScriptHookVDotNet logs into `GameArchipelago/reports/report-<date>.zip` (the
password in `connection.json` is never included). With it, the exact state of the player can be rebuilt: put the
save and the state files back in place and replay the history.

## Troubleshooting

- **Nothing happens when I press F6.** ScriptHookV or ScriptHookVDotNet is not loaded: check BattlEye is off, that
  `ScriptHookV.dll`, `dinput8.dll` and `ScriptHookVDotNet.asi` are next to `GTA5.exe`, and that
  `ScriptHookVDotNet.ini` contains `ScriptsLocation="Archipelago"`. The setup turns the ScriptHookVDotNet console
  off (`ConsoleKeyBinding=None`); for debugging only, set it back to `F4` by hand.
- **"Connect to Archipelago first".** Missions and collectibles only count once a game is loaded: connect once.
- **Missions do not show on the map.** You need the mission item *and* one of its characters, and a sterile game
  (New Game with GameEnvironment). F6 > Missions tells you what is missing.
- **I play a locked character.** Switch to an unlocked one with the character wheel.
- **My other scripts stopped loading.** ScriptHookVDotNet now loads the `Archipelago` folder instead of `scripts`.
  Move the scripts you want into `Archipelago`.

## Building

- **Client:** open `GameArchipelago.sln` in Visual Studio 2022 (.NET Framework 4.8, C# 8). Put
  `ScriptHookVDotNet3.dll`, `LemonUI.SHVDN3.dll`, `MissionLauncher.dll`, `GameEnvironment.dll`, `GameTraps.dll` and
  `GameCollectibles.dll` in `lib`. `Archipelago.MultiClient.Net` comes from NuGet. The output contains
  `GameArchipelago.dll`, `Archipelago.MultiClient.Net.dll`, `Newtonsoft.Json.dll` and `GameArchipelago/config.json`.
- **APWorld:** `python tools/build_apworld.py` writes `apworld/gtav.apworld` from `apworld/gtav`.
- **Shared data:** items, locations and missions are defined once in `apworld/gtav/data.py`.
  `python tools/gen_client.py` regenerates `src/Core/ApData.Generated.cs` from it (and from `tools/missions.json`,
  the mission start points of MissionLauncher). Never change IDs of a released version.

## Release layout

The setup expects one zip per mod at the root of each GitHub release, with the build output inside
(for example `GameArchipelago.dll`, `Archipelago.MultiClient.Net.dll`, `Newtonsoft.Json.dll`,
`GameArchipelago/config.json`), and `gtav.apworld` as a separate file of the GameArchipelago release.

## Credits

Built with [Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net),
[ScriptHookVDotNet](https://github.com/scripthookvdotnet/scripthookvdotnet) and
[LemonUI](https://github.com/LemonUIbyLemon/LemonUI). Mission data comes from the game's own scripts.
GTA V is a trademark of Rockstar Games; this project is not affiliated with Rockstar Games or Take-Two.
