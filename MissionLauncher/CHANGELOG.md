# Changelog

## 1.5.0

- Works with GTA V Legacy 1.0.3889 (latest) and still with 1.0.3725. The game version is detected at startup and the
  matching memory addresses are used. The log shows the detected build on the `Game data` line.
- The `Globals` section of `config.json` is no longer used.

## 1.4.4

- A replay session can no longer stay open forever: 15 s after the game's replay controller is gone, the session is
  closed (the mission markers stay hidden while a session is open).

## 1.4.3

- False passes: 1.4.2 could report a mission as passed the moment it started (one of the new pass signals is also
  on when nothing plays), which sent its checks right away. Now no pass is accepted in the first 15 s of an attempt,
  a "mission passed" flag already on at that point counts as left over from the previous mission, and the mission
  complete jingle only counts when it starts during the attempt.
- The "Completion" and "Medal" lines of the assassination missions are no longer counted as objectives.

## 1.4.2

- Passes were no longer reported in 1.4.1 (clearing the game's "mission passed" flag at the start of an attempt was
  a bad idea, it is gone). A pass is now detected by the game's "mission passed" flag or by the mission complete
  screen and jingle, and an attempt whose script ends is only called abandoned after 6 s without a pass or fail
  signal. This also covers the stale flag that made "Deep Inside" look abandoned in 1.4.0.

## 1.4.1

- A pass could be reported as "Abandoned": the game keeps its "mission passed" flag set after a pass (even in the
  save), so the next mission's pass was not seen. The flag is now cleared when an attempt starts.

## 1.4.0

- API 7: `CurrentObjectives` gives the medal objectives of the running attempt live, so another mod can show which
  ones are left.

## 1.3.0

- Release version: English only (mission names still come from the game, in the game's language).
- API 6: `ReplayGate` and `LockReason` (another mod can refuse a replay and say why; refused missions are greyed
  out in the menu), `UseApiMarkers` (switch the markers to `Api` mode at runtime).
- The Merryweather Heist variant is named "Freighter" instead of "Submersible".
- Installs into the `Archipelago` folder (any `ScriptsLocation` works).
- Mission markers are rebuilt when the game removed their blips during a load (they used to need a script reload).
- The F7 menu is off by default.

## 1.2.2

- Restore point prompt: the game may freeze every script while the prompt is open, so nothing running inside
  the game could answer it. A small watchdog outside the game's script loop now presses Enter when the scripts
  stop for more than 0.4 s after a replay request (game window focused, at most 10 presses, only until the
  restore point is done). The log says how long the scripts were frozen and how many presses were sent.

## 1.2.1

- Fix: the restore point prompt was not confirmed. It is drawn by the game itself while the replay save is
  pending, not as a script warning screen, so it was never detected. It is now detected from the replay save
  status, confirmed with the frontend accept controls, then with a real Enter key press (held 80 ms, sent as a
  scan code) when the game window has focus. The log tells how it was confirmed.

## 1.2.0

- Mission markers on the map, taken from the game's own start points (`missions.json`, embedded; a copy in
  `scripts/MissionLauncher` overrides it): the game's own icon for each mission, in the colour of the character who
  starts it, and only for the character you are playing. Walking into the marker starts the mission. When several
  missions share a place, a chooser appears (arrows to choose, E to start). Completed missions show a tick and are
  replayed with E.
- `Markers`: `All` (every replayable mission), `Api` (only what another mod enables) or `Off`. `MarkerAutoStart`, `MarkerRadius`.
- The restore point prompt shown when a replay starts is confirmed automatically (`AutoConfirmRestorePoint`,
  `RestorePointKeyFallback`).
- API 5: `MissionMarkerState`, `GetMissionMarker`, `SetMissionMarker`, `ClearMissionMarkers` (saved in `markers.json`).

## 1.1.0

- The catalog now only contains what the game itself allows to replay (mission flags for the story, the game's own list for Strangers and Freaks: 46 of 63). Mission numbers changed.
- Missions not completed yet are shown too; `OnlyCompletedMissions` gives exactly the pause menu list.
- Official-style names: *The Big Score* preps by location (Pillbox Hill, Rockford Hills, Mission Row), endings labelled A / B / C.
- New settings: `MenuOrder` (`Story` or `Completion`) and `OnlyCompletedMissions`.
- API 4: `ReplayMission.IsCompleted`, `CompletionOrder`, `MissionApi.GetReplayableMissions`, `MissionApi.RefreshCompletion`.

## 1.0.0

- Replay any story mission (numbered in story order) or Strangers and Freaks mission (101–163) through the game's own replay system.
- In-game menu (F7) with game-language mission names; identical names are completed with their variant.
- Public API: mission catalog, `RequestReplay`, replay / attempt / result events with medal and gold objectives.
- `MissionResults.log` with every replay result.
