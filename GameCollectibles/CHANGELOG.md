# Changelog

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
