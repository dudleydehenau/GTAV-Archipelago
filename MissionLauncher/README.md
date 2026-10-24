# MissionLauncher

Replay any GTA V story mission or Strangers and Freaks mission from an in-game menu, and let other mods
start missions and receive their results (pass / fail, medal, gold objectives).

MissionLauncher uses the game's own **Replay Mission** system, exactly like the pause menu does:

- the story is temporarily set back to the state it had at that point of the game,
- ambient distractions (phone calls, random characters…) are paused,
- the game's own mission passed / mission failed screens are used (Space to retry, Esc to quit),
- when the replay ends, the game restores everything exactly as it was before.

Your save is never modified.

## Requirements

- GTA V **Legacy** 1.0.3725, story mode, BattlEye turned off
- [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/)
- [ScriptHookVDotNet](https://github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases) 3.7 (nightly)
- [LemonUI](https://github.com/LemonUIbyLemon/LemonUI/releases) 2.x (`LemonUI.SHVDN3.dll`)

## Installation

The easiest way is the **GTA V Archipelago Setup**, which installs every mod at once.

By hand, copy into the `Archipelago` folder of your game (the folder set as `ScriptsLocation` in
`ScriptHookVDotNet.ini`; it is `scripts` by default):

```
Archipelago/MissionLauncher.dll
Archipelago/MissionLauncher/config.json
```

`LemonUI.SHVDN3.dll` must be in the same folder.

## Usage

The menu is off by default (`"EnableMenu": true` to use it). Press **F7** to open it, then pick **Story missions** or **Strangers and Freaks**.
The list contains every mission the game allows to replay (the game itself decides this, using the same
data as the pause menu). Unlike the pause menu, missions you have not completed yet are shown too, including the
alternative approaches and all three endings. Set `OnlyCompletedMissions` to get exactly the pause menu list.
Mission names come from the game itself, in the game's language. Missions sharing the same name are
completed with their variant (for example *The Bureau Raid - Fire Crew* / *Roof Entry*,
*The Third Way (Ending C - Save everyone)*).

Every mission has a fixed number. Story missions are listed in story order, side content
(assassinations, family event) comes last.

| Missions | Numbers |
|---|---|
| Story missions | 01 and up, in story order (01 = Prologue) |
| Strangers and Freaks | 101 and up (only the 46 the game can replay) |
| Gold medal objectives | mission × 100 + position (0401, 0402, 10101…) |

## Mission markers

Every mission gets its own icon on the map, at the game's own start point and in the colour of the character
who starts it (Michael blue, Franklin green, Trevor orange). You only see the missions of the character you play,
like in the story. Walk into the marker to start the mission. When several missions share a place, a chooser
appears at the top left (arrows to choose, E to start). Completed missions show a tick and are replayed with E.

`Markers`: `All` (default), `Api` (only the missions another mod enables, see API.md) or `Off`.
The restore point prompt shown when a replay starts is confirmed automatically (`AutoConfirmRestorePoint`).

## With Archipelago

The Archipelago client takes over the markers and the menu: only the missions you have unlocked, with a character
you can play, have a marker. The other missions are greyed out in the F7 menu with the reason (the missing item or
character), and they cannot be started.

## Configuration

`Archipelago/MissionLauncher/config.json`

| Setting | Default | Description |
|---|---|---|
| `EnableMenu` | `false` | `true` shows the F7 menu (other mods can always use the API) |
| `MenuKey` | `F7` | Key that opens the menu (`None` = no key) |
| `MenuOrder` | `Story` | `Story` = story order, `Completion` = the order you completed them in (like the pause menu) |
| `OnlyCompletedMissions` | `false` | `true` only lists the missions completed in your save, like the pause menu |
| `AutoConfirmRestorePoint` | `true` | Confirms the restore point prompt shown when a replay starts |
| `RestorePointKeyFallback` | `true` | Presses Enter from outside the game loop if the prompt freezes the scripts |
| `Markers` | `All` | `All`, `Api` (only the missions another mod enables) or `Off` |
| `MarkerAutoStart` | `true` | Walking into a single mission marker starts it without pressing E |
| `MarkerRadius` | `1.5` | Size of the markers, in metres |
| `VerboseLog` | `false` | Writes the full mission list and debug details to the log |
| `Globals` | | Game memory addresses for build 3725. Do not change them unless you know what you are doing |

The start point of each mission for each character (from the game's own data) is built into the DLL. A
`missions.json` placed in `Archipelago/MissionLauncher` replaces it.

## Logs

In `Archipelago/MissionLauncher/`:

- `MissionResults.log`: every replay, attempt and result (never cleared),
- `MissionLauncher.log`: technical log.

```
[RESULT] 04 Complications - attempt 1 - PASSED, SILVER medal (75 %) - 7:12
    0401 [OK] ...
    0402 [X] ...
```

## For mod developers

See [API.md](API.md).

## Building

Put `ScriptHookVDotNet3.dll` and `LemonUI.SHVDN3.dll` in `lib/`, then build `MissionLauncher.sln`
(.NET Framework 4.8). The output folder has the same layout as the `Archipelago` folder.
