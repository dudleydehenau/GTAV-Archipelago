# Changelog

## 2.0.3

- Skill checks count the progress made since the start of the Archipelago game, given by GameEnvironment 1.9.3: the
  game always puts the story skill values back, so the raw values sent checks at spawn. "Franklin: Stamina 10%" now
  means 10 % of the way from Franklin's starting stamina to 100 %.

## 2.0.2

- Skills only count once GameEnvironment has set this game's own skill values, so the story defaults (Michael's
  shooting, Trevor's flying...) no longer send dozens of skill checks at the start.

## 2.0.1

- Monkey mosaics are moved to the nearest spot you can walk to when they would sit behind a wall or an invisible
  barrier (for example under the bridge in Strawberry), up to 12 m away.
- Fix: "Base Jumping" and "Skydiving" only counted once every jump of that kind was done, because the game marks
  them in its completion list after the last one. The first base jump (from a cliff, a building or on a bike) and the
  first skydive (from a helicopter) now count.

## 2.0.0

- New collectibles: 50 monkey mosaics (on the ground in front of each mosaic), 27 peyote plants and 10 Epsilon tracts.
- Free-roam feats, reported through the same `Collected` event: 50 stunt jumps, Under the Bridge, 15 knife flights,
  44 landmarks with purple map markers, the skill levels of the three characters and police escapes.
- Activities: the game's completion flags of 67 activities are followed (`Activities`).
- `HiddenBlips`: collectible kinds whose map markers are hidden (set from F6 > Map); API `SetBlipHidden`,
  `IsBlipHidden`, `LandmarkBlips`.
- API 4: `Feats`, `SetDisabledFeats`, `SetLandmarkHints`, `GlobalsStatus`. `MarkCollected`, `Restore` and
  `IsCollected` accept feat ids too.

## 1.2.0

- The collectibles are now a pile of diamonds (`ch_prop_casino_diamonds_01a`), size 0.8, easier to spot. A
  `config.json` that still uses the old case is switched to the diamonds automatically.

## 1.1.0

- API 3: `SetDisabledKinds` removes whole kinds from the world (used by the Archipelago client for the kinds that
  are not checks in your seed).

## 1.0.0

- Release version: English only, no more development tools.
- Peyote plants removed (their positions cannot be known in advance): 160 collectibles.
- The 3D model preview was removed from the menu; the model is set with `Model` in `config.json`.
- The game's animal controller script is no longer stopped.
- Installs into the `Archipelago` folder (any `ScriptsLocation` works).
- Map markers come back by themselves when the game removes them during a load (they used to need a toggle).
- The F10 menu is off by default; API 2: `MarkerMode`, `Nearest()` (used by the Archipelago menu).

## 0.3.0

- Peyote plants (27): each real plant within `PeyoteScanRadius` is hidden and replaced by the 3D object, its position saved in `peyote.json`; the game's peyote script is stopped too.

## 0.2.1

- `Size`: shrinks or grows the 3D object (0.7 in the shipped config, 30 % smaller), also in the menu preview.

## 0.2.0

- F10 menu: live 3D preview of the models listed in `PreviewModels`, Enter applies the model to every collectible and saves it.

## 0.1.0

- 160 collectibles (50 spaceship parts, 50 letter scraps, 30 nuclear waste, 30 submarine parts) at the positions
  from the game's scripts, all shown as the same floating 3D object, available from the start.
- The game's own collectible scripts are stopped so nothing shows up twice.
- Collected ids saved in `collected.json`; F10 menu with counts, map markers and a route to the nearest one.
- API version 1: list, state, `Collected` event, `SetCollected` for the Archipelago client.
