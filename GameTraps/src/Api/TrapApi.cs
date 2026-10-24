using System;
using System.Collections.Generic;
using System.Linq;
using GameTraps.Core;

namespace GameTraps
{
    public static class TrapIds
    {
        public const string ScooterBrother = "ScooterBrother";
        public const string Ramp = "Ramp";
        public const string AnimalHunt = "AnimalHunt";
        public const string AnimalTransform = "AnimalTransform";
        public const string FallingPlanes = "FallingPlanes";
        public const string TurboTraffic = "TurboTraffic";
        public const string AlienAbduction = "AlienAbduction";
        public const string Irresistible = "Irresistible";
        public const string SecondComing = "SecondComing";
        public const string Wanted = "Wanted";
        public const string Drunk = "Drunk";
        public const string FlatTires = "FlatTires";
        public const string Disarmed = "Disarmed";
        public const string LesterTax = "LesterTax";
        public const string Paparazzi = "Paparazzi";
        public const string Clowns = "Clowns";
        public const string MoonGravity = "MoonGravity";
        public const string Blizzard = "Blizzard";
        public const string Nightfall = "Nightfall";
        public const string Ragdoll = "Ragdoll";
        public const string LiftOff = "LiftOff";
        public const string PhantomCar = "PhantomCar";
        public const string PhantomWedge = "PhantomWedge";
        public const string RedLight = "RedLight";
        public const string Speed = "Speed";
        public const string TimeMachine = "TimeMachine";
        public const string Purge = "Purge";
        public const string Terminator = "Terminator";
        public const string Matrix = "Matrix";
        public const string MadMax = "MadMax";
        public const string Jaws = "Jaws";
        public const string MacheteGranny = "MacheteGranny";
        public const string HereWeGoAgain = "HereWeGoAgain";
        public const string Bowling = "Bowling";
        public const string ViceCity = "ViceCity";
        public const string BigSmoke = "BigSmoke";
        public const string BattleRoyale = "BattleRoyale";
        public const string MarioKart = "MarioKart";
        public const string TopDown = "TopDown";
        public const string UpsideDown = "UpsideDown";
        public const string Clones = "Clones";
        public const string Kidnap = "Kidnap";
        public const string ObjectRain = "ObjectRain";
        public const string GiantBalls = "GiantBalls";
        public const string FailedHeroes = "FailedHeroes";
        public const string Mariachi = "Mariachi";
        public const string FloatingCars = "FloatingCars";
        public const string Kamikaze = "Kamikaze";
        public const string FlashMob = "FlashMob";
        public const string InitialD = "InitialD";
        public const string ForrestGump = "ForrestGump";
        public const string WalkingDead = "WalkingDead";
    }

    public static class TrapApi
    {
        public const int ApiVersion = 4;

        private static TrapRunner Core => TrapRunner.Instance;

        public static bool IsReady => Core != null && Core.IsReady;

        public static IReadOnlyList<string> Traps =>
            Core == null ? (IReadOnlyList<string>)Array.Empty<string>() : Core.Traps.Select(t => t.Id).ToList();

        public static IReadOnlyList<string> EnabledTraps =>
            Core == null ? (IReadOnlyList<string>)Array.Empty<string>() : Core.Traps.Where(t => t.Enabled).Select(t => t.Id).ToList();

        public static string GetTitle(string id) => Core?.Find(id)?.Title;

        public static bool IsEnabled(string id) => Core?.Find(id)?.Enabled ?? false;

        public static string ActiveTrap => Core?.ActiveId;

        public static int PendingCount => Core?.Pending ?? 0;

        public static bool Trigger(string id, out string error) => Trigger(id, null, out error);

        public static bool Trigger(string id, string source, out string error)
        {
            if (Core == null)
            {
                error = "GameTraps is not loaded.";
                return false;
            }
            return Core.Enqueue(id, source, out error);
        }

        public static IReadOnlyList<string> QueuedTraps => Core == null ? (IReadOnlyList<string>)Array.Empty<string>() : Core.QueuedIds;

        public static bool AllowInMissions
        {
            get => Core != null && Core.Config.AllowInMissions;
            set
            {
                if (Core != null) Core.Config.AllowInMissions = value;
            }
        }

        public static IReadOnlyList<string> MissionTraps =>
            Core?.Config.MissionTraps ?? (IReadOnlyList<string>)Array.Empty<string>();

        public static bool RevealNames
        {
            get => Core == null || Core.Config.RevealNames;
            set
            {
                if (Core != null) Core.Config.RevealNames = value;
            }
        }

        public static string TriggerRandom()
        {
            IReadOnlyList<string> enabled = EnabledTraps;
            if (enabled.Count == 0) return null;
            string id = Spawner.Pick(enabled.ToList());
            return Trigger(id, out _) ? id : null;
        }

        public static IReadOnlyList<string> ActiveTraps => Core == null ? (IReadOnlyList<string>)Array.Empty<string>() : Core.ActiveIds;

        public static bool StreamSafe
        {
            get => Core != null && Core.Config.StreamSafe;
            set => Core?.SetStreamSafe(value);
        }

        public static void ClearQueue() => Core?.ClearQueue();

        public static event Action<string> TrapStarted;

        public static event Action<string, bool> TrapFinished;

        internal static void RaiseStarted(string id) => Raise(() => TrapStarted?.Invoke(id));

        internal static void RaiseFinished(string id, bool interrupted) => Raise(() => TrapFinished?.Invoke(id, interrupted));

        private static void Raise(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log.Error("An API event handler failed.", ex);
            }
        }
    }
}
