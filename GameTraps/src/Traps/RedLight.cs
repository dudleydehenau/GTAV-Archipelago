using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class RedLight : Trap
    {
        private int _until;
        private int _phaseEnd;
        private int _redSince;
        private int _nextShot;
        private bool _red;

        public RedLight(TrapsConfig config) : base(config)
        {
        }

        private RedLightConfig Settings => Config.RedLight;
        public override string Id => TrapIds.RedLight;
        public override string Title => "Red light, green light";
        public override string Subtitle => "Do not move on red.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Vision;

        public override void Start()
        {
            _until = StartTimer(Settings.Duration);
            Green();
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            if (now >= _until || Player.IsDead) return false;
            if (now >= _phaseEnd)
            {
                if (_red) Green();
                else Red();
            }
            Text(_red ? "RED LIGHT" : "GREEN LIGHT", _red ? 230 : 40, _red ? 30 : 220);
            if (!_red || now - _redSince < Settings.Grace || now < _nextShot) return true;
            Ped player = Player;
            Entity mover = player.IsInVehicle() ? (Entity)player.CurrentVehicle : player;
            if (mover.Velocity.Length() <= Settings.MoveTolerance) return true;
            _nextShot = now + Settings.ShotInterval;
            Vector3 target = player.Position + new Vector3(0f, 0f, 0.3f);
            Vector3 from = target + new Vector3(Spawner.Range(-20f, 20f), Spawner.Range(-20f, 20f), 35f);
            Function.Call(Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS, from.X, from.Y, from.Z, target.X, target.Y, target.Z,
                Settings.Damage, true, Spawner.WeaponHash(Settings.Weapon), 0, true, false, 2000f);
            return true;
        }

        public override void Stop(bool interrupted)
        {
            Overrides.ClearTimecycle();
        }

        private void Green()
        {
            _red = false;
            _phaseEnd = Game.GameTime + Spawner.Random.Next(Settings.MinGreen, System.Math.Max(Settings.MinGreen + 1, Settings.MaxGreen));
            Overrides.Timecycle("glasses_green", 0.35f);
            Overrides.Beep("Beep_Green", "DLC_HEIST_HACKING_SNAKE_SOUNDS");
        }

        private void Red()
        {
            _red = true;
            _redSince = Game.GameTime;
            _nextShot = 0;
            _phaseEnd = Game.GameTime + Spawner.Random.Next(Settings.MinRed, System.Math.Max(Settings.MinRed + 1, Settings.MaxRed));
            Overrides.Timecycle("glasses_red", 0.6f);
            Overrides.Beep("Beep_Red", "DLC_HEIST_HACKING_SNAKE_SOUNDS");
        }

        private static void Text(string text, int r, int g) =>
            Overrides.Banner(text, System.Drawing.Color.FromArgb(255, r, g, 40), 90f, 1.4f);
    }
}
