using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace GameTraps.Core
{
    [Flags]
    internal enum TrapChannel
    {
        None = 0,
        Player = 1,
        Vision = 2,
        Gravity = 4,
        Weather = 8,
        Police = 16,
        Phone = 32,
        Camera = 64,
        Time = 128,
    }

    internal enum TrapWeight
    {
        Light,
        Medium,
        Heavy,
    }

    internal static class TrapWeights
    {
        private static readonly HashSet<string> Light = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Ramp", "FlatTires", "Disarmed", "LesterTax", "Wanted", "Nightfall", "Ragdoll",
        };

        private static readonly HashSet<string> Heavy = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AlienAbduction", "AnimalTransform", "SecondComing", "TimeMachine", "Kidnap", "Bowling", "InitialD", "Speed",
            "BattleRoyale", "Purge", "ViceCity", "WalkingDead", "PhantomCar", "Matrix", "TopDown", "UpsideDown", "Clones",
            "PhantomWedge", "MadMax", "HereWeGoAgain", "Irresistible",
        };

        public static TrapWeight Of(string id) =>
            Heavy.Contains(id) ? TrapWeight.Heavy : Light.Contains(id) ? TrapWeight.Light : TrapWeight.Medium;
    }

    internal static class Preconditions
    {
        private static readonly HashSet<string> OnFoot = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AnimalHunt", "AnimalTransform", "AlienAbduction", "SecondComing", "Kidnap", "Bowling", "TimeMachine", "Clowns",
            "Irresistible", "FlashMob", "ForrestGump", "WalkingDead", "Matrix", "Drunk", "BigSmoke", "MacheteGranny", "FailedHeroes",
        };

        private static readonly HashSet<string> Driving = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Ramp", "MarioKart", "Kamikaze", "PhantomWedge",
        };

        public static bool Met(string id, out string reason)
        {
            reason = null;
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead)
            {
                reason = "waiting for you to be alive";
                return false;
            }
            if (Function.Call<bool>(Hash.IS_PED_FALLING, player) || Function.Call<bool>(Hash.IS_PED_IN_PARACHUTE_FREE_FALL, player)
                || Function.Call<int>(Hash.GET_PED_PARACHUTE_STATE, player) >= 0 || Function.Call<bool>(Hash.IS_PED_RAGDOLL, player)
                || Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE, player))
            {
                reason = "waiting for you to settle";
                return false;
            }
            if (OnFoot.Contains(id) && (player.IsInVehicle() || Function.Call<bool>(Hash.IS_PED_SWIMMING, player)))
            {
                reason = "waiting for you to be on foot";
                return false;
            }
            if (Driving.Contains(id))
            {
                Vehicle vehicle = player.IsInVehicle() ? player.CurrentVehicle : null;
                if (vehicle == null || vehicle.Driver != player)
                {
                    reason = "waiting for you to drive";
                    return false;
                }
                Model model = vehicle.Model;
                if (model.IsPlane || model.IsHelicopter || model.IsBoat || model.IsSubmarine)
                {
                    reason = "waiting for you to drive on land";
                    return false;
                }
            }
            return true;
        }
    }

    internal abstract class Trap
    {
        protected Trap(TrapsConfig config)
        {
            Config = config;
        }

        protected TrapsConfig Config { get; }

        public abstract string Id { get; }
        public abstract string Title { get; }
        public abstract string Subtitle { get; }
        public abstract bool Enabled { get; }

        public virtual TrapChannel Channels => TrapChannel.None;

        public virtual bool CanStart(out string reason)
        {
            reason = null;
            return true;
        }

        public abstract void Start();

        public abstract bool Update();

        public abstract void Stop(bool interrupted);

        public virtual void Abort() => Stop(true);

        public virtual void Background()
        {
        }

        protected static Ped Player => Game.Player.Character;

        protected int StartTimer(int durationMs)
        {
            TrapTimer.Start(Id, Title, durationMs);
            return Game.GameTime + durationMs;
        }

        protected static bool PlayerInAircraft
        {
            get
            {
                Ped player = Player;
                if (!player.IsInVehicle()) return false;
                Model model = player.CurrentVehicle.Model;
                return model.IsPlane || model.IsHelicopter;
            }
        }

        protected static bool PlayerInWater => Function.Call<bool>(Hash.IS_ENTITY_IN_WATER, Player);
    }

    internal static class NoPolice
    {
        private static bool _active;

        public static void Begin()
        {
            _active = true;
            Keep();
        }

        public static void Keep()
        {
            if (!_active) return;
            Function.Call(Hash.SET_MAX_WANTED_LEVEL, 0);
            if (Game.Player.Wanted.WantedLevel > 0) Function.Call(Hash.CLEAR_PLAYER_WANTED_LEVEL, Game.Player);
        }

        public static void End()
        {
            if (!_active) return;
            _active = false;
            Function.Call(Hash.SET_MAX_WANTED_LEVEL, 5);
        }
    }

    internal static class Safety
    {
        private static readonly int RepeatController = Joaat("mission_repeat_controller");

        public static int Joaat(string text)
        {
            uint hash = 0;
            foreach (char c in text.ToLowerInvariant())
            {
                hash += c;
                hash += hash << 10;
                hash ^= hash >> 6;
            }
            hash += hash << 3;
            hash ^= hash >> 11;
            hash += hash << 15;
            return unchecked((int)hash);
        }

        public static bool IsReplayRunning =>
            Function.Call<int>(Hash.GET_NUMBER_OF_THREADS_RUNNING_THE_SCRIPT_WITH_THIS_HASH, RepeatController) > 0;

        public static bool MissionEnding =>
            Function.Call<bool>(Hash.IS_MISSION_COMPLETE_PLAYING) || (IsReplayRunning && !Function.Call<bool>(Hash.GET_MISSION_FLAG));

        public static bool MustInterrupt(out string reason)
        {
            if (MissionEnding) { reason = "mission ending"; return true; }
            if (Function.Call<bool>(Hash.GET_MISSION_FLAG)) { reason = "mission"; return true; }
            if (IsReplayRunning) { reason = "replay"; return true; }
            if (Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS)) { reason = "character switch"; return true; }
            reason = null;
            return false;
        }

        public static bool InMission => Function.Call<bool>(Hash.GET_MISSION_FLAG) || IsReplayRunning || MissionEnding;

        public static bool CanStart(bool missionAllowed, out string reason)
        {
            if (MustInterrupt(out reason) && !(missionAllowed && (reason == "mission" || reason == "replay"))) return false;
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead || !Function.Call<bool>(Hash.IS_PLAYER_PLAYING, Game.Player))
            {
                reason = "player not playing";
                return false;
            }
            if (Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE) || !Function.Call<bool>(Hash.IS_SCREEN_FADED_IN))
            {
                reason = "screen not ready";
                return false;
            }
            if (Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) || Function.Call<bool>(Hash.IS_PAUSE_MENU_ACTIVE))
            {
                reason = "cutscene or pause menu";
                return false;
            }
            if (!Function.Call<bool>(Hash.IS_PLAYER_CONTROL_ON, Game.Player))
            {
                reason = "player control off";
                return false;
            }
            reason = null;
            return true;
        }
    }
}
