using GameTraps.Core;
using GTA;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class MoonGravity : Trap
    {
        private int _until;

        public MoonGravity(TrapsConfig config) : base(config)
        {
        }

        private MoonGravityConfig Settings => Config.MoonGravity;
        public override string Id => TrapIds.MoonGravity;
        public override string Title => "Moon gravity";
        public override string Subtitle => "One small step...";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Gravity;

        public override void Start()
        {
            Function.Call(Hash.SET_GRAVITY_LEVEL, Settings.Level);
            _until = StartTimer(Settings.Duration);
        }

        public override bool Update() => Game.GameTime < _until;

        public override void Stop(bool interrupted) => Function.Call(Hash.SET_GRAVITY_LEVEL, 0);
    }
}
