# GTA V Archipelago Setup Guide

## Required software

- GTA V **Legacy** 1.0.3889 (the latest version) or 1.0.3725 (Steam, Epic or Rockstar Games Launcher). GTA V Enhanced is not supported.
- [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/) for your game version.
- The GTA V Archipelago Setup (`GTAVArchipelagoSetup.exe`), from the GTAVArchipelagoSetup releases.
- [Archipelago](https://github.com/ArchipelagoMW/Archipelago/releases) 0.6.4 or newer to generate games.

## Installation

1. Run `GTAVArchipelagoSetup.exe`. It finds your GTA V folder (or use Browse).
2. Click **Install / Update**. It installs ScriptHookVDotNet, LemonUI and the five mods in a new `Archipelago` folder
   next to `GTA5.exe`, and sets `ScriptsLocation="Archipelago"` in `ScriptHookVDotNet.ini`.
3. If ScriptHookV is missing, download its zip from dev-c.com, click **ScriptHookV...** and select the zip.
4. Turn BattlEye off:
   - Rockstar Games Launcher: Settings > Grand Theft Auto V Legacy > uncheck *Enable BattlEye*.
   - Steam: GTA V Legacy > Properties > Launch Options: `-nobattleye`.
   - Epic Games: Library > GTA V > Manage > Launch Options: `-nobattleye`.

## Generating a game

1. Install `gtav.apworld`: the setup copies it to the `custom_worlds` folder of your Archipelago installation when it
   finds it; otherwise double-click the file.
2. Create your options file from the player options page, or with "Generate Template Options" in the Archipelago
   launcher, and generate as usual.

## Connecting

1. Start GTA V and load any save.
2. Press **F6** > Connection, enter the server address (for example `archipelago.gg:38281`), your slot name and the
   password if there is one, then **Connect**.
3. Start a **New Game** from the pause menu. GameEnvironment creates the Archipelago world with your slot's starting
   character. Save it in a free slot.

The client remembers the connection and reconnects by itself next time.

## Streaming

Turn on **F6 > Settings > Stream safe** (or remove `Irresistible` from `enabled_traps`): one trap uses strip club
dancer models that can show nudity.
