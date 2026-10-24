using System;
using System.Collections.Generic;
using System.Linq;
using GameCollectibles.Core;

namespace GameCollectibles
{
    public static class CollectiblesApi
    {
        public const int ApiVersion = 3;

        private static CollectibleRunner Core => CollectibleRunner.Instance;

        public static bool IsReady => Core != null && Core.IsReady;

        public static int Total => Core?.Spots.Count ?? 0;

        public static int CollectedCount => Core?.CollectedCount ?? 0;

        public static IReadOnlyList<CollectibleInfo> All =>
            Core == null ? (IReadOnlyList<CollectibleInfo>)Array.Empty<CollectibleInfo>() : Core.Spots.Select(Info).ToList();

        public static IReadOnlyList<string> CollectedIds =>
            Core == null ? (IReadOnlyList<string>)Array.Empty<string>() : Core.Spots.Where(s => Core.Store.Contains(s.Id)).Select(s => s.Id).ToList();

        public static CollectibleInfo Get(string id)
        {
            Spot spot = Core?.Find(id);
            return spot == null ? null : Info(spot);
        }

        public static bool IsCollected(string id) => Core != null && Core.Find(id) != null && Core.Store.Contains(id);

        public static bool MarkCollected(string id) => Core != null && Core.MarkCollected(id);

        public static bool Restore(string id) => Core != null && Core.Restore(id);

        public static void SetCollected(IEnumerable<string> ids) => Core?.ReplaceCollected(ids);

        public static void ResetAll() => Core?.ReplaceCollected(Enumerable.Empty<string>());

        public static string MarkerMode
        {
            get => Core?.Config.BlipMode.ToString() ?? "Nearby";
            set
            {
                if (Core == null || !Enum.TryParse(value, true, out BlipMode mode)) return;
                Core.Config.BlipMode = mode;
                Core.RefreshBlips();
                Core.Config.Save();
            }
        }

        public static IReadOnlyCollection<CollectibleKind> DisabledKinds =>
            Core == null ? (IReadOnlyCollection<CollectibleKind>)Array.Empty<CollectibleKind>() : Core.DisabledKinds;

        public static void SetDisabledKinds(IEnumerable<CollectibleKind> kinds) => Core?.SetDisabled(kinds);

        public static CollectibleInfo Nearest()
        {
            Spot spot = Core?.Nearest();
            return spot == null ? null : Info(spot);
        }

        public static event Action<string> Collected;

        internal static void RaiseCollected(string id) => Collected?.Invoke(id);

        private static CollectibleInfo Info(Spot spot) => new CollectibleInfo(spot.Id, spot.Kind, spot.Number, spot.Position, Core.Store.Contains(spot.Id));
    }
}
