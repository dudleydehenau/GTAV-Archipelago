# GTA V Archipelago

Play **Grand Theft Auto V** story mode in an [Archipelago](https://archipelago.gg) multiworld randomizer.

Los Santos starts empty: no story, one character, no money. Missions, Strangers and Freaks, gold medals, optional
objectives and 160 hidden collectibles are checks. Michael, Franklin and Trevor, the mission unlocks, money, weapons,
fun vehicles and more than fifty traps are items, shuffled with the other games of the multiworld.

**[Download the latest release](https://github.com/dudleydehenau/GTAV-Archipelago/releases/latest)**: grab
`GTAVArchipelagoSetup`, it installs everything else for you.

> [!NOTE]
> This is version 1.1. It can be played from start to finish, but expect some rough edges. More randomized
> content and more options are in development. Feedback is very welcome, see
> [Feedback and bug reports](#feedback-and-bug-reports).

> [!CAUTION]
> Story mode only, with BattlEye turned off. Before playing GTA Online, click **Disable mods (GTA Online)** in the
> setup and turn BattlEye back on.

> [!WARNING]
> **Streamers:** one trap can show nudity. Turn on **F6 > Settings > Stream safe** in game, or use the "Stream Safe"
> preset.

The full guide (quick start, options, in-game menu, how checks work) is in
[GameArchipelago/README.md](GameArchipelago/README.md).

## Requirements

- GTA V **Legacy** 1.0.3889 (the latest version) or 1.0.3725 (Steam, Epic or Rockstar Games Launcher). The game
  version is detected at startup. The Enhanced edition is not supported.
- [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/) (the setup installs it from the zip you download there).
- [Archipelago](https://github.com/ArchipelagoMW/Archipelago/releases) 0.6.4 or newer to generate a game.

## Setup

1. Download `GTAVArchipelagoSetup` from the [latest release](https://github.com/dudleydehenau/GTAV-Archipelago/releases/latest)
   and run it. It installs ScriptHookVDotNet, LemonUI, the mods and the APWorld.
2. Install ScriptHookV with step 2 of the setup: open the download page, download the zip made for your game
   version, and the setup finds it in your Downloads folder (you can also drag the zip onto the window).
3. Turn BattlEye off: in the Rockstar Games Launcher settings, or with `-nobattleye` in the Steam or Epic launch
   options.
4. Generate your game with `gtav.apworld`. Presets are included: Short, Ten Hours, One Mission, Everything,
   Stream Safe.
5. Start GTA V, press **F6**, open **Connection**, enter the room address, your slot name and password, then start a
   **New Game** from the pause menu.

## Known issues

- The mods never save the game, and any save the game writes by itself is deleted: every start is a new game that
  keeps your money, weapons, vehicles and characters. If you played an older version, move the `SGTA5...` files out
  of `Documents\Rockstar Games\GTA V\Profiles\<your profile>` once.
- If the game still stays on the loading screen, move the newest `SGTA5...` file (and its `.bak`) out of that folder
  and start again. Your Archipelago progress is not affected.

## Feedback and bug reports

Every report helps. You can reach me:

- on Discord: **Deesdee**
- with a [GitHub issue](https://github.com/dudleydehenau/GTAV-Archipelago/issues)

Please attach a bug report: in game, **F6 > Settings > Create a bug report**. It makes a zip with your save, the event
history, the settings and the logs of every mod, in `Archipelago\GameArchipelago\reports` next to `GTA5.exe`.

If the F6 menu does not open or the report is not created, send these files instead:

- `ScriptHookV.log` and `ScriptHookVDotNet.log`, next to `GTA5.exe`
- the `.log` files in the `Archipelago` folder and its subfolders (`GameArchipelago`, `GameEnvironment`,
  `MissionLauncher`, `GameTraps`, `GameCollectibles`)

Tell me what you were doing when it happened, it helps a lot.

## What is in this repository

| Folder | What it is |
|---|---|
| [`GameArchipelago`](GameArchipelago) | The Archipelago client (in-game menu on F6) and the APWorld (`apworld/gtav`). |
| [`GameEnvironment`](GameEnvironment) | Sterile open world: a new game without the story, locked characters, money, weapons, vehicles. |
| [`MissionLauncher`](MissionLauncher) | Starts any mission through the game's replay system: map markers, results, medals. |
| [`GameTraps`](GameTraps) | The traps. |
| [`GameCollectibles`](GameCollectibles) | The 160 collectibles. |
| [`GTAVArchipelagoSetup`](GTAVArchipelagoSetup) | The Windows installer. |

Each mod also works on its own and has its own README and changelog.

## Building

1. Open `GTAV-Archipelago.sln` in Visual Studio 2022 and build in **Release**. The build order is already set.
   `ScriptHookVDotNet3.dll` and `LemonUI.SHVDN3.dll` are in each `lib` folder.
2. Run `Make-Releases.ps1` in PowerShell. It writes one zip per mod, `gtav.apworld` and the setup zip into
   `releases`, with the layout the setup expects.
3. Publish all these files in one GitHub release. The setup always takes the newest release that contains each file.

The APWorld can be built on its own with `python GameArchipelago/tools/build_apworld.py`.

## Credits

- [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/) by Alexander Blade.
- [ScriptHookVDotNet](https://github.com/scripthookvdotnet/scripthookvdotnet) and
  [LemonUI](https://github.com/LemonUIbyLemon/LemonUI), included in the `lib` folders under their own licenses.
- [Archipelago](https://archipelago.gg) and
  [Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net).

Grand Theft Auto V is a trademark of Rockstar Games. This project is not affiliated with Rockstar Games or Take-Two.
