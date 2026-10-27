# GameTraps API (ApiVersion 4)

Reference `GameTraps.dll` with **Copy Local = false** and use the static class `GameTraps.TrapApi`. Call it from a
script tick (the game thread).

```csharp
bool ready = TrapApi.IsReady;
IReadOnlyList<string> all = TrapApi.Traps;
IReadOnlyList<string> enabled = TrapApi.EnabledTraps;
string title = TrapApi.GetTitle(TrapIds.ScooterBrother);

if (!TrapApi.Trigger(TrapIds.AlienAbduction, out string error)) Log(error);
if (!TrapApi.Trigger(TrapIds.Jaws, "Alice", out error)) Log(error);
string picked = TrapApi.TriggerRandom();

IReadOnlyList<string> running = TrapApi.ActiveTraps;
IReadOnlyList<string> waiting = TrapApi.QueuedTraps;
int count = TrapApi.PendingCount;
TrapApi.ClearQueue();

TrapApi.StreamSafe = true;
TrapApi.RevealNames = false;

TrapApi.TrapStarted += id => { };
TrapApi.TrapFinished += (id, interrupted) => { };
```

| Member | Description |
|---|---|
| `IsReady` | `true` once the script has ticked. |
| `Traps`, `EnabledTraps` | Every trap id, and the ids enabled in `config.json`. |
| `GetTitle(id)` | Display title of a trap. |
| `Trigger(id, out error)` | Queues a trap. Fails for an unknown id or a disabled trap. |
| `Trigger(id, source, out error)` | Same, with the name of who sent it. The start banner says "Courtesy of <source>". |
| `TriggerRandom()` | Queues a random enabled trap and returns its id, or `null`. |
| `ActiveTrap`, `ActiveTraps` | The running traps. |
| `QueuedTraps`, `PendingCount` | The traps waiting to start. |
| `ClearQueue()` | Drops every waiting trap. |
| `StreamSafe` | Stream safe mode (saved in `menu.json`). |
| `RevealNames` | `false` hides the trap name in the start banner ("Trap!"). Not saved: set it each session. |
| `AllowInMissions`, `MissionTraps` | Whether the traps of `MissionTraps` can start during a mission or a replay. |
| `TrapStarted`, `TrapFinished` | Events with the trap id (and whether it was interrupted). |

`Trigger` only queues the trap: it starts as soon as the game allows it (see the README). A trap that needs a
situation (a vehicle, a weapon in hand, people around) waits for it, up to `MaxWait`. The queue survives script
reloads.

## Changes

- ApiVersion 4: `Trigger(id, source, out error)`, `QueuedTraps`, `RevealNames`. `TriggerAll` was removed.
- ApiVersion 3: `StreamSafe`.
- ApiVersion 2: `ActiveTraps`.

## Trap ids

`TrapIds`: `ScooterBrother`, `Ramp`, `AnimalHunt`, `AnimalTransform`, `FallingPlanes`, `TurboTraffic`,
`AlienAbduction`, `Irresistible`, `SecondComing`, `Wanted`, `Drunk`, `FlatTires`, `Disarmed`, `LesterTax`,
`Paparazzi`, `Clowns`, `MoonGravity`, `Blizzard`, `Nightfall`, `Ragdoll`, `LiftOff`, `PhantomCar`, `PhantomWedge`,
`RedLight`, `Speed`, `TimeMachine`, `Purge`, `Terminator`, `Matrix`, `MadMax`, `Jaws`, `MacheteGranny`,
`HereWeGoAgain`, `Bowling`, `ViceCity`, `BigSmoke`, `BattleRoyale`, `MarioKart`, `TopDown`, `UpsideDown`, `Clones`,
`Kidnap`, `ObjectRain`, `GiantBalls`, `FailedHeroes`, `Mariachi`, `FloatingCars`, `Kamikaze`, `FlashMob`,
`InitialD`, `ForrestGump`, `WalkingDead`, `Bouncy`.
