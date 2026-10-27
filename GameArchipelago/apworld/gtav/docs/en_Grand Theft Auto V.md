# Grand Theft Auto V

## Where is the options page?

The [player options page for this game](../player-options) contains all the options you need to configure and
export a config file.

## What does randomization do to this game?

GTA V story mode becomes a sterile open world: the story never moves forward on its own and you start with a single
character, no money and no weapons. Missions are played through the game's own replay system, from markers that only
appear once you have the mission's item and a character who can start it. The 160 hidden collectibles (spaceship
parts, letter scraps, nuclear waste, submarine parts) are all available from the start as floating objects.

## What items and locations get shuffled?

Items: the three characters (one is given at the start), one unlock per mission, money, Weapon Gifts (a random weapon,
online weapons included), Vehicle Gifts (a random vehicle from a list: Deluxo, Oppressor Mk II, Stromberg, helicopters,
a submarine...), the Collectible Radar (the route to the nearest collectible), six guaranteed vehicles (a sports car, a
motorcycle, a helicopter, an underwater vehicle, a boat and the Scramjet) and 53 traps. The helicopter and the
underwater vehicle are placed early; the underwater collectibles need the underwater vehicle in logic.

A mission is in logic when you have its item, a character who can start it, and every character it is played with
(a heist with Michael, Franklin and Trevor needs the three of them).

Locations: the replayable story missions (67; the free-roam preparations are left out because they break as replays) and Strangers and Freaks missions (44, optional), cut down to the
Mission Count you choose, bonus checks per mission, "Gold Medal N" and "Objective N" counters, and collectible
milestones ("Spaceship Parts: at 5 collected": a count of pickups, whichever ones), each worth up to three locations. The high milestones are paced by your mission
progress in logic.

## Which items can be in another player's world?

All of them, including the characters and the mission unlocks.

## What does another world's item look like in GTA V?

Nothing physical: when you pass a mission or pick up a collectible, a notification tells you what you sent and to
whom.

## When the player receives an item, what happens?

A phone-style message, with the slot that sent the item as its contact, shows the item, and the item takes effect: a
new mission marker, a character in the wheel, money, a weapon in your hands, a vehicle in your garage (F6 > Vehicles)
or a boat to call on the water (F6 > Boats). During a mission, weapons are given at once. Traps are only
announced as "a trap" so they stay a surprise; F6 > Recent traps lists them precisely afterwards. Everything stays
in F6 > Archipelago log, with the full list of received items and every check of the multiworld.

## What is the goal?

Depending on the options:

- **finale**: pass one of the three final missions.
- **the_third_way**: pass The Third Way.
- **mission_percentage**: pass a share of the missions of your world, any of them.
- **one_mission**: pass the mission you pick in Goal Mission (a drop-down list), or a surprise one that needs a
  character you do not start with.
- **one_collectible**: pick up one random collectible; the client tells you which one and can route you to it.
- **one_check**: check one random location of your world; the client tells you which one.

When the goal is done, the room's release setting decides whether your remaining items are sent to the others.

With Post-Goal Chaos on, the game goes on after the goal: passed missions come back on the map at random and the
other players' checks can set off traps and gifts in your game.

## How long is a game?

The spoiler log and F6 > Progress show two estimates: the time to reach the goal (the missions it needs, plus 30 %
for unlocks and driving) and an upper bound for every location. Count about 10 minutes per story mission (20 for a
heist and the big finales), 6 per Strangers and Freaks mission and 2 per collectible:

| Settings | To the goal | Every location (upper bound) |
|---|---|---|
| 30 missions, goal 30 % of them | about 1 h 50 | about 11 hours |
| Default (finale, 30 missions, the 160 collectibles) | about 4 hours | about 11 h 30 |
| "Short" preset (20 missions, spaceship parts only) | about 3 hours | about 6 hours |
| "Ten Hours" preset (finale, 80 missions) | about 10 hours | about 21 hours |
| "One Mission" preset (50 missions) | about 4 hours | about 14 h 40 |
| "Everything" preset (111 missions, every check) | about 14 hours | about 31 hours |

Presets: on the website's player options page, pick one in the "Preset" list at the top. In the Archipelago
launcher, "Generate Template Options" writes one YAML per preset in `Players/Templates/Presets` (the Options Creator
does not show presets). They are also in the `presets` folder of the GameArchipelago repository.

To keep a game around an evening, lower Mission Count, set the collectibles you do not want to 0 and use the
"one_mission" goal. With a release at the goal, you do not need to do everything.

## Is it safe with GTA Online?

The mods need BattlEye turned off, and GTA Online cannot be played with BattlEye off. The client also closes the game
if an online session starts while the mods are loaded. Use the setup's "Disable mods" button and turn BattlEye back on
before playing GTA Online. The ScriptHookVDotNet console (F4) and the Native Trainer are turned off by the setup.
