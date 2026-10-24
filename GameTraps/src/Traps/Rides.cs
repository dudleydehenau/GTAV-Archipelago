using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class Kidnap : Trap
    {
        private static readonly string[] English =
        {
            "~y~Daniel:~s~ Welcome to my taxi. Hold on tight, we're going to Marseille!",
            "~y~Daniel:~s~ Speed camera? It flashed? Relax, I know a cop. Kind of.",
            "~y~Daniel:~s~ Watch this. Fifth gear, and that's just the warm-up.",
            "~y~Daniel:~s~ Red lights are a suggestion. A pink one, at most.",
            "~y~Daniel:~s~ My fiancée says I drive too fast. She's never been in the car.",
        };

        private readonly ForcedRide _ride = new ForcedRide();
        private int _until;
        private int _nextLine;
        private int _line;

        public Kidnap(TrapsConfig config) : base(config)
        {
        }

        private RideConfig Settings => Config.Kidnap;
        public override string Id => TrapIds.Kidnap;
        public override string Title => "Taxi";
        public override string Subtitle => "Marseille, 13 minutes flat.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Player;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft || PlayerInWater ? "waiting for you to be on land" : null;
            return reason == null;
        }

        public override void Start()
        {
            if (!_ride.Begin(Settings)) return;
            Dress(_ride.Vehicle);
            _until = StartTimer(Settings.Duration);
            _line = 0;
            _nextLine = 0;
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            if (now >= _until || Player.IsDead || _ride.Vehicle == null) return false;
            _ride.Process();
            if (now >= _nextLine && _line < English.Length)
            {
                GTA.UI.Screen.ShowSubtitle(English[_line], 5000);
                _line++;
                _nextLine = now + 6000;
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            _ride.End(interrupted);
            if (!interrupted) Overrides.Subtitle("~y~Daniel:~s~ There you go. That'll be $250, tip not included.", 4000);
        }

        private static void Dress(Vehicle taxi)
        {
            if (taxi == null || !taxi.Exists()) return;
            Function.Call(Hash.SET_VEHICLE_MOD_KIT, taxi, 0);
            Function.Call(Hash.SET_VEHICLE_COLOURS, taxi, 111, 111);
            Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, taxi, "TAXI 13");
            foreach (int type in new[] { 0, 1, 2, 3, 4, 6, 7, 11, 12, 13, 15 })
            {
                int count = Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS, taxi, type);
                if (count > 0) Function.Call(Hash.SET_VEHICLE_MOD, taxi, type, count - 1, false);
            }
            Function.Call(Hash.TOGGLE_VEHICLE_MOD, taxi, 18, true);
            Function.Call(Hash.SET_VEHICLE_WINDOW_TINT, taxi, 1);
            Spawner.Poof(taxi.Position);
        }
    }

    internal sealed class Bowling : Trap
    {
        private static readonly Vector3 Alley = new Vector3(-1350f, -1400f, 4.5f);

        private static readonly string[] English =
        {
            "Cousin! It's Roman! Let's go bowling!",
            "Niko? NIKO! Bowling! Tonight! You and me!",
            "Cousin, why you not answer? I already rented the shoes!",
            "Cousin! The bowling lanes, they are waiting for us!",
            "I send taxi. Do not argue. BOWLING!",
            "Cousin, I bought the big balls. The BIG ones.",
        };

        private readonly ForcedRide _ride = new ForcedRide();
        private int _until;
        private int _nextMessage;
        private int _message;
        private bool _riding;

        public Bowling(TrapsConfig config) : base(config)
        {
        }

        private RideConfig Settings => Config.Bowling;
        public override string Id => TrapIds.Bowling;
        public override string Title => "Let's go bowling!";
        public override string Subtitle => "Your cousin is calling.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Player | TrapChannel.Phone;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft || PlayerInWater ? "waiting for you to be on land" : null;
            return reason == null;
        }

        public override void Start()
        {
            _message = 0;
            _riding = false;
            _nextMessage = 0;
            _until = StartTimer(Settings.Duration);
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            if (now >= _until || Player.IsDead) return false;
            if (_message < English.Length && now >= _nextMessage)
            {
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "Text_Arrive_Tone", "Phone_SoundSet_Default", true);
                GameText.Feed("CHAR_DEFAULT", "Roman", "Cousin!", English[_message]);
                _message++;
                _nextMessage = now + 1200;
            }
            if (!_riding && _message >= 3)
            {
                _riding = true;
                if (!_ride.Begin(Settings)) return false;
                _ride.DriveTo(Alley);
                Overrides.Subtitle("~y~Taxi:~s~ Mr. Bellic sent me. To the bowling alley!", 5000);
            }
            if (_riding)
            {
                if (_ride.Vehicle == null) return false;
                _ride.Process();
                if (_ride.Vehicle.Position.DistanceTo(Alley) < 25f) return false;
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            _ride.End(interrupted);
            if (_riding && !interrupted)
            {
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "Text_Arrive_Tone", "Phone_SoundSet_Default", true);
                GameText.Feed("CHAR_DEFAULT", "Roman", "Cousin!",
                    "Ha ha! There is no bowling in Los Santos, cousin! Next time, Liberty City!");
            }
            _riding = false;
        }
    }
}
