# Changelog

## 1.4.1

- The welcome screen and the Play step say to keep Rockstar cloud saves off: with them on, story mode can stay stuck
  on the loading screen.
- The Play step explains the start in three steps: choose Story Mode on the start screen, connect in the menu that
  opens by itself, then New Game.
- More > Collect logs for a bug report: one zip on your desktop with the ScriptHookV, ScriptHookVDotNet and mod logs,
  the settings, your Archipelago save, the game version and the list of files and story saves. Your Archipelago
  password is left out. It works even when the mods do not load in game.

## 1.4.0

- New look, matching the project page: dark, yellow and condensed titles.
- The setup is now a wizard with one step per screen: game folder, ScriptHookV, mods, then BattlEye and play. A rail at
  the top shows where you are and what is done, and Back / Next take you through it.
- Each step says in plain words what to do next, and Next only lights up when the step is ready.
- ScriptHookV: three numbered actions, and the setup keeps watching your Downloads folder for the zip.
- Installing the mods is one big button. Picking components and keeping settings moved under "Choose what to install".
- GTA Online mode, the mods folder, the details, the guide and Uninstall are in the bottom bar and its "More" menu.
- The details open in their own window, by themselves when an install fails.

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
