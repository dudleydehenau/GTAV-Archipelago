# GameEnvironment

A sterile free-roam start for GTA V story mode (Legacy, builds 1.0.3889 and 1.0.3725), built for randomizers such as
[Archipelago](https://archipelago.gg) and for anyone who wants the open world without the story.

When you choose **New Game**, GameEnvironment skips North Yankton and drops you in Los Santos. No story mission
triggers on its own, and the completion percentage shows `???` in the mod menu. Missions are started through
[MissionLauncher](https://github.com/dudleydehenau/GTAV-Archipelago/tree/main/MissionLauncher), which uses the game's own replay system.
Characters, money, weapons and vehicles are given through a public API (the Archipelago client uses it).

Story saves are never touched: GameEnvironment only acts on a new game and on the saves it created.

## Requirements

- GTA V **Legacy** 1.0.3889 (latest) or 1.0.3725, story mode, BattlEye turned off
- [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/)
- [ScriptHookVDotNet](https://github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases) 3.7 (nightly)
- [LemonUI](https://github.com/LemonUIbyLemon/LemonUI/releases) 2.x (`LemonUI.SHVDN3.dll`)
- MissionLauncher 1.3 (recommended: replays are also detected without it)

## Installation

The easiest way is the **GTA V Archipelago Setup**, which installs every mod at once.

By hand: copy `GameEnvironment.dll` and the `GameEnvironment` folder (with `config.json`) into the `Archipelago`
folder of your game (the folder set as `ScriptsLocation` in `ScriptHookVDotNet.ini`).

Then start a **New Game** from the pause menu (Game > New Game). Keep your story saves in other slots.

## What it does

- **New game**: halts the story flow (the "story finished" flag), stops the prologue, cleans up North
  Yankton, picks the start character (`StartCharacter`: `Michael` by default, `Franklin`, `Trevor` or `Random`), places
  them next to their home, removes their weapons, sets the money to `StartMoney` and requests an autosave.
- **Character wheel**: locked characters are greyed out, using the game's own availability flags. A
  character is never locked again unless `AllowRelock` is `true`, and at least one stays unlocked.
  `CharacterLocking: false` unlocks all three.
- **State file**: `Archipelago/GameEnvironment/state.json` is the source of truth (unlocks, money and granted
  weapons per character, vehicles). It is written immediately and re-applied after each load and after
  each replay.
- **Money and weapons**: given to the current character only. A weapon you already have gets the ammo added.
  The weapons of the save are guarded: one the game takes away is given back, and ammo that drops without you
  firing is put back; the ammo you fire is saved.
- **Locked characters**: if you play a character that is locked (new game, loaded save), you are switched to an
  unlocked one automatically.
- **Vehicles**: delivered next to the player, with a blip. A vehicle you leave behind
  (`VehicleStoreDistance`) is stored. Order it again from the menu at any time, with no cooldown. A destroyed
  vehicle is greyed out until you pay to repair it (`DefaultRepairCost`, `RepairCosts` per vehicle class).
- **Random events** are disabled (`DisableRandomEvents`), and pending story calls, texts and emails are dropped (`BlockCommunications`).
- **Replays**: nothing is touched while a replay runs. The game handles the characters itself.
- **Open doors** (`OpenDoors`): in free roam, the doors of the places where missions start (the three
  homes, Franklin's villa, Floyd's apartment, Lester's house and factory, the Vanilla Unicorn, Simeon's
  dealership, Trevor Philips Industries) are unlocked when you are within `Radius` metres. As soon as a
  replay is requested or a mission runs, every door goes back to the state the game had given it, so the
  mission finds its building exactly as the story expects. `Groups` lists the places: `Name`, centre
  `X`/`Y`/`Z`, optional `Radius`, `Doors` (door system names such as `DOORHASH_T_APARTMENT_VB`, or numbers),
  optional `Ipls` to load and `Interior` entity sets to switch (`Enable` / `Disable`). IPLs and entity sets
  are not reverted. `Enabled: false` turns the feature off.

## Menu (F8)

Off by default (the Archipelago menu, F6 > Vehicles, delivers and repairs your vehicles). With `EnableMenu: true`:
money, completion `???`, characters (unlocked or locked) and vehicles (deliver, repair).

## Configuration

`Archipelago/GameEnvironment/config.json`:

| Setting | Default | Description |
|---|---|---|
| `EnableMenu` | `false` | Shows the F8 menu. |
| `MenuKey` | `"F8"` | Key of the menu ([key names](https://learn.microsoft.com/dotnet/api/system.windows.forms.keys)). |
| `VerboseLog` | `false` | More details in `GameEnvironment.log`. |
| `ShowInactiveNotice` | `true` | Message above the minimap on a story save, reminding that a New Game is needed. |
| `NewGameSetup` | `true` | Turns a new game into a sterile game. |
| `StartCharacter` | `"Michael"` | `Michael`, `Franklin`, `Trevor` or `Random`. The Archipelago client overrides it with your slot's character. |
| `CharacterLocking` | `true` | Only the start character is unlocked at first. `false` unlocks all three. |
| `AllowRelock` | `false` | Lets the API lock a character again (`SetUnlockedCharacters` ignores it). |
| `StartMoney` | `0` | Money of each character on a new game. |
| `StartHour` | `10` | Hour on a new game. |
| `DisableRandomEvents` | `true` | No random events. |
| `UnlockSpecialAbilities` | `true` | Michael's bullet time, Franklin's driving focus and Trevor's rage are unlocked and kept on, missions included (some gold objectives need them; during a replay they are unlocked but left to the mission). |
| `BlockCommunications` | `true` | Drops pending story calls, texts and emails. |
| `NoSaves` | `true` | The mod never saves the game, and any save the game writes by itself during a sterile session is deleted (saves that were already there are never touched). With no save in your profile, every start is a new game that keeps your money, weapons, vehicles and characters as you left them, with the last character you played. `AutoSaveAfterSetup` and `AutoSaveMinutes` are ignored. |
| `OpenGunShopsInMissions` | `true` | Ammu-Nation opens during missions and replays (some missions need it) and closes again afterwards, so weapons cannot be bought outside missions. |
| `AutoSaveAfterSetup` | `true` | Autosave right after the new game setup. |
| `VehicleStoreDistance` | `300` | A delivered vehicle left this far away goes back to the garage. |
| `DefaultRepairCost` | `1000` | Price to repair a destroyed vehicle. |
| `RepairCosts` | `Super`, `Planes`, `Helicopters`, `Military`: `10000` | Repair price per vehicle class. |
| `Spawns` | the three homes | Where each character appears on a new game (`Character`, `X`, `Y`, `Z`, `Heading`). |
| `OpenDoors` | 10 places | Doors opened near the player in free roam (see above). |

## Files

| File | Content |
|---|---|
| `GameEnvironment/config.json` | Settings. |
| `GameEnvironment/state.json` | The sterile game: unlocks, money and weapons per character, vehicles. Backed up before a new game. |
| `GameEnvironment/GameEnvironment.log` | Every save classification, the new game setup step by step and the state applied. |

## Building

Open `GameEnvironment.sln` in Visual Studio 2022 (.NET Framework 4.8). Put `ScriptHookVDotNet3.dll` and
`LemonUI.SHVDN3.dll` in `lib` first. The build output contains `GameEnvironment.dll` and
`GameEnvironment/config.json`.

## For developers

See [API.md](API.md).
