# GameCollectibles

Every hidden collectible of GTA V story mode (Legacy, builds 1.0.3889 and 1.0.3725) is replaced by one floating 3D object, placed at
the official spot and available from the very start, whatever the story progress. Each pickup is reported through
a public API, which makes every collectible a check for the [Archipelago](https://archipelago.gg) randomizer.

## Requirements

- GTA V **Legacy** 1.0.3889 (latest) or 1.0.3725, story mode, BattlEye turned off
- [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/)
- [ScriptHookVDotNet](https://github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases) 3.7 (nightly)
- [LemonUI](https://github.com/LemonUIbyLemon/LemonUI/releases) 2.x (`LemonUI.SHVDN3.dll`)

## Installation

The easiest way is the **GTA V Archipelago Setup**, which installs every mod at once.

By hand: copy `GameCollectibles.dll` and the `GameCollectibles` folder (with `config.json`) into the `Archipelago`
folder of your game (the folder set as `ScriptsLocation` in `ScriptHookVDotNet.ini`).

## Collectibles

| Kind | Count | Ids | Where |
|---|---|---|---|
| Spaceship parts | 50 | `SpaceshipPart_01` to `SpaceshipPart_50` | On land, a few on rooftops and cliffs |
| Letter scraps | 50 | `LetterScrap_01` to `LetterScrap_50` | On land |
| Nuclear waste | 30 | `NuclearWaste_01` to `NuclearWaste_30` | Underwater |
| Submarine parts | 30 | `SubmarinePart_01` to `SubmarinePart_30` | Underwater |

160 collectibles in all. The positions come from the game's own scripts (`spaceshipparts`, `letterscraps`,
`ambient_sonar`, `ambient_diving`), in the same order as the game. The game's own versions are stopped
(`HideGameCollectibles`) so they never show up twice.

The object (`Model`) floats, spins and glows. It appears when you get within `StreamDistance` m and is picked up
within `PickupRadius` m on foot, `VehiclePickupRadius` m in a vehicle and `UnderwaterPickupRadius` m in the water
or in a submarine. With Archipelago, nothing is required for the underwater ones: finding a way down is up to you.

## Menu (F10)

Off by default (the Archipelago menu, F6 > Collectibles, shows the same things). With `EnableMenu: true`: count per
kind, map markers (`None`, `Nearby`, `All`) and a route to the nearest collectible left.

## Configuration

`Archipelago/GameCollectibles/config.json`:

| Setting | Default | Description |
|---|---|---|
| `EnableMenu` | `false` | Shows the F10 menu. |
| `MenuKey` | `"F10"` | Key of the menu ([key names](https://learn.microsoft.com/dotnet/api/system.windows.forms.keys)). |
| `VerboseLog` | `false` | More details in `GameCollectibles.log`. |
| `Model` | `"ch_prop_casino_diamonds_01a"` | Prop used for every collectible. If the model cannot be loaded, `prop_ld_case_01` is used. Any prop name works; browse them on [PlebMasters Forge](https://forge.plebmasters.de/objects). |
| `Size` | `0.8` | Visual size (1 = normal, 0.8 = 20 % smaller). |
| `HoverHeight` | `0.6` | Height above the spot, in metres. |
| `SpinSpeed` | `90` | Rotation, in degrees per second. |
| `Glow` | `true` | Light under the object. |
| `StreamDistance` | `150` | Distance at which objects appear. |
| `PickupRadius` | `1.6` | Pickup distance on foot. |
| `VehiclePickupRadius` | `3` | Pickup distance in a vehicle. |
| `UnderwaterPickupRadius` | `4.5` | Pickup distance in the water or in a submarine. |
| `Blips` | `"Nearby"` | Map markers: `None`, `Nearby` or `All`. Also in the menu. |
| `BlipDistance` | `250` | Range of the `Nearby` markers. |
| `HideGameCollectibles` | `true` | Stops the game's own collectible scripts. |
| `Notify` | `false` | Notification with the count on each pickup (the Archipelago client shows its own, with the next check). |
| `SpaceshipParts`, `LetterScraps`, `NuclearWaste`, `SubmarineParts` | `true` | One switch per kind. |

## Files

| File | Content |
|---|---|
| `GameCollectibles/config.json` | Settings. |
| `GameCollectibles/collected.json` | Collected ids (the Archipelago client replaces it with your save of the current game). |
| `GameCollectibles/GameCollectibles.log` | Log. |

## Building

Open `GameCollectibles.sln` in Visual Studio 2022 (.NET Framework 4.8). Put `ScriptHookVDotNet3.dll` and
`LemonUI.SHVDN3.dll` in `lib` first. The build output contains `GameCollectibles.dll` and
`GameCollectibles/config.json`.

## For developers

See [API.md](API.md).
