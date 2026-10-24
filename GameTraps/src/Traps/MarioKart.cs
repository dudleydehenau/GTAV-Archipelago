using System.Collections.Generic;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class MarioKart : Trap
    {
        private const int SphereMarker = 28;
        private readonly EntityTracker _entities = new EntityTracker();
        private readonly List<Prop> _peels = new List<Prop>();
        private List<Model> _models = new List<Model>();
        private int _until;
        private int _nextPeel;
        private int _spinUntil;
        private int _shellAt;
        private bool _shellFlying;
        private bool _shellDone;
        private Vector3 _shell;
        private Vector3 _shellDirection;
        private int _shellUntil;

        public MarioKart(TrapsConfig config) : base(config)
        {
        }

        private MarioKartConfig Settings => Config.MarioKart;
        public override string Id => TrapIds.MarioKart;
        public override string Title => "Rainbow Road";
        public override string Subtitle => "Watch out for banana peels.";
        public override bool Enabled => Settings.Enabled;

        public override bool CanStart(out string reason)
        {
            reason = null;
            Ped player = Player;
            if (!player.IsInVehicle() || PlayerInAircraft || player.CurrentVehicle.Model.IsBoat || player.CurrentVehicle.Speed < 5f)
                reason = "waiting for you to drive";
            return reason == null;
        }

        public override void Start()
        {
            _peels.Clear();
            _models = Spawner.LoadAll(Settings.PeelModels);
            _until = StartTimer(Settings.Duration);
            _nextPeel = Game.GameTime + 1000;
            _spinUntil = 0;
            _shellAt = Settings.BlueShell ? Game.GameTime + Settings.ShellDelay : int.MaxValue;
            _shellFlying = false;
            _shellDone = false;
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            Ped player = Player;
            if (now >= _until || player.IsDead) return false;
            Vehicle car = player.IsInVehicle() ? player.CurrentVehicle : null;

            if (car != null && now >= _nextPeel && _peels.Count < Settings.Peels && _models.Count > 0)
            {
                _nextPeel = now + Settings.PeelInterval;
                float ahead = System.Math.Max(15f, car.Speed * 1.6f);
                Vector3 spot = car.Position + car.ForwardVector * ahead + car.RightVector * Spawner.Range(-1.5f, 1.5f);
                spot.Z = Spawner.GroundZ(spot + new Vector3(0f, 0f, 4f), car.Position.Z - 0.5f) + 0.05f;
                Prop peel = Prop.Create(Spawner.Pick(_models), spot, false, false);
                if (peel != null)
                {
                    peel.IsPositionFrozen = true;
                    peel.IsCollisionEnabled = false;
                    _entities.Add(peel);
                    _peels.Add(peel);
                }
            }

            foreach (Prop peel in _peels)
            {
                if (!peel.Exists()) continue;
                Vector3 p = peel.Position;
                Function.Call(Hash.DRAW_LIGHT_WITH_RANGE, p.X, p.Y, p.Z + 0.5f, 255, 230, 0, 2.5f, 4f);
                if (car == null || now < _spinUntil || car.Position.DistanceTo(p) > 2.5f) continue;
                Spin(car);
                peel.Delete();
            }
            _peels.RemoveAll(p => !p.Exists());

            if (_spinUntil != 0 && now >= _spinUntil && car != null)
            {
                Function.Call(Hash.SET_VEHICLE_REDUCE_GRIP, car, false);
                _spinUntil = 0;
            }

            if (!_shellDone && now >= _shellAt) Shell(car ?? (Entity)player);
            return true;
        }

        public override void Stop(bool interrupted)
        {
            Ped player = Player;
            if (player.IsInVehicle()) Function.Call(Hash.SET_VEHICLE_REDUCE_GRIP, player.CurrentVehicle, false);
            _peels.Clear();
            _entities.DeleteAll();
        }

        private void Spin(Vehicle car)
        {
            _spinUntil = Game.GameTime + Settings.SpinTime;
            Function.Call(Hash.SET_VEHICLE_REDUCE_GRIP, car, true);
            float side = Spawner.Random.Next(2) == 0 ? -1f : 1f;
            Function.Call(Hash.APPLY_FORCE_TO_ENTITY, car, 1, side * 6f, 0f, 0f, 0f, 2.5f, 0f, 0, true, true, true, false, true);
            Overrides.Beep("Bounce", "DLC_IE_JUMP_SOUNDS");
            GTA.UI.Screen.ShowSubtitle("~y~Banana!", 1500);
        }

        private void Shell(Entity target)
        {
            int now = Game.GameTime;
            if (!_shellFlying)
            {
                _shellFlying = true;
                _shellUntil = now + Settings.ShellLifetime;
                _shell = target.Position - target.ForwardVector * 70f + new Vector3(0f, 0f, 3f);
                _shellDirection = (target.Position - _shell).Normalized;
                Overrides.Subtitle("~b~Blue shell incoming! Shake it off!", 3000);
                Overrides.Beep("5_SEC_WARNING", "HUD_MINI_GAME_SOUNDSET");
            }
            if (now >= _shellUntil)
            {
                _shellDone = true;
                Function.Call(Hash.ADD_EXPLOSION, _shell.X, _shell.Y, _shell.Z, 2, 0f, true, false, 0.3f);
                Overrides.Subtitle("~b~Blue shell dodged!", 3000);
                return;
            }
            Vector3 goal = target.Position + new Vector3(0f, 0f, 1f);
            Vector3 delta = goal - _shell;
            if (delta.Length() <= 2.2f)
            {
                _shellDone = true;
                Function.Call(Hash.ADD_EXPLOSION, goal.X, goal.Y, goal.Z, 2, 0.1f, true, false, 1.5f);
                target.Velocity = target.Velocity * 0.3f + new Vector3(0f, 0f, 9f);
                if (target is Vehicle vehicle) Function.Call(Hash.APPLY_FORCE_TO_ENTITY, vehicle, 1, 0f, 0f, 6f, 1.5f, 0f, 0f, 0, true, true, true, false, true);
                return;
            }
            float dt = Game.LastFrameTime;
            float turn = System.Math.Min(1f, Settings.ShellTurnRate * dt);
            Vector3 blended = _shellDirection + (delta.Normalized - _shellDirection) * turn;
            if (blended.Length() > 0.001f) _shellDirection = blended.Normalized;
            _shell += _shellDirection * Settings.ShellSpeed * dt;
            float ground = Spawner.GroundZ(_shell + new Vector3(0f, 0f, 3f), _shell.Z - 1f);
            if (_shell.Z < ground + 0.8f) _shell.Z = ground + 0.8f;
            Function.Call(Hash.DRAW_MARKER, SphereMarker, _shell.X, _shell.Y, _shell.Z, 0f, 0f, 0f, 0f, 0f, 0f,
                1.2f, 1.2f, 1.2f, 30, 90, 255, 230, false, false, 2, true, 0, 0, false);
            Function.Call(Hash.DRAW_LIGHT_WITH_RANGE, _shell.X, _shell.Y, _shell.Z, 30, 90, 255, 10f, 15f);
            Overrides.Banner($"Blue shell: {delta.Length():0} m", System.Drawing.Color.DeepSkyBlue, 640f, 0.6f);
        }
    }
}
