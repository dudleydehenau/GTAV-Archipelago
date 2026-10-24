# GameTraps

52 traps for GTA V story mode (Legacy, build 1.0.3725), made for the
[Archipelago](https://archipelago.gg) multiworld randomizer and usable by any other mod through a public API.

> [!WARNING]
> **Streamers: turn on stream safe mode.** The `Irresistible` trap turns people into Vanilla Unicorn dancers who
> give private dances, and some of these models can show nudity. Turn on **Stream safe** (Archipelago menu, F6 >
> Settings, or `"StreamSafe": true` in `config.json`) and the trap uses nightclub customers instead. You can also
> remove `Irresistible` from `enabled_traps` in your Archipelago options, or set `"Enabled": false` for it.
> Other traps show blood, explosions and gunfights, like the game itself.

## Requirements

- GTA V **Legacy** 1.0.3725, story mode, BattlEye turned off
- [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/)
- [ScriptHookVDotNet](https://github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases) 3.7 (nightly)
- [LemonUI](https://github.com/LemonUIbyLemon/LemonUI/releases) 2.x (`LemonUI.SHVDN3.dll`)

## Installation

The easiest way is the **GTA V Archipelago Setup**, which installs every mod at once.

By hand: copy `GameTraps.dll` and the `GameTraps` folder (with `config.json`) into the `Archipelago` folder of
your game (the folder set as `ScriptsLocation` in `ScriptHookVDotNet.ini`; it is `scripts` by default).

## The traps

| Id | Name | What happens |
|---|---|---|
| `ScooterBrother` | Scooter Brothers! | Trevor shouts his line: in person if you play Trevor, otherwise your phone rings, you take the call, hear him and he hangs up. Then every land vehicle within 100 m, yours included, becomes a Faggio. Your own GameEnvironment vehicle counts as destroyed. |
| `Ramp` | Ramp! | While you drive, a stunt ramp appears right in front of you (6 to 20 m, `SecondsAhead`), removed after `Lifetime`. If the ramp faces the wrong way, set `HeadingOffset` to 180. |
| `AnimalHunt` | Hunting season | Hostile predators (red blips) spawn around you and attack for `Duration`. |
| `AnimalTransform` | Wild side | You become a random, invincible animal for `Duration`. Clothes, health, armour, weapons, ammo, attachments and tints are given back. The character wheel is disabled meanwhile. |
| `FallingPlanes` | Mayday! | Empty planes dive out of the sky around you and explode on impact. |
| `TurboTraffic` | Nitro for everyone | Every car driven by a ped speeds up out of control for `Duration`. |
| `AlienAbduction` | Close encounter | A UFO beams you up and drops you on top of Mount Chiliad (`Destinations`) with the railgun, the Up-n-Atomizer and the Widowmaker (`PlayerWeapons`, taken back afterwards unless you already had them). In the alien vision, `Aliens` aliens in the full alien costume (default outfit, `AlienOutfit` to force parts) armed with Up-n-Atomizers attack from various distances and every dead one is replaced until the timer ends. You take hits and get thrown around but cannot die (`CannotDie`: fall and impact proof, `FightHealth` health; every hit throws you, `KnockbackForce`), 5 s of protection on landing, no police. |
| `Irresistible` | Irresistible | People on foot around you turn into Vanilla Unicorn dancers (`DancerModels`, or nightclub customers in stream safe mode, `SafeDancerModels`), run after you with hearts above their heads and give you the club's private, lap and pole dances. Every 5 s (`TipInterval`) you make it rain: $10 (`TipPerDancer`) per dancer close to you, really taken from your money. |
| `SecondComing` | Second coming | People around you turn into Jesus and pray while the Messiah slowly rises into the sky (light haze). You wake up in front of Hill Valley Church in Pacific Bluffs (`Destinations`), far from any mission start, in a strong psychedelic vision (`ChurchEffects`): you are fully healed, the worshippers lying dead in a circle rise again and dance (`Worshippers`, `ResurrectDelay`, `ResurrectEmotes`), then it rains fish, sharks and dolphins (`FishModels`, `FishCount`, `FishInterval`). 45 s in all. |
| `Wanted` | Wanted | `Stars` more wanted stars at once (2). |
| `Drunk` | Last round | A bottle drops at your feet, then drunk walk, camera and screen for `Duration` (60 s). |
| `FlatTires` | Flat tires | Every tire of your current vehicle bursts. |
| `Disarmed` | Butterfingers | The weapon in your hands (or your best one) is gone, also from GameEnvironment's state. |
| `LesterTax` | Lester's cut | Lester takes `Percent` % of your money (10 %) and texts you one of his excuses. |
| `Paparazzi` | Paparazzi | Photographers run after you and take pictures; if you drive, some follow you by car. |
| `Clowns` | Send in the clowns | A clown van arrives with hostile clowns armed with bats; every dead clown is replaced until the timer ends, in the peyote vision, with no police. |
| `MoonGravity` | Moon gravity | Low gravity for `Duration` (30 s). |
| `Blizzard` | North Yankton weather | Christmas weather with snow lying on the whole map (`GroundSnow`), tire and footprint tracks and icy roads (`IcyRoads`, `GripLevel`) for `Duration` (2 min), then everything goes back to normal. The ground snow natives were found thanks to SnowForSP by CruelMasterMC. |
| `Nightfall` | Nightfall | The clock jumps to `Hour` (midnight). |
| `Ragdoll` | Banana peel | Your character falls to the ground (also knocks you off a bike). |
| `LiftOff` | Lift-off | Gravity goes away for `Duration` (10 s): cars and people float up, then everything falls back. You are protected from the fall (`ProtectPlayer`). |
| `PhantomCar` | Phantom car | Night falls, red fog: a driverless hearse with red lights hunts you, charges you and vanishes to reappear right behind you (`VanishInterval`). It cannot be destroyed. Time, weather and filter go back to how they were afterwards. |
| `PhantomWedge` | Phantom Wedge | Only while you drive. Waves of indestructible, boosted Phantom Wedge trucks (`Cars`, `Waves`, `WaveInterval`, up to `MaxVehicles`) ram you at full speed (`RamSpeed`, m/s); a stuck or distanced truck comes back behind you. Anything in front of their wedge, cars and people, you included, is launched into the air. No police. |
| `RedLight` | Red light, green light | Squid Game: a big GREEN LIGHT / RED LIGHT sign. On red, after a short grace (`Grace`), every movement draws a sniper shot (`Damage`). 60 s. |
| `Speed` | Speed | Your car (or a bus if you are on foot) has a bomb: after 15 s (`ArmDelay`), dropping under 60 km/h (`MinSpeedKmh`) for more than 1.5 s, or getting out, blows it up and kills you (`SurviveBlast` to survive). Hold 45 s. |
| `TimeMachine` | Back to the future | You are put in a boosted Deluxo (`Boost`, `PowerMultiplier`). Hit 88 mph: fire trails, a flash, and you land in North Yankton in 1955 (`Past`), in black and white among old cars (`OldCars`), Hit 88 mph again there, this time flying (X), to leave (or wait `PastDuration`). Then Doc sends you to "1885" in Sandy Shores (`Present`)... before admitting it always looks like this there. |
| `Purge` | The Purge | Night, prison siren, no police, big countdown: people around you pick up bats, knives and pistols and attack everyone (`CrowdMax`), while masked bikers hunt you (`Hunters`). At the end the bikers vanish, people drop their weapons and the time goes back. |
| `Terminator` | Terminator | Lightning, then an invincible bouncer in black with sunglasses walks after you with a shotgun; if you drive, he chases you on a chopper (`BikeModel`). Too far away? "I'll be back" and he reappears near you. |
| `Matrix` | The Matrix | Bullet time (`TimeScale`), green screen and agents in suits; every agent you kill comes back as two (up to `MaxAgents`). |
| `MadMax` | Fury Road | Waves of rusty rat-rods and dune buggies (`Waves`) chase you with shotguns and molotovs. Empty vehicles lose their red marker. |
| `Jaws` | Jaws | Sharks keep falling from the sky until the end, aimed ahead of you when you drive (`Lead`). |
| `MacheteGranny` | Florida grannies | Only on foot. Grannies with machetes run at you (GTA 6 trailer nod); every dead one is replaced. No police. |
| `HereWeGoAgain` | Here we go again | "Ah shit, here we go again": Ballas in purple cars drive by and shoot at you, two Grove Street homies join you (`Friends`). |
| `Bowling` | Let's go bowling! | Roman floods your phone with bowling invitations, then a taxi picks you up by force (doors locked) and drives you to the "bowling alley". |
| `ViceCity` | Welcome to the 80s | Forced night, rain, pink filter, neon lights all around you and neon kits on every car (`Lights`, `NeonRadius`), two cops in white Infernus chase you with 2 stars. Everything is undone at the end, cops and stars included. |
| `BigSmoke` | Big Smoke's order | A Burger Shot employee appears and throws 40 burgers, fries and sodas at you while the full order scrolls in subtitles. Food that gets stuck in the air is removed. |
| `BattleRoyale` | Battle royale | A purple zone (map circle and wall) 320 to 450 m away shrinks for 90 s. You have 30 s (`Grace`) to reach it, then you lose health outside. Inside waits Jimmy (`EnemyModel`), the last one standing: kill him for the Top 1. |
| `MarioKart` | Rainbow Road | While you drive, glowing banana peels appear on the road and spin you out; after 20 s, a blue shell homes in on you. It turns slowly (`ShellTurnRate`) and gives up after 15 s, so it can be dodged. |
| `TopDown` | Grand Theft Auto 1997 | Top-down camera like GTA 1 for 30 s. Movement follows the screen (up, down, left, right, and diagonals). On foot, you face and shoot at the mouse cursor (it snaps to people nearby); with a controller you shoot at the nearest attacker. |
| `UpsideDown` | Upside down | The camera turns upside down for 20 s. |
| `Clones` | Attack of the clones | 10 copies of your character appear: some run, some steal cars, some shoot in the air. Police units are already there (no spawn effect), with 2 stars; most officers go after the clones. |
| `Kidnap` | Taxi | A white tuned taxi (plate TAXI 13) picks you up by force: Daniel drives flat out for 30 s, doors locked, with his best lines. |
| `ObjectRain` | Cloudy with a chance of toilets | Toilets, trolleys, TVs, couches, cows and pigs fall from the sky around you (`PropModels`, `PedModels`). |
| `GiantBalls` | Indiana Jones | Giant stunt footballs roll down the nearest slope at you. |
| `FailedHeroes` | Tighten | Megamind nod: Impotent Rage plays Tighten, the hero gone bad, leaping at you and knocking you down; Pogo plays Minion, the fish in the monkey suit. Every dead one is replaced. No police. |
| `Mariachi` | Mariachi | A mariachi band plays around you; when you get away they pop up next to you or ahead of your car and keep playing. |
| `FloatingCars` | Flying cars | Cars around you lose gravity and float a few meters up with their drivers, then fall back. Yours stays on the road (`IncludePlayer`). |
| `Kamikaze` | Kamikaze pedestrians | While you drive, pedestrians run across the road right in front of you. |
| `FlashMob` | Flash mob | Everybody around you dances the same dance at the same time and follows you; the dance changes every 8 s. |
| `InitialD` | Initial D | Night falls and you are put in a two-tone Futo at the top of the Galileo Observatory road, with a rival on your tail. Drive down the mountain to the tofu shop (yellow route, `Delivery`) before the timer ends without spilling the water (violent moves spill it): you are paid up to $5,000 (`Reward`) according to the water left. Assisted drift: hold the handbrake or steer hard to slide, the slide angle (`MaxDriftAngle`) and the spin (`MaxYawRate`) are capped so it never turns into a spinning top. DEJA VU on every slide. |
| `ForrestGump` | Run, Forrest, run! | Unlimited stamina, and a growing crowd of people runs behind you in a pack wherever you go, stopping when you stop. Forrest's lines in subtitles. On foot. |
| `WalkingDead` | The Walking Dead | Halloween night (`Weather`), city blackout (`Blackout`) and washed-out colors (`Timecycle`), all undone at the end: people around you turn into the Vinewood street zombie (`Model`) and more shamble in with a drunk walk (`Clipset`); they attack bare-handed and keep coming until the end. No police. |

## How traps run

A timer bar shows how long a timed trap lasts.

Traps can overlap (`StackTraps`), at most `MaxActive` at a time (2), started `GapBetweenTraps` ms apart, under
strict rules so they never fight each other:

- **Heavy traps always run alone**: nothing else starts while they run, and they wait until nothing else runs.
  These are the traps that teleport you, put you in a vehicle, take over the camera, the time scale or the whole
  street: `AlienAbduction`, `AnimalTransform`, `SecondComing`, `TimeMachine`, `Kidnap`, `Bowling`, `InitialD`,
  `Speed`, `BattleRoyale`, `Purge`, `ViceCity`, `WalkingDead`, `PhantomCar`, `Matrix`, `TopDown`, `UpsideDown`,
  `Clones`, `PhantomWedge`, `MadMax`, `HereWeGoAgain`, `Irresistible`. The next trap waits at least 2.5 s after a
  heavy one ends.
- **Light traps** (`Ramp`, `FlatTires`, `Disarmed`, `LesterTax`, `Wanted`, `Nightfall`, `Ragdoll`) can join any
  trap that is not heavy.
- **Every other trap** is medium: one medium trap at a time, plus light ones.
- Some traps need a situation and wait for it: **on foot** (not in a vehicle, not swimming) for `AnimalHunt`,
  `AnimalTransform`, `AlienAbduction`, `SecondComing`, `Kidnap`, `Bowling`, `TimeMachine`, `Clowns`, `Irresistible`,
  `FlashMob`, `ForrestGump`, `WalkingDead`, `Matrix`, `Drunk`, `BigSmoke`, `MacheteGranny`, `FailedHeroes`;
  **driving a land vehicle** for `Ramp`, `MarioKart`, `Kamikaze`, `PhantomWedge`. No trap starts while you fall,
  parachute, ragdoll or get into a vehicle.
- On top of that, two traps never share the same thing: the player, the screen vision, gravity, the weather, the
  police, the phone, the camera or the time scale. When a trap is received, only "X traps waiting..." is shown (`AnnounceQueue`), so the surprise stays until it starts. A trap that needs a situation waits for it instead of
being lost: the ramp waits until you drive, flat tires until you are in a vehicle, butterfingers until you
hold a weapon on foot, the ragdoll until you are on foot, people traps until someone is around, and most
traps until you are no longer flying. Other traps go ahead meanwhile. After `MaxWait` ms (3 minutes) a
waiting trap is dropped. A trap waits while a mission, a replay, a cutscene, a
character switch, a loading screen or the pause menu is active, and a running trap is stopped and cleaned up
as soon as a mission or a replay starts (if it began less than 20 s before, it is queued again). Nothing starts on
the mission results screen, and after a mission or a replay the queue waits until the game has been back to normal
for 10 s in a row (screen faded in, player in control, no loading). The queue is saved in `queue.json`,
so a trap received during a replay runs afterwards. A transformation still active when the scripts reload is undone from `transform.json`.

With Archipelago, the received trap is never named in the notifications: you only learn which one it is when it
starts. Set `RevealNames` to `false` (the Archipelago client does it for you) and the start banner only says
"Trap! Courtesy of <player>". The Archipelago menu (F6 > Recent traps) still lists the exact traps you received.

## Menu (F9)

Off by default (`"EnableMenu": false`), because it lets you start any trap. Turn it on to test traps: it lists
every trap, a random trap, stream safe mode and a button to clear the queue. `MenuKey` changes the key.

## Configuration

`Archipelago/GameTraps/config.json`. Durations and intervals are in milliseconds, distances in metres.

| Setting | Default | Description |
|---|---|---|
| `EnableMenu` | `false` | Shows the F9 test menu. |
| `MenuKey` | `"F9"` | Key of the menu ([key names](https://learn.microsoft.com/dotnet/api/system.windows.forms.keys)). |
| `VerboseLog` | `false` | More details in `GameTraps.log`. |
| `Announce` | `true` | Big banner when a trap starts. |
| `RevealNames` | `true` | Shows the name of the trap in that banner. `false` keeps the surprise ("Trap!"). |
| `AllowInMissions` | `false` | Lets the traps of `MissionTraps` start during a mission or a replay (the Archipelago client sets it from your options). The other traps wait for the end of the mission. |
| `MissionTraps` | 27 traps | The traps allowed in missions: the ones that cannot break a mission (Animal Transform is never allowed, it changes the character model) (Mayday, Matrix, Terminator, Walking Dead, Phantom Car, Jaws...). |
| `GapBetweenTraps` | `1500` | Delay between two traps that start together. |
| `StackTraps` | `true` | Lets compatible traps run at the same time (see the rules above). |
| `MaxActive` | `2` | Most traps running at the same time. |
| `AnnounceQueue` | `true` | "X traps waiting..." when traps are queued. |
| `StreamSafe` | `false` | Never uses dancer or hooker models. Also in the menus, saved in `menu.json`. |
| `MaxWait` | `180000` | A trap that cannot start after this delay is dropped. |

Every trap has its own block with `Enabled` (set it to `false` to never run that trap) and its settings.
`Emotes` entries are either `{ "Scenario": "WORLD_HUMAN_..." }` or `{ "Dict": "...", "Name": "..." }`; a missing
animation falls back to a scenario and is logged. `Destinations` entries are `{ "X", "Y", "Z", "Heading" }`.

<details><summary><code>ScooterBrother</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Radius` | `100` |
| `MaxVehicles` | `40` |
| `Models` | `faggio2`, `faggio`, `faggio3` |
| `Line` | object |

</details>

<details><summary><code>Ramp</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Models` | `prop_mp_ramp_03` |
| `HeadingOffset` | `0` |
| `MinDistance` | `6` |
| `MaxDistance` | `20` |
| `SecondsAhead` | `0.45` |
| `Lifetime` | `20000` |
| `MinSpeed` | `8` |

</details>

<details><summary><code>AnimalHunt</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Count` | `4` |
| `SpawnDistance` | `35` |
| `Duration` | `60000` |
| `Models` | `a_c_mtlion`, `a_c_coyote`, `a_c_rottweiler`, `a_c_shepherd`, `a_c_husky` |

</details>

<details><summary><code>AnimalTransform</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `60000` |
| `Models` | list of 16 |

</details>

<details><summary><code>FallingPlanes</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Count` | `5` |
| `Height` | `160` |
| `SpawnRadius` | `120` |
| `TargetRadius` | `25` |
| `Speed` | `55` |
| `Interval` | `1500` |
| `Models` | `jet`, `cuban800`, `duster`, `mammatus`, `titan`, `luxor`, `shamal` |

</details>

<details><summary><code>TurboTraffic</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `30000` |
| `Radius` | `300` |
| `Speed` | `60` |
| `Boost` | `8` |

</details>

<details><summary><code>AlienAbduction</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `UfoModel` | `"p_spinning_anus_s"` |
| `AlienModel` | `"s_m_m_movalien_01"` |
| `AlienWeapons` | `WEAPON_RAYPISTOL` |
| `AlienAccuracy` | `25` |
| `PlayerWeapons` | `WEAPON_RAILGUN`, `WEAPON_RAYPISTOL`, `WEAPON_RAYMINIGUN` |
| `RemoveWeaponsAfter` | `true` |
| `InvinciblePlayer` | `false` |
| `CannotDie` | `true` |
| `FightHealth` | `1000` |
| `KnockbackForce` | `9` |
| `AlienOutfit` | empty list |
| `SpawnProtection` | `5000` |
| `PlayerAmmo` | `9999` |
| `Aliens` | `5` |
| `MinDistance` | `12` |
| `MaxDistance` | `40` |
| `UfoHeight` | `35` |
| `BeamDuration` | `5000` |
| `FightDuration` | `60000` |
| `Destinations` | list of 1 |

</details>

<details><summary><code>Irresistible</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Radius` | `80` |
| `MaxPeds` | `10` |
| `Duration` | `45000` |
| `EmoteChange` | `9000` |
| `TipPerDancer` | `10` |
| `TipInterval` | `5000` |
| `TipDistance` | `3` |
| `TipEmote` | object |
| `TipEffectAsset` | `"scr_xs_celebration"` |
| `TipEffect` | `"scr_xs_money_rain"` |
| `DancerModels` | list of 4 |
| `SafeDancerModels` | list of 5 |
| `Emotes` | list of 9 |

</details>

<details><summary><code>SecondComing</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Model` | `"u_m_m_jesus_01"` |
| `Radius` | `60` |
| `MaxPeds` | `20` |
| `AscendDuration` | `10000` |
| `AscendSpeed` | `1.2` |
| `VisionDuration` | `45000` |
| `Emotes` | list of 3 |
| `Destinations` | list of 1 |
| `AnchorModel` | `"prop_golf_ball"` |
| `MessiahEmote` | `{"Dict": "amb@world_human_yoga@male@base", "Name": "base_a"}` |
| `AscensionBlur` | `0.6` |
| `Worshippers` | `8` |
| `ResurrectDelay` | `5000` |
| `ResurrectEmotes` | list of 4 |
| `FishModels` | `a_c_fish`, `a_c_fish`, `a_c_fish`, `a_c_sharktiger`, `a_c_dolphin` |
| `FishCount` | `40` |
| `FishInterval` | `350` |
| `WorshipperModels` | list of 5 |
| `ChurchEffects` | `DrugsTrevorClownsFight`, `DMT_flight` |

</details>

<details><summary><code>Wanted</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Stars` | `2` |

</details>

<details><summary><code>Drunk</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `60000` |
| `Clipset` | `"move_m@drunk@verydrunk"` |
| `Timecycle` | `"Drunk"` |
| `BottleModels` | list of 4 |

</details>

<details><summary><code>FlatTires</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |

</details>

<details><summary><code>Disarmed</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |

</details>

<details><summary><code>LesterTax</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Percent` | `10` |
| `Minimum` | `0` |

</details>

<details><summary><code>Paparazzi</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Count` | `6` |
| `Cars` | `2` |
| `Duration` | `45000` |
| `SpawnDistance` | `30` |
| `Model` | `"a_m_m_paparazzi_01"` |
| `CarModel` | `"blista"` |

</details>

<details><summary><code>Clowns</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Count` | `4` |
| `Duration` | `90000` |
| `SpawnDistance` | `60` |
| `VanModel` | `"speedo2"` |
| `ClownModel` | `"s_m_y_clown_01"` |
| `Weapon` | `"WEAPON_BAT"` |
| `VisionDuration` | `0` |

</details>

<details><summary><code>MoonGravity</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `30000` |
| `Level` | `1` |

</details>

<details><summary><code>Blizzard</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `120000` |
| `Weather` | `"XMAS"` |
| `SnowTracks` | `true` |
| `GroundSnow` | `true` |
| `IcyRoads` | `true` |
| `IcyRadius` | `250` |
| `GripLevel` | `3` |

</details>

<details><summary><code>Nightfall</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Hour` | `0` |

</details>

<details><summary><code>Ragdoll</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `4000` |

</details>

<details><summary><code>LiftOff</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `10000` |
| `Radius` | `120` |
| `UpSpeed` | `2.5` |
| `ProtectPlayer` | `true` |

</details>

<details><summary><code>PhantomCar</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `60000` |
| `Speed` | `30` |
| `ChargeDistance` | `45` |
| `VanishInterval` | `12000` |
| `Timecycle` | `"glasses_red"` |
| `Weather` | `"FOGGY"` |
| `Hour` | `0` |
| `Models` | `romero`, `hearse` |

</details>

<details><summary><code>PhantomWedge</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `60000` |
| `Cars` | `5` |
| `Passengers` | `0` |
| `Accuracy` | `0` |
| `Color` | `-1` |
| `SpawnDistance` | `60` |
| `Boost` | `14` |
| `RamSpeed` | `42` |
| `MaxVehicles` | `18` |
| `VehicleModels` | `phantom2`, `phantom` |
| `PedModels` | `s_m_y_dockwork_01`, `s_m_m_trucker_01` |
| `Weapons` | empty list |
| `Waves` | `5` |
| `WaveInterval` | `9000` |

</details>

<details><summary><code>RedLight</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `60000` |
| `MinGreen` | `3000` |
| `MaxGreen` | `6000` |
| `MinRed` | `3000` |
| `MaxRed` | `5000` |
| `Grace` | `700` |
| `MoveTolerance` | `0.6` |
| `Damage` | `25` |
| `ShotInterval` | `600` |
| `Weapon` | `"WEAPON_SNIPERRIFLE"` |

</details>

<details><summary><code>Speed</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `45000` |
| `MinSpeedKmh` | `60` |
| `ArmDelay` | `15000` |
| `Tolerance` | `1500` |
| `SurviveBlast` | `false` |
| `BusModels` | `bus`, `coach` |

</details>

<details><summary><code>TimeMachine</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `90000` |
| `TargetMph` | `88` |
| `Boost` | `7` |
| `PowerMultiplier` | `2` |
| `FireTrails` | `true` |
| `PastDuration` | `120000` |
| `PastTimecycle` | `"NG_filmnoir_BW01"` |
| `PastWeather` | `"SNOWLIGHT"` |
| `Year` | `1955` |
| `OldCarCount` | `10` |
| `OldDriver` | `"a_m_m_farmer_01"` |
| `FarWestYear` | `1885` |
| `FarWestTimecycle` | `"glasses_brown"` |
| `Models` | `deluxo` |
| `OldCars` | list of 10 |
| `Past` | object |
| `Present` | object |

</details>

<details><summary><code>Purge</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `60000` |
| `Hour` | `23` |
| `Hunters` | `4` |
| `CrowdRadius` | `80` |
| `CrowdMax` | `20` |
| `Alarm` | `true` |
| `Models` | list of 5 |
| `Weapons` | list of 5 |
| `CrowdWeapons` | list of 5 |

</details>

<details><summary><code>Terminator</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `60000` |
| `Model` | `"s_m_m_bouncer_01"` |
| `Weapon` | `"WEAPON_PUMPSHOTGUN"` |
| `Accuracy` | `8` |
| `SpawnDistance` | `30` |
| `CatchUpDistance` | `120` |
| `BikeModel` | `"bagger"` |

</details>

<details><summary><code>Matrix</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `40000` |
| `TimeScale` | `0.45` |
| `Agents` | `3` |
| `MaxAgents` | `10` |
| `Accuracy` | `15` |
| `Timecycle` | `"glasses_green"` |
| `Models` | `s_m_m_highsec_01`, `s_m_m_highsec_02`, `s_m_m_fiboffice_01` |
| `Weapons` | `WEAPON_PISTOL`, `WEAPON_COMBATPISTOL` |

</details>

<details><summary><code>MadMax</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `75000` |
| `Cars` | `3` |
| `Passengers` | `2` |
| `Accuracy` | `15` |
| `Color` | `-1` |
| `SpawnDistance` | `70` |
| `Boost` | `4` |
| `VehicleModels` | `ratloader`, `ratloader2`, `dune`, `bfinjection`, `rebel`, `technical` |
| `PedModels` | list of 5 |
| `Weapons` | `WEAPON_SAWNOFFSHOTGUN`, `WEAPON_MICROSMG`, `WEAPON_MOLOTOV` |
| `Waves` | `5` |
| `WaveInterval` | `15000` |

</details>

<details><summary><code>Jaws</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `40000` |
| `RainInterval` | `600` |
| `Lead` | `1.8` |
| `MaxFallen` | `30` |
| `SharkModels` | `a_c_sharktiger`, `a_c_sharkhammer` |

</details>

<details><summary><code>MacheteGranny</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `60000` |
| `Count` | `5` |
| `SpawnDistance` | `45` |
| `Respawn` | `true` |
| `MoveRate` | `1.2` |
| `Accuracy` | `20` |
| `Models` | list of 6 |
| `Weapons` | `WEAPON_MACHETE` |

</details>

<details><summary><code>HereWeGoAgain</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `60000` |
| `Cars` | `2` |
| `Passengers` | `3` |
| `Accuracy` | `20` |
| `Color` | `145` |
| `SpawnDistance` | `70` |
| `Boost` | `4` |
| `VehicleModels` | `buccaneer`, `voodoo`, `emperor`, `manana` |
| `PedModels` | `g_m_y_ballaorig_01`, `g_m_y_ballaeast_01`, `g_m_y_ballasout_01` |
| `Weapons` | `WEAPON_MICROSMG`, `WEAPON_PISTOL` |
| `Friends` | `2` |
| `FriendModels` | `g_m_y_famca_01`, `g_m_y_famdnf_01`, `g_m_y_famfor_01` |
| `FriendWeapons` | `WEAPON_PISTOL`, `WEAPON_MICROSMG` |
| `Waves` | `3` |
| `WaveInterval` | `20000` |

</details>

<details><summary><code>Bowling</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `35000` |
| `VehicleModel` | `"taxi"` |
| `DriverModel` | `"s_m_m_gentransport"` |
| `Speed` | `22` |
| `Crazy` | `false` |

</details>

<details><summary><code>ViceCity</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `60000` |
| `Timecycle` | `"glasses_pink"` |
| `Weather` | `"RAIN"` |
| `Hour` | `22` |
| `Cars` | `2` |
| `Stars` | `2` |
| `VehicleModels` | `infernus` |
| `CopModels` | `s_m_y_cop_01` |
| `Lights` | `12` |
| `NeonRadius` | `80` |

</details>

<details><summary><code>BigSmoke</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Model` | `"u_m_y_burgerdrug_01"` |
| `Count` | `40` |
| `Interval` | `250` |
| `ThrowSpeed` | `14` |
| `FoodModels` | list of 5 |

</details>

<details><summary><code>BattleRoyale</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `90000` |
| `StartRadius` | `200` |
| `EndRadius` | `30` |
| `MinOffset` | `320` |
| `MaxOffset` | `450` |
| `EnemyModel` | `"ig_jimmydisanto"` |
| `EnemyName` | `"Jimmy"` |
| `EnemyWeapon` | `"WEAPON_PISTOL"` |
| `EnemyHealth` | `400` |
| `Grace` | `30000` |
| `DamagePerTick` | `4` |
| `DamageInterval` | `600` |

</details>

<details><summary><code>MarioKart</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `45000` |
| `Peels` | `12` |
| `PeelInterval` | `2500` |
| `SpinTime` | `1500` |
| `BlueShell` | `true` |
| `ShellDelay` | `20000` |
| `ShellSpeed` | `32` |
| `PeelModels` | `ng_proc_food_nana1a`, `prop_bananas`, `prop_food_bs_chips` |
| `ShellTurnRate` | `1.2` |
| `ShellLifetime` | `15000` |

</details>

<details><summary><code>TopDown</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `30000` |
| `Height` | `35` |

</details>

<details><summary><code>UpsideDown</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `20000` |
| `Height` | `0` |

</details>

<details><summary><code>Clones</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `60000` |
| `Count` | `10` |
| `Stars` | `2` |
| `Weapon` | `""` |
| `PoliceUnits` | `3` |
| `CopModel` | `"s_m_y_cop_01"` |
| `PoliceCars` | `police`, `police2`, `police3` |

</details>

<details><summary><code>Kidnap</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `30000` |
| `VehicleModel` | `"asterope"` |
| `DriverModel` | `"a_m_y_stlat_01"` |
| `Speed` | `45` |
| `Crazy` | `true` |

</details>

<details><summary><code>ObjectRain</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `30000` |
| `Count` | `45` |
| `Interval` | `400` |
| `Spread` | `18` |
| `MinHeight` | `18` |
| `MaxHeight` | `32` |
| `PropModels` | list of 8 |
| `PedModels` | `a_c_cow`, `a_c_pig`, `a_c_hen` |

</details>

<details><summary><code>GiantBalls</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `30000` |
| `Interval` | `3500` |
| `Distance` | `45` |
| `Speed` | `16` |
| `MaxBalls` | `8` |
| `Models` | list of 4 |

</details>

<details><summary><code>FailedHeroes</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `60000` |
| `Count` | `4` |
| `SpawnDistance` | `40` |
| `Respawn` | `true` |
| `MoveRate` | `1.1` |
| `Accuracy` | `20` |
| `Models` | `u_m_y_imporage`, `u_m_y_pogo_01` |
| `Weapons` | `WEAPON_UNARMED`, `WEAPON_BAT` |

</details>

<details><summary><code>Mariachi</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `60000` |
| `Count` | `3` |
| `Scenario` | `"WORLD_HUMAN_MUSICIAN"` |
| `Models` | `s_m_m_mariachi_01` |
| `FollowDistance` | `9` |

</details>

<details><summary><code>FloatingCars</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `25000` |
| `Radius` | `120` |
| `HoverHeight` | `6` |
| `UpSpeed` | `1.5` |
| `IncludePlayer` | `false` |

</details>

<details><summary><code>Kamikaze</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `30000` |
| `Interval` | `1200` |
| `MinAhead` | `25` |
| `MaxAhead` | `45` |
| `Models` | list of 5 |

</details>

<details><summary><code>FlashMob</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `40000` |
| `Radius` | `60` |
| `MaxPeds` | `25` |
| `SongLength` | `8000` |
| `FollowDistance` | `7` |
| `Emotes` | list of 4 |

</details>

<details><summary><code>InitialD</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `120000` |
| `Hour` | `4` |
| `Drift` | `true` |
| `SpillThreshold` | `10` |
| `SpillRate` | `1.2` |
| `RivalDriver` | `"a_m_y_ktown_01"` |
| `Models` | `futo` |
| `RivalModels` | `zr350`, `elegy`, `sultan` |
| `Destinations` | list of 1 |
| `Reward` | `5000` |
| `DriftSlide` | `0.25` |
| `MaxDriftAngle` | `35` |
| `MaxYawRate` | `2.2` |
| `DeliveryRadius` | `8` |
| `Delivery` | object |

</details>

<details><summary><code>ForrestGump</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `45000` |
| `Radius` | `80` |
| `MaxPeds` | `20` |
| `MinFollowers` | `10` |
| `FollowDistance` | `3` |
| `RunSpeed` | `3` |

</details>

<details><summary><code>WalkingDead</code></summary>

| Setting | Default |
|---|---|
| `Enabled` | `true` |
| `Duration` | `60000` |
| `Model` | `"u_m_y_zombie_01"` |
| `Clipset` | `"move_m@drunk@verydrunk"` |
| `Count` | `14` |
| `Convert` | `10` |
| `Radius` | `60` |
| `SpawnDistance` | `35` |
| `MoveRate` | `0.85` |
| `Health` | `250` |
| `Hour` | `23` |
| `Weather` | `"HALLOWEEN"` |
| `Blackout` | `true` |
| `Timecycle` | `"NG_filmnoir_BW01"` |
| `TimecycleStrength` | `0.35` |

</details>

## Files

| File | Content |
|---|---|
| `GameTraps/config.json` | Settings. |
| `GameTraps/menu.json` | Stream safe choice made in a menu. |
| `GameTraps/queue.json` | Traps waiting to start (kept across reloads). |
| `GameTraps/transform.json` | Undo data of an animal transformation in progress. |
| `GameTraps/GameTraps.log` | Log. |

## Building

Open `GameTraps.sln` in Visual Studio 2022 (.NET Framework 4.8). Put `ScriptHookVDotNet3.dll` and
`LemonUI.SHVDN3.dll` in `lib` first (they are referenced only). The build output contains `GameTraps.dll` and
`GameTraps/config.json`.

## For developers

See [API.md](API.md).

## Credits

The ground snow natives of `Blizzard` were found thanks to SnowForSP by CruelMasterMC.
