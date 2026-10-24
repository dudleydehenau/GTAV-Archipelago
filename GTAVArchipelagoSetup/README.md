# GTA V Archipelago Setup

A small Windows program that downloads and installs everything needed to play GTA V story mode in an
[Archipelago](https://archipelago.gg) multiworld, and helps you switch back to GTA Online safely.

> [!CAUTION]
> The mods are for **story mode only**, with **BattlEye turned off**. Before playing GTA Online, click
> **Disable mods (GTA Online)** and turn BattlEye back on.

> [!WARNING]
> **Streamers:** one trap can show nudity. Turn on stream safe mode in game (F6 > Settings > Stream safe) or remove
> `Irresistible` from your Archipelago options.

## What it does

- Finds your **GTA V Legacy** folder (Rockstar Games Launcher, Steam or Epic), shows the game version and warns if it
  is not 1.0.3725 or if it is the Enhanced edition.
- Downloads the latest releases from GitHub and installs them:
  - ScriptHookVDotNet 3.7 (nightly) next to `GTA5.exe`,
  - LemonUI, MissionLauncher, GameEnvironment, GameTraps, GameCollectibles and GameArchipelago into a new
    **`Archipelago`** folder next to `GTA5.exe`,
  - `gtav.apworld` into the `custom_worlds` folder of your Archipelago launcher (or `Archipelago/apworld`).
- Sets `ScriptsLocation="Archipelago"` in `ScriptHookVDotNet.ini`, so the scripts load from that folder, and turns
  the ScriptHookVDotNet console (F4) and reload key off. `NativeTrainer.asi` is renamed
  `NativeTrainer.asi.disabled` so its F4 menu cannot be opened in game.
- Moves older installs of these mods from `scripts` to `Archipelago`, with their settings and saves.
- Keeps your `config.json` files on update (uncheck "Keep my settings" to reset them).
- Installs ScriptHookV from the zip you download from dev-c.com (it cannot be redistributed).
- **Disable mods (GTA Online)** moves `dinput8.dll`, `ScriptHookV.dll` and `ScriptHookVDotNet.asi` aside;
  **Enable mods (story mode)** puts them back.
- Shows how to turn BattlEye off on your platform, and on Steam starts the game with `-nobattleye`.
- **Uninstall** removes what it installed, optionally keeping your settings and Archipelago saves, and sets
  `ScriptsLocation` back to `scripts`, the console back on F4 and the Native Trainer back. ScriptHookV is left in place.

The program asks for administrator rights because games are often installed under `Program Files`.

## Download sources

The list of downloads is `sources.json`, next to the program (a built-in copy is used if the file is missing):

| Field | Description |
|---|---|
| `Name`, `Description` | Shown in the list. |
| `Repo` | GitHub repository (`owner/name`). The newest release with a matching file is used. |
| `Asset` | Regular expression for the release file name. |
| `Prerelease` | `true` also accepts pre-releases (ScriptHookVDotNet nightlies are published that way). |
| `Url` | A direct download link, used instead of `Repo`. |
| `Target` | `Game` (next to `GTA5.exe`), `Mods` (the `Archipelago` folder) or `World` (an APWorld). |
| `Files` | Only these file names are taken from the zip (folders are ignored). |

By default every mod and `gtav.apworld` come from the releases of
[dudleydehenau/GTAV-Archipelago](https://github.com/dudleydehenau/GTAV-Archipelago/releases). Entries whose `Repo`
starts with `YOUR_GITHUB_NAME` are skipped, which is handy in a fork that publishes only some of the parts.

## Building

Open `GTAVArchipelagoSetup.sln` in Visual Studio 2022 (.NET Framework 4.8, Windows Forms). The output is
`GTAVArchipelagoSetup.exe` with `sources.json`.
