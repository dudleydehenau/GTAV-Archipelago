# Changelog

## 1.9.0

- Ammu-Nation is replaced by a new shop (`CustomGunShop`): press E in any Ammu-Nation to browse. The camera turns to
  your character, who holds each weapon as you scroll; upgrades are shown on the weapon before you buy them. 81
  weapons, online ones included, in nine categories, plus ammo refills, upgrades and body armor. Weapons bought go to
  the three characters, upgrades are kept and put back after a death or a new game.
- API: `CanBuyWeapon`, `CanBuyUpgrades`, `WeaponNote`, `UpgradeNote`, `ShopPriceMultiplier`, `WeaponBought`,
  `UpgradeBought`, `IsShopOpen` (used by the Archipelago client for the weapon unlocks and the shopsanity).
- Game shops: barbers, clothes stores, tattoo parlors and Los Santos Customs report each purchase with its
  category (API: `GameShopPurchase`), and each kind of shop can be kept closed (`SetGameShopLocked`).
- Darts open for the three characters (the game only opened them after some story missions).
- The hunting icon is gone: Cletus's hunting needs story scripts the empty world does not run.
- The race icon next to the Yellow Jack Inn is gone: it belongs to a race cut from the game, it never starts.
- Locked shops open during missions: Blitz Play needs the mask shop and a Michael mission needs Ponsonbys.
- Bars, clubs and shops used by activities and missions are unlocked (Tequi-la-la, the Yellow Jack Inn, the golf club
  gates, the shooting ranges, Epsilon). The log lists the doors still locked at start.
- API: `FinishedActivityIcons` gives the map icons the game marked as done.
- With `CustomGunShop` on, every Ammu-Nation is open from the start (the sterile story never opens them).
- `OpenActivities` (on by default): golf, tennis, darts, yoga, the shooting ranges, Flight School, the stunt plane
  trials, the off-road, street and sea races, the triathlons, parachuting, hunting and Trevor's jobs are opened from
  the start, and every shop (clothes, barbers, tattoos, LS Customs...) is open.
- The map icons of the activities and shops (golf, tennis, races, Flight School, Ammu-Nation, barbers...) are put
  on the map: the sterile story never shows them.
- Big planes are delivered at an airfield instead of the street: the nearest of Los Santos International Airport,
  Sandy Shores Airfield and McKenzie Field (the airliner, the cargo plane and other very big ones always go to the
  airport). A waypoint is set, and the plane waits there for 30 minutes before going back to the garage.
- Planes and helicopters get their own map marker.
- `StartPosition` (`Random` by default): the sterile game starts at a random place of the map (82 spots spread
  from Paleto Bay to the docks), in the city (Legion Square) or at the character's house (`Home`). An older
  `RandomStartPosition` setting is still read. API: `SetNextStartPosition`.
- Activities can be started again: their launchers stopped right away because the game had not drawn their map
  icon. The icons GameEnvironment draws are now handed to the game as its own.
- The log lists the activity scripts running near you, to find why one does not start.
- Off-road races, triathlons and street races are all open from the start: the game normally opens them one by one
  (each win opens the next) or through Franklin's story.
- Ammu-Nation: the game's own shop script is now kept off everywhere, not only inside the shop. It was restarting
  when you walked out and kept blocking jumping, sprinting and the weapon wheel.
- Ammu-Nation: the weapon shown no longer makes the menu flicker while it loads, and the restrictions the game puts
  on you in a shop (no weapon switch, no weapon in hand) are lifted when you close the shop and when you leave.
- API: `StoreNearestVehicle` puts the closest delivered vehicle back in the garage.

## 1.8.0

- `StartCharacter` is now `Michael` by default (Trevor's trailer in Sandy Shores was a rough start).
- `NoSaves` (on by default): the mod no longer saves the game, and any save the game writes by itself during a
  sterile session (its own autosave right after the new game, for example) is deleted a few seconds later and when
  the game closes. Saves that were already there are never touched. Without any save, each start is a new game that
  keeps your progress: money, weapons, vehicles and unlocked characters stay exactly as you left them (nothing is
  given twice), and you start as the last character you played.
- `OpenGunShopsInMissions` (on by default): Ammu-Nation opens during missions and replays, then closes again, so a
  mission that sends you to buy a weapon works but nothing can be bought outside missions.

## 1.7.1

- Fall guard: for 25 s after a mission replay or a building swap (Lester's factory), a player who falls under the
  map is put back on the ground.

## 1.7.0

- Works with GTA V Legacy 1.0.3889 (latest) and still with 1.0.3725. The game version is detected at startup and the
  matching memory addresses are used. The log shows the detected build on the `Game data` line.
- The `Globals` section of `config.json` is no longer used.

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
