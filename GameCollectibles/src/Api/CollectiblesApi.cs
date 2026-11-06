using System;
using System.Collections.Generic;
using System.Linq;
using GameCollectibles.Core;

namespace GameCollectibles
{
    public static class CollectiblesApi
    {
        public const int ApiVersion = 4;

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

        public static bool IsCollected(string id) => Core != null && !string.IsNullOrEmpty(id) && Core.Store.Contains(id);

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

        public static IReadOnlyList<FeatInfo> Feats =>
            Core == null ? (IReadOnlyList<FeatInfo>)Array.Empty<FeatInfo>() : Core.Feats.All.Select(FeatOf).ToList();

        public static IReadOnlyCollection<FeatKind> DisabledFeats =>
            Core == null ? (IReadOnlyCollection<FeatKind>)Array.Empty<FeatKind>() : Core.Feats.DisabledKinds;

        public static void SetDisabledFeats(IEnumerable<FeatKind> kinds) => Core?.Feats.SetDisabled(kinds);

        public static void SetLandmarkHints(IEnumerable<string> ids) => Core?.Feats.SetHints(ids);

        public static bool IsBlipHidden(CollectibleKind kind) => Core != null && Core.Config.HiddenBlips.Contains(kind.ToString());

        public static void SetBlipHidden(CollectibleKind kind, bool hidden)
        {
            if (Core == null) return;
            string key = kind.ToString();
            bool has = Core.Config.HiddenBlips.Contains(key);
            if (hidden == has) return;
            if (hidden) Core.Config.HiddenBlips.Add(key);
            else Core.Config.HiddenBlips.Remove(key);
            Core.RefreshBlips();
            Core.Config.Save();
        }

        public static bool LandmarkBlips
        {
            get => Core != null && Core.Config.LandmarkBlips;
            set
            {
                if (Core == null || Core.Config.LandmarkBlips == value) return;
                Core.Config.LandmarkBlips = value;
                Core.Feats.SetHints(Core.Feats.Hints);
                Core.Config.Save();
            }
        }

        public static string GlobalsStatus => Core?.Feats.GlobalsStatus ?? "not loaded";

        public static event Action<string> Collected;

        internal static void RaiseCollected(string id) => Collected?.Invoke(id);

        private static FeatInfo FeatOf(FeatInfoSource feat) => new FeatInfo(feat.Id, feat.Kind, feat.Name, feat.Position, Core.Store.Contains(feat.Id));

        private static CollectibleInfo Info(Spot spot) => new CollectibleInfo(spot.Id, spot.Kind, spot.Number, spot.Position, Core.Store.Contains(spot.Id));
    }
}
