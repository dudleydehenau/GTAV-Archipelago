# GTA V Archipelago

Play **Grand Theft Auto V** story mode in an [Archipelago](https://archipelago.gg) multiworld randomizer.

Los Santos starts empty: no story, one character, no money. Missions, Strangers and Freaks, gold medals, optional
objectives and 160 hidden collectibles are checks. Michael, Franklin and Trevor, the mission unlocks, money, weapons,
fun vehicles and more than fifty traps are items, shuffled with the other games of the multiworld.

**[Download the latest release](https://github.com/dudleydehenau/GTAV-Archipelago/releases/latest)**: grab
`GTAVArchipelagoSetup`, it installs everything else for you.

> [!CAUTION]
> Story mode only, with BattlEye turned off. Before playing GTA Online, click **Disable mods (GTA Online)** in the
> setup and turn BattlEye back on.

> [!WARNING]
> **Streamers:** one trap can show nudity. Turn on **F6 > Settings > Stream safe** in game, or use the "Stream Safe"
> preset.

The full guide (quick start, options, in-game menu, how checks work) is in
[GameArchipelago/README.md](GameArchipelago/README.md).

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
