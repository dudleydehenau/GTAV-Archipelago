using System;
using System.Collections.Generic;
using MissionLauncher.Core;

namespace MissionLauncher
{
    public static class MissionApi
    {
        public const int ApiVersion = 7;

        public static bool IsReady => Launcher.Instance != null && Launcher.Instance.IsReady;

        public static IReadOnlyList<ReplayMission> Missions =>
            Launcher.Instance?.Catalog.All ?? (IReadOnlyList<ReplayMission>)Array.Empty<ReplayMission>();

        public static IReadOnlyList<ReplayMission> GetMissions(ReplayCategory category)
        {
            ReplayCatalog catalog = Launcher.Instance?.Catalog;
            if (catalog == null) return Array.Empty<ReplayMission>();
            return category == ReplayCategory.Story ? catalog.Story : catalog.Strangers;
        }

        public static IReadOnlyList<ReplayMission> GetReplayableMissions(ReplayCategory category)
        {
            ReplayCatalog catalog = Launcher.Instance?.Catalog;
            if (catalog == null || !catalog.Built) return Array.Empty<ReplayMission>();
            catalog.RefreshCompletion();
            return catalog.Visible(category, true, false);
        }

        public static void RefreshCompletion() => Launcher.Instance?.Catalog.RefreshCompletion();

        public static ReplayMission FindMission(int number) => Launcher.Instance?.Catalog.Find(number);

        public static bool RequestReplay(int number, out string error)
        {
            Launcher launcher = Launcher.Instance;
            if (launcher == null)
            {
                error = "MissionLauncher is not loaded.";
                return false;
            }
            return launcher.RequestReplay(number, out error);
        }

        public static bool RequestReplay(int number) => RequestReplay(number, out _);

        public static MissionMarkerState GetMissionMarker(int number) =>
            Launcher.Instance?.Markers.Get(number) ?? MissionMarkerState.Hidden;

        public static void SetMissionMarker(int number, MissionMarkerState state) => Launcher.Instance?.Markers.Set(number, state);

        public static void ClearMissionMarkers() => Launcher.Instance?.Markers.Clear();

        public static Func<ReplayMission, string> ReplayGate { get; set; }

        public static bool UseApiMarkers { get; set; }

        public static string LockReason(ReplayMission mission)
        {
            Func<ReplayMission, string> gate = ReplayGate;
            if (gate == null || mission == null) return null;
            try
            {
                return gate(mission);
            }
            catch (Exception ex)
            {
                Log.Error("ReplayGate failed.", ex);
                return null;
            }
        }

        public static bool IsReplayActive => Launcher.Instance?.Observer?.SessionActive ?? false;

        public static ReplayMission CurrentReplay => Launcher.Instance?.Observer?.CurrentMission;

        public static IReadOnlyList<MissionObjective> CurrentObjectives =>
            Launcher.Instance?.Observer?.LiveObjectives() ?? (IReadOnlyList<MissionObjective>)Array.Empty<MissionObjective>();

        public static event Action<ReplayMission> ReplayStarted;
        public static event Action<ReplayAttemptEventArgs> ReplayAttemptStarted;
        public static event Action<ReplayResultEventArgs> ReplayAttemptEnded;
        public static event Action<ReplayFinishedEventArgs> ReplayFinished;

        internal static void RaiseReplayStarted(ReplayMission mission) => SafeInvoke(ReplayStarted, h => h(mission));
        internal static void RaiseAttemptStarted(ReplayAttemptEventArgs args) => SafeInvoke(ReplayAttemptStarted, h => h(args));
        internal static void RaiseAttemptEnded(ReplayResultEventArgs args) => SafeInvoke(ReplayAttemptEnded, h => h(args));
        internal static void RaiseReplayFinished(ReplayFinishedEventArgs args) => SafeInvoke(ReplayFinished, h => h(args));

        private static void SafeInvoke<T>(T multicast, Action<T> invoke) where T : Delegate
        {
            if (multicast == null) return;
            foreach (Delegate d in multicast.GetInvocationList())
            {
                try
                {
                    invoke((T)d);
                }
                catch (Exception ex)
                {
                    Log.Error($"Exception in an event handler ({d.Method.DeclaringType?.FullName}.{d.Method.Name}).", ex);
                }
            }
        }
    }
}
