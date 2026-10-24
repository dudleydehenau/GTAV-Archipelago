using System;
using System.Runtime.CompilerServices;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal static class ReplayGuard
    {
        private static bool _launcherMissing;

        public static bool IsBusy(StoryGlobals globals) =>
            globals.IsRepeatPlay || globals.IsRepeatControllerRunning || IsLauncherReplayActive();

        public static bool IsMissionRunning => Function.Call<bool>(Hash.GET_MISSION_FLAG);

        public static bool IsLauncherReplayActive()
        {
            if (_launcherMissing) return false;
            try
            {
                return QueryLauncher();
            }
            catch (Exception ex)
            {
                _launcherMissing = true;
                Log.Warn("MissionLauncher is not available, replays are detected from the game only (" + ex.GetType().Name + ").");
                return false;
            }
        }

        public static bool IsLauncherPresent
        {
            get
            {
                if (_launcherMissing) return false;
                try
                {
                    QueryLauncherReady();
                    return true;
                }
                catch
                {
                    _launcherMissing = true;
                    return false;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool QueryLauncher() => MissionLauncher.MissionApi.IsReplayActive;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool QueryLauncherReady() => MissionLauncher.MissionApi.IsReady;
    }
}
