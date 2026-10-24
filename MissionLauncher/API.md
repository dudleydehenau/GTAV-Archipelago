# MissionLauncher API (ApiVersion 7)

Reference `MissionLauncher.dll` from your mod with **Copy Local = false**. Players install MissionLauncher once
in the scripts folder (`Archipelago`). Everything goes through the static class `MissionLauncher.MissionApi`.

## Identifiers

| | Format | Example |
|---|---|---|
| Story mission | `Number` = position in story order, `Id` = 2 digits | `1` / `"01"` = Prologue |
| Strangers and Freaks | `Number` = 101 and up, replayable ones only (`GameIndex` = game index) | `101` |
| Gold objective | `Number × 100 + position` | `401` / `"0401"`, `10101` |

Numbers are fixed: story missions follow the story order, Strangers and Freaks follow the game's replay list. Set `"VerboseLog": true` to get the full list in
`MissionLauncher.log`.

## Catalog

```csharp
bool ready = MissionApi.IsReady;
IReadOnlyList<ReplayMission> all = MissionApi.Missions;
IReadOnlyList<ReplayMission> story = MissionApi.GetMissions(ReplayCategory.Story);
ReplayMission mission = MissionApi.FindMission(4);
IReadOnlyList<ReplayMission> replayable = MissionApi.GetReplayableMissions(ReplayCategory.Story);
```

`ReplayMission`: `Number`, `Id`, `Category`, `Code` (`"ARM3"`, `"RC_ABI1"`), `Title` (game language),
`TitleLabel`, `GameIndex`, `IsCompleted`, `CompletionOrder`, `ObjectiveId(position)`, `ObjectiveCode(position)`.

`Missions` / `GetMissions` contain every mission the game can replay. `GetReplayableMissions` returns what the
game's pause menu would offer: only the missions completed in the current save.
`IsCompleted` / `CompletionOrder` (0 = first mission completed, -1 = not completed) are refreshed by that call,
by `MissionApi.RefreshCompletion()` and before every replay request.

## Starting a mission

```csharp
if (!MissionApi.RequestReplay(4, out string error))
    GTA.UI.Notification.PostTicker(error, false);
```

Refused when MissionLauncher is not ready, a replay or a mission is already running, or the player is dead.
Missions not completed in the save can be requested too (a line is logged); the game's own menu never offers them.
`MissionApi.IsReplayActive` and `MissionApi.CurrentReplay` describe the running replay.
`MissionApi.CurrentObjectives` reads the medal objectives of the running attempt live (text, current value,
target, whether it is still on track); it is empty before the mission registers them.

## Mission markers

With `"Markers": "Api"` in `config.json`, MissionLauncher only shows the missions your mod enables:

```csharp
MissionApi.SetMissionMarker(4, MissionMarkerState.Available);
MissionApi.SetMissionMarker(4, MissionMarkerState.Replay);
MissionApi.SetMissionMarker(4, MissionMarkerState.Hidden);
MissionApi.ClearMissionMarkers();
MissionMarkerState state = MissionApi.GetMissionMarker(4);
```

`Available`: the mission starts as soon as the player walks into its marker. `Replay`: the blip shows a tick and
the mission starts with E. States are saved in `Archipelago/MissionLauncher/markers.json` and survive the script reload
that follows every replay. A marker is only shown to a character that can start the mission in the game; several
missions at the same place share one blip and a chooser.

## Locking missions

```csharp
MissionApi.ReplayGate = mission => unlocked.Contains(mission.Code) ? null : "Locked: you need this mission's item.";
MissionApi.UseApiMarkers = true;
string reason = MissionApi.LockReason(mission);
```

`ReplayGate` is asked before every replay request (menu, markers and `RequestReplay`): return `null` to allow the
mission, or the reason shown to the player to refuse it. The F7 menu greys out refused missions with that reason.
`UseApiMarkers` switches the markers to `Api` mode at runtime, whatever `Markers` says in `config.json`. Both are
reset when the scripts reload: set them again from your script.

## Results

```csharp
MissionApi.ReplayStarted += mission => { };

MissionApi.ReplayAttemptStarted += e => { };

MissionApi.ReplayAttemptEnded += e =>
{
    switch (e.Outcome)
    {
        case ReplayOutcome.Passed:
            foreach (MissionObjective o in e.Objectives)
            {
            }
            break;
        case ReplayOutcome.Failed:
            break;
        case ReplayOutcome.Abandoned:
            break;
    }
};

MissionApi.ReplayFinished += e => { };
```

| Event | Data |
|---|---|
| `ReplayStarted` | `ReplayMission` (null if the mission is not in the catalog) |
| `ReplayAttemptStarted` | `Mission`, `Attempt` (1, then 2 after *Retry*…) |
| `ReplayAttemptEnded` | `Mission`, `Attempt`, `Outcome`, `Medal`, `Gold`, `Percent`, `Objectives`, `FailReasonLabel`, `FailReasonText`, `Duration` |
| `ReplayFinished` | `Mission`, `Attempts`, `LastOutcome` |

`MissionObjective`: `Id`, `Code`, `Position`, `Passed`, `Text`, `Value`, `Target`, `LowerIsBetter`,
`StatType`, `StatId`, `Label`, `FormatValue(int)`.

Missions without medal objectives pass with `Medal = None` and an empty objective list.

Replays started from the game's pause menu are tracked too.

## Important: scripts are reloaded after a replay

When a replay ends, the game restores the previous state and ScriptHookVDotNet restarts every script,
including yours. Save what you need (to a file, for example) as soon as you receive `ReplayAttemptEnded`.
`ReplayFinished` is raised either right before the reload or about three seconds after the scripts restart.

Events are raised on MissionLauncher's script thread. An exception thrown by a handler is logged and ignored.

## Changes

- ApiVersion 7: `CurrentObjectives`.
- ApiVersion 6: `ReplayGate`, `LockReason`, `UseApiMarkers`.
- ApiVersion 5: mission markers (`SetMissionMarker`, `GetMissionMarker`, `ClearMissionMarkers`).
