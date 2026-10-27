# Changelog

## 1.4.0

- New trap: **Boing Boing** (`Bouncy`). For 30 s, every car and every person around you bounces like a ball each
  time they touch the ground, you included (53 traps).
- Only the traps that move you (Alien Abduction, Second Coming, Time Machine, Initial D, Kidnap, Bowling) still run
  alone. Every other trap can run with others, unless both change the same thing. `MaxActive` is now 4, so a big
  wave of traps empties faster.
- `DebugAllTraps` and `DebugKey` (F11): for testing, the key starts every enabled trap at once.

## 1.3.3

- Animal Transformation can no longer start during a mission, whatever `MissionTraps` says: it changes the player
  model, and when the mission switched character meanwhile (Father/Son), the old model came back over the new
  character.

## 1.3.2

- Traps stayed "waiting" forever since 1.3.0: one of the "end of mission" checks is also on when nothing happens, so
  the queue never started. It is gone.
- After a mission, the queue now waits until the game is back to normal (screen faded in, player in control, no
  loading) for 10 s in a row; the count starts again from zero if the game reloads or fades meanwhile. Outside
  missions, 1.5 s of calm is enough.
- The reason why traps wait is written to `GameTraps.log`.

## 1.3.1

- Animal transformation: every animal is now fully protected (some, like the hen, could still die from a fall,
  water or an explosion).

## 1.3.0

- No trap starts on the mission results screen any more: the end of a mission (results, medal screen, return to
  the open world) blocks every trap, and the queue waits 6 s after a mission or a replay before starting again.
- A trap cut short by a mission (start or end) less than 20 s after it began is queued again instead of being lost.

## 1.2.0

- The Cayo Perico getaway trap is removed: the island does not load properly in story mode (floating grass, no
  ground, and the map stays broken until the game is restarted). 52 traps.

## 1.1.0

- Traps in missions: `AllowInMissions` (and `TrapApi.AllowInMissions`) lets the traps listed in `MissionTraps` start
  during a mission or a replay (Mayday, dog transform, Matrix, Terminator, Walking Dead, Phantom car...). The other
  traps wait for the mission to end, as before.

## 1.0.0

- Release version: every text and joke is in English, the French texts are gone.
- `RevealNames` (and `TrapApi.RevealNames`): the start banner can hide the trap name ("Trap! Courtesy of Alice").
- `TrapApi.Trigger(id, source, out error)` keeps who sent the trap, shown when it starts; `TrapApi.QueuedTraps`.
- The queue file now stores the sender (old queues are still read).
- The F9 menu is off by default, and its test entries ("Stack every trap", "Show my position") are gone.
  `TrapApi.TriggerAll` was removed.
- Installs into the `Archipelago` folder (any `ScriptsLocation` works).
- Stacking rules to stop crashes: heavy traps (teleports, forced vehicles, camera, time scale, big crowds) always run
  alone, one medium trap at a time, light traps join freely, `MaxActive` (2) traps at most.
- Phantom Wedge: only while you drive; 5 boosted, armoured trucks per wave, 5 waves, they ram you at full speed
  (`RamSpeed`, `MaxVehicles`) and come back behind you when stuck or left behind.
- Florida grannies and Tighten: only start when you are on foot.
- Irresistible, Lester's cut and Initial D change your money through GameEnvironment, so the tips are really paid.
- New trap: Cayo Perico getaway (53 traps).
- Start conditions checked for every trap: on foot for the hunting season, the taxi rides, the time machine and every
  trap that spawns things around you or puts you in a vehicle; driving a land vehicle for the road traps; no trap
  while you fall, parachute, ragdoll or get into a vehicle.

## 0.10.0

- GTA 1 camera: movement follows the screen (up is up, diagonals work), shooting no longer turns the camera.
- Clones: the officers really go after the clones.
- World War Z removed (52 traps).
- Red light, green light lasts 60 s.
- Speed: 60 km/h, 15 s to reach it.
- Jaws: sharks always fall from the sky (sharks in the water did not attack).

## 0.9.5

- Initial D: deliver the tofu to the shop at the bottom of the mountain (yellow route and marker, `Delivery`), 2 minutes; pay depends on the water left.

## 0.9.4

- Initial D: water sensitivity halfway between 0.9.2 and 0.9.3 (`SpillThreshold` 10, `SpillRate` 1.2, faster reaction to hits).

## 0.9.3

- Back to the future: flight (X) is blocked on the way to North Yankton; to leave 1955 you must hit 88 mph again while flying. The boost only pushes forward along the ground, no more light floating. Doc's Sandy Shores joke stays 5 s longer.
- Initial D: the water only spills on violent moves (smoothed, `SpillThreshold` 13, `SpillRate` 0.8).

## 0.9.2

- Initial D: drift always on, but assisted and light (`DriftSlide`, `MaxDriftAngle`, `MaxYawRate`) instead of losing all grip.

## 0.9.1

- Back to the future: 1955 in North Yankton (black and white, old cars), then "1885" in Sandy Shores and Doc's joke; no screen effect left behind.
- Initial D: normal handling by default (`Drift` to slide), $5,000 if the water arrives intact (`Reward`).
- GTA 1 camera: you face and shoot straight at the cursor instead of aiming at the sky.

## 0.9.0

- New traps: Run, Forrest, run! and The Walking Dead (Vinewood street zombie). 53 traps.
- The Walking Dead: Halloween weather, city blackout and washed-out colors, back to normal at the end.

## 0.8.1

- Build fix in Back to the future (forward speed is read from the velocity).
- Benny Hill becomes World War Z (`WorldWarZ`).

## 0.8.0

- New trap: Initial D (Futo, mountain pass, rival, water cup, DEJA VU). 51 traps.
- Phantom Wedge: waves, and the wedge launches cars and people into the air.
- Phantom car, Purge, Vice City: time, weather and filter go back to how they were afterwards.
- Back to the future: boosted Deluxo, landing in Sandy Shores in 1955 (black and white), then back to the present.
- Speed: 45 km/h, and the explosion kills you (`SurviveBlast` to survive).
- Purge: big countdown; at the end the bikers vanish and nobody keeps attacking.
- Terminator: bouncer in black with sunglasses, chases you on a chopper when you drive.
- Jaws: sharks until the end, falling ahead of your car.
- Fury Road: waves; empty vehicles lose their red marker. Convoys of every trap get waves.
- Vice City: neon everywhere, forced night, cops and stars removed at the end.
- Big Smoke: physical food, floating food removed.
- Battle royale: zone far away with 30 s to reach it, Jimmy waiting inside.
- Rainbow Road: the blue shell can be dodged.
- GTA 1 camera: aim with the mouse cursor, or at the nearest attacker with a controller.
- Clones: police already there, clones steal cars and shoot in the air.
- Five stars driver becomes Taxi (Daniel, TAXI 13). Failed heroes become Tighten and Minion (Megamind).
- Mariachi: they never run off and always play again.

## 0.7.1

- Build fix: wanted stars for Vice City and clones are set through the game, radius blips use `Blip.Create`.

## 0.7.0

- 29 new traps, 50 in all. Pop culture: phantom car, Phantom Wedge convoy, red light green light, Speed, back to
  the future, the Purge, Terminator, the Matrix, Fury Road, Jaws, World War Z. GTA: Florida grannies, here we go
  again, let's go bowling, Vice City, Big Smoke's order. Games: battle royale, Rainbow Road. Chaos: GTA 1
  camera, upside down, clones, crazy driver, rain of toilets, giant balls, failed heroes, mariachi, flying cars,
  kamikaze pedestrians, flash mob.
- New channels so traps that use a scripted camera or the time scale never run together.
- API version 3: the new ids in `TrapIds`.

## 0.6.5

- Second coming: stronger haze while the Messiah rises (`AscensionBlur` 0.6) and the trip to the church
  comes 5 s sooner (`AscendDuration` 10 s).
- Teleports look for the ground just under the given height first, so you no longer land on a roof; the
  church spot is now the middle of the lawn in front of it (-1704.29, -297.78, 50.89, heading 317,
  `Clearance` 0: you land exactly there).
- Menu: "Show my position" writes your position and heading to `GameTraps.log`, ready to paste in a
  `Destinations` entry.
- Every teleport writes the asked point and the landing point to `GameTraps.log`.

## 0.6.4

- Second coming: the Messiah no longer comes back down. Miracles at the church instead: you are fully healed,
  dead worshippers rise again and dance, then it rains fish, sharks and dolphins. The vision there is much
  stronger (`ChurchEffects`); 45 s in all.

## 0.6.2

- Second coming: you wake up in front of Hill Valley Church in Pacific Bluffs instead of the Epsilon mansion,
  where the game's own Epsilon launcher started the final mission even 25 m away. No mission starts within 240 m.
- Close encounter: aliens always wear the full alien costume (no men in black, no missing heads);
  `AlienOutfit` can force each part.
- Close encounter: the Up-n-Atomizer throws you again (no more explosion proof, only fall and impact proof),
  and every hit also triggers a ragdoll push away from the alien (`KnockbackForce`).

## 0.6.1

- Close encounter: explosion and fall proof during the fight with 1000 health (`FightHealth`); the Up-n-Atomizer
  still throws you around and hurts, but can no longer kill you in one hit.
- Irresistible: make it rain, $10 per dancer within 3 m every 5 s (`TipPerDancer`, `TipInterval`, `TipDistance`).
- Stream safe mode is off by default and can be switched in the F9 menu (saved in `menu.json`). Off: Vanilla
  Unicorn dancers; on: nightclub customers. API: `TrapApi.StreamSafe`.

## 0.6.0

- Compatible traps run at the same time (`StackTraps`); traps that would fight over the player, the vision,
  gravity, weather, police or phone wait for each other. One timer bar per running trap.
- Received traps only show "X traps waiting..." (`AnnounceQueue`), keeping the surprise.
- Menu: "Stack every trap" queues every enabled trap at once, to test stacking.
- Irresistible: dressed nightclub dancers; `StreamSafe` refuses stripper and hooker models (one showed nudity).
- Scooter Brothers: Trevor's whole call plays (the call no longer hangs up during a pause between lines).
- API 2: `TriggerAll`, `ActiveTraps`.

## 0.5.1

- Irresistible: dancers in Vanilla Unicorn outfits (`DancerModels`).
- Scooter Brothers: Trevor's call cannot be hung up (phone controls locked until he hangs up).

## 0.5.0

- Night shift merged into Irresistible: dressed women give you Vanilla Unicorn dances (strip club animations);
  passers-by are turned into women when there are not enough. The `NightShift` trap id is removed.
- Scooter Brothers: real phone call when you are not Trevor (ringtone, phone to the ear, hang-up).
- Ramp: much closer (6 to 20 m), hard to avoid.
- Close encounter: every alien has an Up-n-Atomizer; you take hits but cannot die (`CannotDie`), 5 s of
  protection on landing, no police during the fight.
- Second coming: light haze while the Messiah rises so he stays visible; you wake up away from the Epsilon
  mission marker (`Clearance`), so the mission no longer starts.
- Last round: physics bottles, and the bottle is put on the ground if it stays in the air.
- Clowns: no police while you fight them.

## 0.4.1

- North Yankton weather: snow lying on the ground (`GroundSnow`), with the same game functions as SnowForSP
  (no memory patch needed). Everything is reset when the trap ends.

## 0.4.0

- Trevor's line now plays with his voice, built exactly like the game's dialogue handler (in person as Trevor,
  as a phone call otherwise).
- Wild side: fixed a freeze when transforming twice in a row (weapon attachments are only read when the weapon
  can take them, the parachute is skipped), 10 s rest between two transformations, step-by-step log.
- Close encounter: invincible player, railgun, Up-n-Atomizer and Widowmaker for the fight, aliens with ray guns
  that keep attacking.
- Second coming: smooth, slow ascension; wake up at the Epsilon mansion in Rockford Hills.
- Last round: the bottle falls to the ground.
- Clowns: peyote vision for the whole trap.
- North Yankton weather: icy roads, snow footstep and wheel effects.

## 0.3.0

- Timer bar on every timed trap. Screen effects (alien, peyote, Messiah visions) now always end.
- Scooter Brothers: your vehicle is always transformed (an owned GameEnvironment vehicle counts as destroyed),
  and Trevor's line plays (in person as Trevor, otherwise as a call from Trevor).
- Wild side: the animal is invincible (dying as an animal left an endless loading screen).
- Close encounter: top of Mount Chiliad, minigun, endless alien fight until the timer ends.
- Second coming: the Messiah rises into the sky, then you wake up at the Epsilon Program center.
- Last round: 60 s and a bottle drops at your feet.
- Butterfingers: the weapon is removed, also from GameEnvironment's state.
- Lester's cut: proper Lester excuses.
- Clowns: endless clowns until the timer ends.
- North Yankton weather: Christmas snow with tracks, 2 minutes.
- New trap: Lift-off (10 s without gravity, then everything falls back).

## 0.2.1

- Build fix: the wanted trap class clashed with `GTA.Wanted`. No more obsolete ScriptHookVDotNet calls.

## 0.2.0

- 11 new traps: wanted, drunk, flat tires, butterfingers, Lester's cut, paparazzi, clowns, moon gravity,
  blizzard, nightfall, banana peel.
- A trap that needs a situation now waits for it instead of being skipped (`MaxWait`, 3 minutes); other
  traps go ahead meanwhile. The ramp only appears while you drive.
- Scooter Brothers radius: 100 m.

## 0.1.0

- First version: 10 traps (Scooter Brothers, ramp, hunting animals, animal transformation, falling planes,
  turbo traffic, alien abduction, night shift, irresistible, second coming), persistent queue, F9 test menu,
  API 1.
