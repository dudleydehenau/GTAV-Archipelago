# Changelog

## 1.3.1

- Fix: the game closed when loading story mode with ScriptHookVDotNet nightly 196 or newer. That version needs a new
  file, `ScriptHookVDotNet.dll`, which the setup did not copy. Run the setup again and click Install / Update.
- ScriptHookVDotNet and LemonUI are now pinned to the versions tested with the mods (ScriptHookVDotNet nightly 195,
  LemonUI 2.2) instead of always taking the newest one, so a new release of either can no longer break the install.

## 1.3.0

- GTA V Legacy 1.0.3889 (latest) is now supported, as well as 1.0.3725.
- New look: a dark window with five numbered steps (game, ScriptHookV, mods, play, GTA Online and tools), each with a
  clear status.
- ScriptHookV: step-by-step instructions, the zip is found automatically in your Downloads folder (or dropped onto
  the window), and the setup warns when the installed ScriptHookV is not made for your game version.
- The game version check says whether your game is too old or newer than the mods.
- The details (log) are hidden until you need them, and open by themselves when something fails.

## 1.2.0

- Every mod and the APWorld now come from the releases of a single repository,
  [dudleydehenau/GTAV-Archipelago](https://github.com/dudleydehenau/GTAV-Archipelago).

## 1.1.0

- The ScriptHookVDotNet console (F4) and the reload key are turned off in `ScriptHookVDotNet.ini`, and
  `NativeTrainer.asi` is renamed `NativeTrainer.asi.disabled`. Both come back when you uninstall.

## 1.0.0

- First release: game detection, download and installation into the `Archipelago` folder, ScriptHookV from zip,
  APWorld installation, mods on/off for GTA Online, BattlEye help, uninstall.
