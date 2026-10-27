# GTA V Archipelago

Play **Grand Theft Auto V** story mode in an [Archipelago](https://archipelago.gg) multiworld randomizer.

Los Santos starts empty: no story, one character, no money. Missions, Strangers and Freaks, gold medals, optional
objectives and 160 hidden collectibles are checks. Michael, Franklin and Trevor, the mission unlocks, money, weapons,
fun vehicles and more than fifty traps are items, shuffled with the other games of the multiworld.

**[Download the latest release](https://github.com/dudleydehenau/GTAV-Archipelago/releases/latest)**: grab
`GTAVArchipelagoSetup`, it installs everything else for you.

> [!NOTE]
> This is the first version (V1.0.0). It can be played from start to finish, but expect some rough edges. More
> randomized content and more options are in development. Feedback is very welcome, see
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

- GTA V **Legacy** 1.0.3725 (Steam, Epic or Rockstar Games Launcher), see [Game version](#game-version). The Enhanced
  edition and newer Legacy builds are not supported yet.
- [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/) (the setup installs it from the zip you download there).
- [Archipelago](https://github.com/ArchipelagoMW/Archipelago/releases) 0.6.4 or newer to generate a game.

## Setup

1. Download `GTAVArchipelagoSetup` from the [latest release](https://github.com/dudleydehenau/GTAV-Archipelago/releases/latest)
   and run it. It installs ScriptHookVDotNet, LemonUI, the mods and the APWorld.
2. Download the ScriptHookV zip from dev-c.com and install it with the **ScriptHookV...** button of the setup.
3. Turn BattlEye off: in the Rockstar Games Launcher settings, or with `-nobattleye` in the Steam or Epic launch
   options.
4. Generate your game with `gtav.apworld`. Presets are included: Short, Ten Hours, One Mission, Everything,
   Stream Safe.
5. Start GTA V, press **F6**, open **Connection**, enter the room address, your slot name and password, then start a
   **New Game** from the pause menu.

## Game version

The mods only work with GTA V **Legacy 1.0.3725**. If your game updated to a newer build, ScriptHookV and the mods do
not load. To check your version, right-click `GTA5.exe`, open **Properties > Details** and look at the product
version. Support for the latest Legacy build is in development.

### Steam

Steam can download the 1.0.3725 build with its own console. The game must already be installed through Steam: these
commands only bring the changed files back to 1.0.3725.

1. Close GTA V. Press `Win + R`, type `steam://open/console` and press Enter. A **Console** tab opens in Steam.
2. Type these three commands, one at a time, and wait for `Depot download complete` after each one:
   ```
   download_depot 271590 271591 773775905684914568
   download_depot 271590 271592 1613219817674806364
   download_depot 271590 271594 5939773577741748237
   ```
3. The files are in `Steam\steamapps\content\app_271590\`. Back up your game folder, then copy the content of the
   three `depot_...` folders over it.
4. Check that `GTA5.exe` shows version 1.0.3725.0 in **Properties > Details**.
5. Do not use **Verify integrity of game files**, it puts the newest version back. If Steam shows a pending update
   later, go offline (**Steam > Go Offline**) before starting the game. Keep a copy of the downloaded files to
   restore them quickly.

### Epic Games and Rockstar Games Launcher

These launchers cannot download an older build. The only way is to replace `GTA5.exe`, `update\update.rpf` and
sometimes `title.rgl` with the files of the 1.0.3725 build, for example from a backup made before updating. Keep a
backup of these files before every game update.

## Known issues

- If the game is closed while a mission replay is running, the next start can stay on the loading screen. Open
  `Documents\Rockstar Games\GTA V\Profiles\<your profile>`, move the newest `SGTA5...` file (and its `.bak`) to another
  folder and start again. Your Archipelago progress is not affected.

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

## Requirements

- GTA V **Legacy** 1.0.3889 (the latest version) or 1.0.3725 (Steam, Epic or Rockstar Games Launcher). The Enhanced edition is not supported.
- [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/) (the setup installs it from the zip you download there).
- [Archipelago](https://github.com/ArchipelagoMW/Archipelago/releases) 0.6.4 or newer to generate a game.

## Known issues

- The mods never save the game: every start is a new game, and your money, weapons, vehicles, characters and checks
  come back from the mods. Any save the game writes by itself is deleted. If you played with
  an older version, move the `SGTA5...` files out of `Documents\Rockstar Games\GTA V\Profiles\<your profile>` once.
- If the game still stays on the loading screen, move the newest `SGTA5...` file (and its `.bak`) out of that folder
  and start again. Your Archipelago progress is not affected.
- Bug reports are welcome in the [issues](https://github.com/dudleydehenau/GTAV-Archipelago/issues): attach the zip
  made by **F6 > Settings > Create a bug report**.

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
