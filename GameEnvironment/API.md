# GameEnvironment API (ApiVersion 5)

Reference `GameEnvironment.dll` with **Copy Local = false**. Everything goes through the static class
`GameEnvironment.EnvironmentApi`. All calls must come from a script tick (main thread).

## State

```csharp
bool ready = EnvironmentApi.IsReady;
EnvironmentMode mode = EnvironmentApi.Mode;
bool sterile = EnvironmentApi.IsSterile;
bool replay = EnvironmentApi.IsReplayActive;
PlayableCharacter? current = EnvironmentApi.CurrentCharacter;
PlayableCharacter? start = EnvironmentApi.StartCharacter;
```

```csharp
EnvironmentApi.OpenDoorsEnabled = false;
bool open = EnvironmentApi.AreDoorsOpen;
```

`OpenDoorsEnabled` switches the free-roam door opening for the session (the config value is not written).
Turning it off locks the opened doors again right away. `AreDoorsOpen` is `true` while doors are held open.

`EnvironmentMode`: `Starting`, `Waiting` (save not loaded yet), `Sterile` (active), `StorySave` (normal save,
the mod does nothing), `Disabled` (unsupported game build).

Every method that changes something returns `false` / `null` with an error unless `Mode == Sterile`.
During a replay, changes are written to `state.json` and applied when the replay is over.

## Characters

```csharp
EnvironmentApi.UnlockCharacter(PlayableCharacter.Trevor);
bool unlocked = EnvironmentApi.IsCharacterUnlocked(PlayableCharacter.Michael);
IReadOnlyList<PlayableCharacter> list = EnvironmentApi.UnlockedCharacters;
EnvironmentApi.LockCharacter(PlayableCharacter.Trevor, out string error);
EnvironmentApi.SetNextStartCharacter(PlayableCharacter.Franklin);
EnvironmentApi.SetUnlockedCharacters(new[] { PlayableCharacter.Michael, PlayableCharacter.Trevor }, out error);
```

`LockCharacter` is refused unless `AllowRelock` is `true`, for the current character, and for the last
unlocked one. `SetNextStartCharacter` overrides `StartCharacter` for the next new game only (`null` clears it);
it is saved in `state.json`, so it survives a reload. `SetUnlockedCharacters` sets the exact list of unlocked
characters, ignoring `AllowRelock` (it is what a randomizer client uses to mirror its items). It refuses an empty
list. A locked current character stays playable until you switch away from it.

```csharp
string game = EnvironmentApi.GameId;
```

`GameId` identifies the sterile game (its creation date). It changes when a new game is started, which tells a
client that money, weapons and vehicles must be given again.

## Money and weapons

Always for the **current** character (the last one played if called during a replay).

```csharp
EnvironmentApi.GiveMoney(5000, out string error);
EnvironmentApi.GiveMoney(-1000, out error);
int cash = EnvironmentApi.GetMoney(PlayableCharacter.Franklin);
EnvironmentApi.GiveWeapon(WeaponHash.Pistol, 120, out error);
EnvironmentApi.RemoveWeapon(WeaponHash.Pistol);
IReadOnlyList<WeaponHash> weapons = EnvironmentApi.GetWeapons(PlayableCharacter.Franklin);
```

Granted weapons are given back if the character loses them after a load or a replay.

## Vehicles

```csharp
OwnedVehicle car = EnvironmentApi.GrantVehicle(VehicleHash.Banshee, out string error);
OwnedVehicle boat = EnvironmentApi.GrantVehicle("squalo", out error);
IReadOnlyList<OwnedVehicle> all = EnvironmentApi.Vehicles;
EnvironmentApi.RequestDelivery(car.Id, out error);
EnvironmentApi.RepairVehicle(car.Id, out error);
EnvironmentApi.RemoveVehicle(car.Id);
```

`OwnedVehicle`: `Id`, `Model`, `Name` (game language), `Status` (`Stored`, `Delivered`, `Destroyed`),
`DeliveryPending`, `RepairCost`, `IsDestroyed`. A granted vehicle goes to the garage; it is only
delivered on request (`RequestDelivery`), on the nearest safe road (never in the water, never while the player is
swimming, flying or falling). The first one shows a help text that explains the menu.
`RepairVehicle` takes the cost from the current character.

## Events

```csharp
EnvironmentApi.SterileGameCreated += start => { };
EnvironmentApi.StateApplied += () => { };
EnvironmentApi.CharacterUnlocked += character => { };
EnvironmentApi.VehicleGranted += vehicle => { };
EnvironmentApi.VehicleDestroyed += vehicle => { };
```

`StateApplied` fires after every load and every replay, once `state.json` has been re-applied. Remember
that SHVDN reloads every script after a replay: subscribe again in your constructor and keep your own state
on disk.

## Changes

- ApiVersion 5: `Owner` (who the sterile game belongs to) and `ResetProgress(owner, out error)` (clears money,
  weapons and vehicles, new `GameId`).
- ApiVersion 4: `GiveWeaponTo(character, weapon, ammo, out error)`, `SwitchCharacter(character, out error)` (on foot,
  outside missions and replays, unlocked characters only).
- ApiVersion 3: `SetUnlockedCharacters`, `GameId`.
- ApiVersion 2: `OpenDoorsEnabled`, `AreDoorsOpen`.
