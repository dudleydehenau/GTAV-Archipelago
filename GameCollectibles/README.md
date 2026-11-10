# GameCollectibles

Every hidden collectible of GTA V story mode (Legacy, builds 1.0.3889 and 1.0.3725) is replaced by one floating 3D object, placed at
the official spot and available from the very start, whatever the story progress. The mod also follows the free-roam
feats: stunt jumps, flights under the bridges, knife flights, landmarks, skill levels and police escapes. Each pickup
and each feat is reported through a public API, which makes them checks for the [Archipelago](https://archipelago.gg)
randomizer.

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
| Monkey mosaics | 50 | `MonkeyMosaic_01` to `MonkeyMosaic_50` | On the ground in front of each mosaic |
| Peyote plants | 27 | `PeyotePlant_01` to `PeyotePlant_27` | On land, the last six underwater |
| Epsilon tracts | 10 | `EpsilonTract_01` to `EpsilonTract_10` | Summits, rooftops, one underwater |

247 collectibles in all. The positions come from the game's own scripts (`spaceshipparts`, `letterscraps`,
`ambient_sonar`, `ambient_diving`, `photographymonkey`, `animal_controller`, `epsilontract`), in the same order as
the game. Picking up a peyote plant does not turn you into an animal; the game's own mosaics, peyote plants and
tracts are left alone. The game's own versions are stopped
(`HideGameCollectibles`) so they never show up twice.

The object (`Model`) floats, spins and glows. It appears when you get within `StreamDistance` m and is picked up
within `PickupRadius` m on foot, `VehiclePickupRadius` m in a vehicle and `UnderwaterPickupRadius` m in the water
or in a submarine. With Archipelago, nothing is required for the underwater ones: finding a way down is up to you.

## Free-roam feats

| Feat | Ids | Detection |
|---|---|---|
| Stunt jumps | `StuntJump_01` to `StuntJump_50` | A jump the game counts as successful, matched with the closest of the 50 jump starts. |
| Under the bridge | `UnderBridge_00` to `UnderBridge_69` (50 real bridges) | The game's own bridge flags. |
| Knife flights | `KnifeFlight_33` to `KnifeFlight_60` (15) | The game's own flags. |
| Landmarks | `Landmark_MountChiliad`, `Landmark_VinewoodSign`... (44) | You get within the landmark's radius (and above its height for summits and rooftops). Purple markers show the ones left. |
| Skills | `Skill_Michael_Stamina_10` to `Skill_Trevor_LungCapacity_100` | Each skill stat of each character, every 10 % of progress from its value at the start of the game (with GameEnvironment). |
| Activities | `Activity_112` to `Activity_309` (67) | The game's own 100 % completion flags (races, Flight School, shooting range, sports, shops...). |
| Police escapes | `Escape_1` to `Escape_5` | The wanted level goes away after its stars flashed (being busted or wasted does not count). Escaping 4 stars also counts 1 to 3. |

The bridge flags are read from the game's saved globals, for builds 3889 and 3725 (checked at startup, see the log).
A feat is reported once; its id goes in `collected.json` with the collectibles.

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
| `MosaicPickupRadius` | `4` | Pickup distance for monkey mosaics: they sit on walls, sometimes behind rubble you cannot walk through (in the story they are only photographed). |
| `UnderwaterPickupRadius` | `4.5` | Pickup distance in the water or in a submarine. |
| `Blips` | `"Nearby"` | Map markers: `None`, `Nearby` or `All`. Also in the menu. |
| `BlipDistance` | `250` | Range of the `Nearby` markers. |
| `HideGameCollectibles` | `true` | Stops the game's own collectible scripts. |
| `Notify` | `false` | Notification with the count on each pickup (the Archipelago client shows its own, with the next check). |
| `SpaceshipParts`, `LetterScraps`, `NuclearWaste`, `SubmarineParts`, `MonkeyMosaics`, `PeyotePlants`, `EpsilonTracts` | `true` | One switch per kind. |
| `StuntJumps`, `Bridges`, `Landmarks`, `Skills`, `Escapes` | `true` | One switch per feat (`Bridges` covers the knife flights). |
| `LandmarkBlips` | `true` | Purple markers on the landmarks left. |

## Files

| File | Content |
|---|---|
| `GameCollectibles/config.json` | Settings. |
| `GameCollectibles/collected.json` | Collected and done ids (the Archipelago client replaces it with your save of the current game). |
| `GameCollectibles/GameCollectibles.log` | Log. |

## Building

Open `GameCollectibles.sln` in Visual Studio 2022 (.NET Framework 4.8). Put `ScriptHookVDotNet3.dll` and
`LemonUI.SHVDN3.dll` in `lib` first. The build output contains `GameCollectibles.dll` and
`GameCollectibles/config.json`.

## For developers

See [API.md](API.md).
