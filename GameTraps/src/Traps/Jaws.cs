using GameTraps.Core;
using GTA;
using GTA.Math;

namespace GameTraps.Traps
{
    internal sealed class Jaws : Trap
    {
        private readonly SkyRain _rain = new SkyRain();
        private int _until;

        public Jaws(TrapsConfig config) : base(config)
        {
        }

        private JawsConfig Settings => Config.Jaws;
        public override string Id => TrapIds.Jaws;
        public override string Title => "Jaws";
        public override string Subtitle => "You're gonna need a bigger boat.";
        public override bool Enabled => Settings.Enabled;

        public override void Start()
        {
            _rain.MaxAlive = Settings.MaxFallen;
            _rain.Begin(null, Settings.SharkModels, int.MaxValue, Settings.RainInterval, 10f, 20f, 30f);
            _until = StartTimer(Settings.Duration);
            Overrides.Subtitle("~b~Duh-dum... duh-dum... look up.", 5000);
        }

        public override bool Update()
        {
            Ped player = Player;
            if (Game.GameTime >= _until || player.IsDead) return false;
            Entity mover = player.IsInVehicle() ? (Entity)player.CurrentVehicle : player;
            Vector3 velocity = mover.Velocity;
            velocity.Z = 0f;
            _rain.Process(mover.Position + velocity * Settings.Lead);
            return true;
        }

        public override void Stop(bool interrupted) => _rain.Clear(interrupted);
    }
}
