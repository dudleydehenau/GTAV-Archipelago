# Changelog

## 1.6.2

- Melee weapons (machete, knife...) are no longer "given back their ammo" every second.

## 1.6.1

- During a mission replay, GameEnvironment no longer touches the characters: it used to make all three available
  when a replay started, which can get in the way of a scripted character switch (Father/Son: Michael's model with
  Franklin's voice, and a second Franklin outside the car). The Archipelago client already makes sure every
  character of the mission is unlocked.
- During a replay, the special abilities are only unlocked (no longer forced on), and the wheel refresh waits for
  the end of the mission.
- Ammo above the game's maximum is brought down to that maximum (it was "restored" every second in a loop).

## 1.6.0

- Received vehicles go to the garage and are no longer delivered on their own (they could land in the water or
  fall from the sky). `DeliverGrantedVehicles` is gone.
- A delivery is refused while you swim, fly or fall, and the vehicle appears on the nearest road away from water
  and other vehicles; if there is none, you are asked to go to a street.

## 1.5.1

- `RepairCurrentVehicle`: repairs, on the spot, the garage vehicle you are sitting in.

## 1.5.0

- API 5: `Owner` and `ResetProgress(owner)`: the Archipelago client binds the sterile game to its slot and clears the
  money, weapons and vehicles of another slot's game.

## 1.4.0

- Weapons are guarded: every second, a weapon of your save that the game took away is given back, and ammo that
  drops without you firing is put back (the game restores its own inventory after loads and missions, which
  emptied the weapons and removed the grenade launcher). Ammo you really fire is saved.
- A locked character is switched to an unlocked one automatically (new game, loaded game, connection), also in a
  vehicle: you stay in your seat.

## 1.3.1

- Ammo: only the weapon in your hands can lose ammo in the save, and nothing is read in the 5 s after the weapons
  are given back (a reload could save an empty weapon before the ammo was restored).
- Wheels: they are set again every time you get into a vehicle, twice (at once and 3 s later), including the car the
  game puts you back in after a mission; an invalid wheel type or wheel index is replaced by the stock wheels.

## 1.3.0

- Ammo is saved: the ammo of your weapons is tracked while you play and given back after a reload, a mission or a
  character switch (it used to come back empty when the game kept the weapon without its ammo).
- `UnlockSpecialAbilities`: Michael's, Franklin's and Trevor's special abilities are unlocked and kept on,
  missions included.

## 1.2.0

- API 4: `GiveWeaponTo(character, ...)` (a weapon for one character, even when you play another one) and
  `SwitchCharacter(character)` (switches to an unlocked character on foot, outside missions).
- Invisible wheels: the wheels of every vehicle you get into, and of every garage vehicle delivered, are set again
  once the vehicle's parts are streamed in, which brings back the rims and tyres the game sometimes forgets after a
  load or a mission.

## 1.1.0

- During a mission replay every character is available, so a mission with several protagonists never spawns a copy
  of one of them or puts the player in the wrong skin. The roster is put back after the replay.
- Vehicle looks: an invalid wheel type or wheel index is no longer applied (it could leave a car without rims or
  tyres after a mission).

## 1.0.0

- Release version: English only. The debug shortcuts (`DebugKeys`, Ctrl+Shift+1..9) were removed.
- API 3: `SetUnlockedCharacters` (exact list of unlocked characters, used by the Archipelago client) and `GameId`.
- Installs into the `Archipelago` folder (any `ScriptsLocation` works).
- An unlocked character shows up in the character wheel at once (it used to need a script reload).
- A weapon you already own gets the ammo added instead of nothing.
- The F8 menu is off by default.

## 0.1.8

- Open doors in free roam: some missions start inside buildings that the halted story keeps locked
  (Floyd's apartment, Lester's house, the safehouses…). Their doors are unlocked near the player and given
  back to the game as soon as a replay is requested or a mission runs. Configured in `OpenDoors`.
- API 2: `OpenDoorsEnabled`, `AreDoorsOpen`.

## 0.1.7

- No more obsolete ScriptHookVDotNet calls (`Game.IsLoading`, `World.GetGroundHeight`); x64 build target.

## 0.1.6

- Fix: the character wheel never opened in a new sterile game. The prologue disables the wheel when it starts and only re-enables it when it ends normally; since the prologue is stopped early, the wheel stayed disabled. It is now re-enabled after the prologue is stopped and on every apply.

## 0.1.5

- Fix: the character wheel never opened in a new sterile game. The halted story never released the prologue's mission slot, so the game still believed a story mission was running and never started the character switch script. The slot is now released exactly like the official replay does.

## 0.1.4

- Fix: the character wheel never opened. The story had queued an important call that never fires while the story is halted, and the game blocks switching while one is pending. The communication queue is now emptied in a sterile game (`BlockCommunications`, also stops story calls, texts and emails).
- Fix: crash to desktop when starting a New Game from a story save. The minimap message is no longer drawn while the screen is faded, loading or switching, or while the player is not in control.
- The story flow busy flag is cleared only 5 s after the sterile state is applied, once the screen is faded in.

## 0.1.2

- Fix: the character wheel never opened in a sterile game (the story flow was halted while marked busy).
- Fix: delivered vehicles keep their colours and modifications (saved every 2 s and restored on delivery).
- Fix: no duplicate vehicle when a delivery is requested while driving it or standing next to it.
- Message above the minimap when a story save is loaded (the mod is inactive, start a New Game). `ShowInactiveNotice: false` hides it.
- `DebugKeys`: Ctrl+Shift+1..9 test shortcuts (unlock characters, money, pistol, vehicles, state), logged and shown as notifications. Turn it off for release.

## 0.1.0

- New game: halts the story flow, stops the prologue, cleans up North Yankton and starts in Los Santos as a
  random or chosen character, with an autosave.
- Character wheel locking through the game's own availability flags (greyed out), never re-locked by default.
- `state.json` as the source of truth: unlocks, money and weapons per character, vehicles. Re-applied after
  every load and every replay.
- Vehicles delivered next to the player, stored when left behind, repaired for a fee when destroyed.
- Random events disabled.
- F8 menu and public API (ApiVersion 1).
- Story saves are detected and never modified.
