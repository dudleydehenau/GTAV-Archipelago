using GameTraps.Core;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class Nightfall : Trap
    {
        public Nightfall(TrapsConfig config) : base(config)
        {
        }

        public override string Id => TrapIds.Nightfall;
        public override string Title => "Nightfall";
        public override string Subtitle => "Who turned off the sun?";
        public override bool Enabled => Config.Nightfall.Enabled;

        public override void Start()
        {
            int hour = ((Config.Nightfall.Hour % 24) + 24) % 24;
            Function.Call(Hash.SET_CLOCK_TIME, hour, 0, 0);
        }

        public override bool Update() => false;

        public override void Stop(bool interrupted)
        {
        }
    }
}
