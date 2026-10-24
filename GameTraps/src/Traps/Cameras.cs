using System.Linq;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class TopDown : Trap
    {
        private readonly ScriptCam _cam = new ScriptCam();
        private int _until;

        public TopDown(TrapsConfig config) : base(config)
        {
        }

        private CameraTrapConfig Settings => Config.TopDown;
        public override string Id => TrapIds.TopDown;
        public override string Title => "Grand Theft Auto 1997";
        public override string Subtitle => "Back to the top-down days.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Camera;

        public override void Start()
        {
            _cam.Begin();
            Follow();
            _until = StartTimer(Settings.Duration);
        }

        public override bool Update()
        {
            if (Game.GameTime >= _until || Player.IsDead) return false;
            Follow();
            return true;
        }

        public override void Stop(bool interrupted) => _cam.End();

        private const float Fov = 50f;
        private const int ShotInterval = 140;

        private void Follow()
        {
            Ped player = Player;
            if (!player.IsInVehicle())
            {
                float north = -player.Heading;
                while (north > 180f) north -= 360f;
                while (north < -180f) north += 360f;
                Function.Call(Hash.SET_GAMEPLAY_CAM_RELATIVE_HEADING, north);
                Function.Call(Hash.SET_GAMEPLAY_CAM_RELATIVE_PITCH, 0f, 1f);
            }
            Entity mover = player.IsInVehicle() ? (Entity)player.CurrentVehicle : player;
            float height = Settings.Height + mover.Velocity.Length() * 0.6f;
            Vector3 position = mover.Position + new Vector3(0f, 0f, height);
            _cam.Set(position, new Vector3(-90f, 0f, 0f), Fov);
            Aim(player, position, height);
        }

        private int _nextShot;

        private void Aim(Ped player, Vector3 cam, float height)
        {
            Vector3 target;
            if (Function.Call<bool>(Hash.IS_USING_KEYBOARD_AND_MOUSE, 2))
            {
                Function.Call(Hash.SET_MOUSE_CURSOR_THIS_FRAME);
                Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 1, true);
                Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 2, true);
                float x = Function.Call<float>(Hash.GET_DISABLED_CONTROL_NORMAL, 0, 239);
                float y = Function.Call<float>(Hash.GET_DISABLED_CONTROL_NORMAL, 0, 240);
                float aspect = Function.Call<float>(Hash.GET_ASPECT_RATIO, false);
                float halfHeight = height * (float)System.Math.Tan(Fov * 0.5 * System.Math.PI / 180.0);
                float halfWidth = halfHeight * aspect;
                target = new Vector3(cam.X + (x - 0.5f) * 2f * halfWidth, cam.Y + (0.5f - y) * 2f * halfHeight, player.Position.Z);
                Ped snap = World.GetNearbyPeds(target, 3f).Where(p => p.Exists() && p != player && !p.IsDead).OrderBy(p => p.Position.DistanceTo2D(target)).FirstOrDefault();
                target = snap != null ? snap.Position + new Vector3(0f, 0f, 0.4f) : target + new Vector3(0f, 0f, 0.8f);
            }
            else
            {
                Ped closest = World.GetNearbyPeds(player, 40f)
                    .Where(p => p.Exists() && !p.IsDead && Function.Call<bool>(Hash.IS_PED_IN_COMBAT, p, player))
                    .OrderBy(p => p.Position.DistanceTo(player.Position)).FirstOrDefault();
                if (closest == null) return;
                target = closest.Position + new Vector3(0f, 0f, 0.4f);
            }
            Function.Call(Hash.DRAW_MARKER, 28, target.X, target.Y, target.Z, 0f, 0f, 0f, 0f, 0f, 0f,
                0.35f, 0.35f, 0.35f, 255, 40, 40, 200, false, false, 2, false, 0, 0, false);
            Vector3 from = player.Position + new Vector3(0f, 0f, 0.6f);
            float heading = Spawner.HeadingTowards(from, target);
            if (player.IsInVehicle()) return;
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 24, true);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 25, true);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 257, true);
            bool aiming = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 0, 25);
            bool firing = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 0, 24) || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 0, 257);
            if (!aiming && !firing) return;
            player.Heading = heading;
            if (!firing || Game.GameTime < _nextShot) return;
            _nextShot = Game.GameTime + ShotInterval;
            Function.Call(Hash.SET_PED_SHOOTS_AT_COORD, player, target.X, target.Y, target.Z, true);
        }
    }

    internal sealed class UpsideDown : Trap
    {
        private readonly ScriptCam _cam = new ScriptCam();
        private int _until;

        public UpsideDown(TrapsConfig config) : base(config)
        {
        }

        private CameraTrapConfig Settings => Config.UpsideDown;
        public override string Id => TrapIds.UpsideDown;
        public override string Title => "Upside down";
        public override string Subtitle => "Australia mode.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Camera;

        public override void Start()
        {
            _cam.Begin();
            Follow();
            _until = StartTimer(Settings.Duration);
        }

        public override bool Update()
        {
            if (Game.GameTime >= _until || Player.IsDead) return false;
            Follow();
            return true;
        }

        public override void Stop(bool interrupted) => _cam.End();

        private void Follow()
        {
            Vector3 position = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_COORD);
            Vector3 rotation = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 2);
            float fov = Function.Call<float>(Hash.GET_GAMEPLAY_CAM_FOV);
            _cam.Set(position, new Vector3(rotation.X, 180f, rotation.Z), fov);
        }
    }
}
