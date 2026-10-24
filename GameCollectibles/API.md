# GameCollectibles API (ApiVersion 3)

Reference `GameCollectibles.dll` with **Copy Local = false** and use the static class `GameCollectibles.CollectiblesApi`.

```csharp
CollectiblesApi.Collected += id => SendCheck(id);
foreach (CollectibleInfo info in CollectiblesApi.All) Log(info.Id, info.Kind, info.Number, info.Position, info.Collected);
CollectiblesApi.SetCollected(serverCheckedIds);
```

| Member | Description |
|---|---|
| `IsReady` | `true` once the script has ticked. |
| `Total`, `CollectedCount` | Number of collectibles and how many are collected. |
| `All` | Every collectible as `CollectibleInfo` (`Id`, `Kind`, `Number`, `Position`, `Collected`). |
| `CollectedIds` | Ids already collected. |
| `Get(id)` | One collectible, or `null`. |
| `IsCollected(id)` | Whether it is collected. |
| `MarkCollected(id)` | Marks it collected and removes it from the world, without raising `Collected`. |
| `Restore(id)` | Puts it back in the world. |
| `SetCollected(ids)` | Replaces the whole collected list (for example from the Archipelago server). |
| `ResetAll()` | Puts everything back. |
| `MarkerMode` | Map markers: `"None"`, `"Nearby"` or `"All"` (saved in `config.json`). |
| `Nearest()` | The closest collectible left, or `null`. Disabled kinds are skipped. |
| `DisabledKinds`, `SetDisabledKinds(kinds)` | Kinds removed from the world (no object, no marker, cannot be picked up). |
| `Collected` | Event raised with the id when the player picks one up. |

`CollectibleKind`: `SpaceshipPart`, `LetterScrap`, `NuclearWaste`, `SubmarinePart`. Ids look like `SpaceshipPart_01`;
`Number` is 1-based and follows the game's order.
