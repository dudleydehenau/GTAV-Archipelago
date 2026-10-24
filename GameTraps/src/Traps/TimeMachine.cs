using System.Collections.Generic;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class TimeMachine : Trap
    {
        private const float MphToMs = 0.44704f;
        private const int AccelerateControl = 71;
        private const int JokeDelay = 5000;
        private const int JokeLength = 15000;
        private const int FlightControl = 357;

        private static readonly string[] YanktonIpls =
        {
            "prologue01", "prologue01c", "prologue01d", "prologue01e", "prologue01f", "prologue01g", "prologue01h",
            "prologue01i", "prologue01j", "prologue01k", "prologue01z", "prologue02", "prologue03", "prologue03b",
            "prologue03_grv_dug", "prologue_grv_torch", "prologue04", "prologue04b", "prologue04_cover",
            "des_protree_end", "des_protree_start", "prologue05", "prologue05b", "prologue06", "prologue06b",
            "prologue06_int", "prologue06_pannel", "plg_occl_00", "prologue_occl", "prologuerd", "prologuerdb",
        };

        private enum Stage
        {
            Driving,
            Igniting,
            Past,
            Joke,
        }

        private readonly EntityTracker _entities = new EntityTracker();
        private readonly EntityTracker _oldTimers = new EntityTracker();
        private readonly Ambience _ambience = new Ambience();
        private readonly List<Vector3> _trail = new List<Vector3>();
        private readonly List<int> _fires = new List<int>();
        private Vehicle _car;
        private Stage _stage;
        private int _until;
        private int _nextTrail;
        private int _stageAt;
        private bool _jokeTold;
        private bool _yankton;
        private bool _fromPast;

        public TimeMachine(TrapsConfig config) : base(config)
        {
        }

        private TimeMachineConfig Settings => Config.TimeMachine;
        public override string Id => TrapIds.TimeMachine;
        public override string Title => "Back to the future";
        public override string Subtitle => "Great Scott!";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Player | TrapChannel.Vision | TrapChannel.Weather;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft || PlayerInWater ? "waiting for you to be on land" : null;
            return reason == null;
        }

        public override void Start()
        {
            _stage = Stage.Driving;
            _jokeTold = false;
            _yankton = false;
            _trail.Clear();
            _fires.Clear();
            Model model = Spawner.LoadAny(Settings.Models);
            if (model.Hash == 0) return;
            Ped player = Player;
            Vector3 origin = player.IsInVehicle() ? player.CurrentVehicle.Position : player.Position;
            float heading = player.IsInVehicle() ? player.CurrentVehicle.Heading : player.Heading;
            float speed = player.IsInVehicle() ? player.CurrentVehicle.Speed : 0f;
            Spawner.WarpOutOfVehicle(player);
            Vector3 street = World.GetNextPositionOnStreet(origin);
            if (street.DistanceTo(origin) > 40f) street = origin;
            Function.Call(Hash.FORCE_LIGHTNING_FLASH);
            _car = Vehicle.Create(model, street + new Vector3(0f, 0f, 0.5f), heading);
            if (_car == null) return;
            _entities.Add(_car);
            Function.Call(Hash.SET_VEHICLE_COLOURS, _car, 4, 4);
            Function.Call(Hash.SET_PED_INTO_VEHICLE, player, _car, -1);
            Function.Call(Hash.SET_VEHICLE_ENGINE_ON, _car, true, true, false);
            Function.Call(Hash.SET_SPECIAL_FLIGHT_MODE_ALLOWED, _car, false);
            _car.ForwardSpeed = speed;
            Spawner.Poof(_car.Position);
            _until = StartTimer(Settings.Duration);
            _nextTrail = 0;
            Overrides.Subtitle($"~b~Doc:~s~ If my calculations are correct, when this baby hits {Settings.TargetMph:0} mph...", 6000);
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            Ped player = Player;
            if (player.IsDead) return false;
            switch (_stage)
            {
                case Stage.Joke:
                    if (!_jokeTold && now >= _stageAt + JokeDelay)
                    {
                        _jokeTold = true;
                        _ambience.Restore();
                        Overrides.Subtitle("~b~Doc:~s~ ...No, wait. It's just Sandy Shores. It always looks like this here.", 10000);
                    }
                    return now < _stageAt + JokeLength;
                case Stage.Igniting:
                    if (_car == null || !_car.Exists()) return false;
                    if (now < _stageAt) return true;
                    if (_fromPast) ToSandyShores();
                    else ToYankton();
                    return true;
                case Stage.Past:
                    if (now >= _stageAt)
                    {
                        ToSandyShores();
                        return true;
                    }
                    return Drive(now, player, true);
            }
            if (now >= _until) return false;
            return Drive(now, player, false);
        }

        private bool Drive(int now, Ped player, bool mustFly)
        {
            if (_car == null || !_car.Exists() || _car.IsDead) return false;
            if (!mustFly) Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, FlightControl, true);
            if (!player.IsInVehicle(_car)) return true;
            float mph = _car.Speed / MphToMs;
            bool airborne = Function.Call<float>(Hash.GET_ENTITY_HEIGHT_ABOVE_GROUND, _car) > 2f;
            Boost(mph, airborne);
            System.Drawing.Color color = mph >= Settings.TargetMph * 0.9f ? System.Drawing.Color.OrangeRed : System.Drawing.Color.Gold;
            Overrides.Banner($"{mph:0} / {Settings.TargetMph:0} mph", color, 600f, 0.9f);
            if (mustFly && !airborne)
                Overrides.Banner("Press X to fly, then hit 88 mph in the air!", System.Drawing.Color.DeepSkyBlue, 640f, 0.6f);
            if (now >= _nextTrail)
            {
                _nextTrail = now + 100;
                _trail.Add(_car.Position);
                if (_trail.Count > 16) _trail.RemoveAt(0);
            }
            if (mph >= Settings.TargetMph * 0.85f)
            {
                Vector3 p = _car.Position;
                Function.Call(Hash.DRAW_LIGHT_WITH_RANGE, p.X, p.Y, p.Z, 120, 180, 255, 8f, 6f);
            }
            if (mph >= Settings.TargetMph && (!mustFly || airborne)) Ignite(mustFly, airborne);
            return true;
        }

        public override void Stop(bool interrupted)
        {
            ClearFires();
            _trail.Clear();
            _oldTimers.DeleteAll();
            if (_yankton) Yankton(false);
            if (!Function.Call<bool>(Hash.IS_SCREEN_FADED_IN)) GTA.UI.Screen.FadeIn(500);
            ScreenEffects.Stop("MinigameTransitionIn");
            ScreenEffects.Stop("MinigameTransitionOut");
            _ambience.Restore();
            if (_car != null && _car.Exists()) Function.Call(Hash.SET_VEHICLE_CHEAT_POWER_INCREASE, _car, 1f);
            _car = null;
            _entities.Forget();
        }

        private void Boost(float mph, bool airborne)
        {
            Function.Call(Hash.SET_VEHICLE_CHEAT_POWER_INCREASE, _car, Settings.PowerMultiplier);
            if (!Function.Call<bool>(Hash.IS_CONTROL_PRESSED, 0, AccelerateControl)) return;
            if (!airborne && !_car.IsOnAllWheels) return;
            Vector3 velocity = _car.Velocity;
            Vector3 forward = new Vector3(_car.ForwardVector.X, _car.ForwardVector.Y, 0f).Normalized;
            if (mph >= Settings.TargetMph * 1.05f || Vector3.Dot(velocity, forward) < 0f) return;
            Vector3 push = forward * (Settings.Boost * Game.LastFrameTime);
            _car.Velocity = new Vector3(velocity.X + push.X, velocity.Y + push.Y, velocity.Z);
        }

        private void Ignite(bool fromPast, bool airborne)
        {
            _fromPast = fromPast;
            _stage = Stage.Igniting;
            _stageAt = Game.GameTime + 600;
            TrapTimer.Hide(Id);
            Vector3 right = _car.RightVector;
            if (Settings.FireTrails && !airborne)
            {
                foreach (Vector3 point in _trail)
                {
                    foreach (float side in new[] { -0.9f, 0.9f })
                    {
                        Vector3 spot = point + right * side;
                        spot.Z = Spawner.GroundZ(spot + new Vector3(0f, 0f, 2f), point.Z - 0.5f);
                        _fires.Add(Function.Call<int>(Hash.START_SCRIPT_FIRE, spot.X, spot.Y, spot.Z, 1, false));
                    }
                }
            }
            Function.Call(Hash.FORCE_LIGHTNING_FLASH);
            Spawner.Ptfx("scr_rcbarry1", "scr_alien_teleport", _car.Position, 2f);
        }

        private void ToYankton()
        {
            Flash(true);
            float speed = _car.Speed;
            Yankton(true);
            Destination past = Settings.Past;
            Spawner.TeleportVehicle(_car, new Vector3(past.X, past.Y, past.Z), past.Heading);
            _car.ForwardSpeed = speed * 0.5f;
            ClearFires();
            _ambience.Weather(Settings.PastWeather);
            _ambience.Timecycle(Settings.PastTimecycle, 1f);
            SpawnOldTimers();
            _trail.Clear();
            Function.Call(Hash.SET_SPECIAL_FLIGHT_MODE_ALLOWED, _car, true);
            Flash(false);
            _stage = Stage.Past;
            _stageAt = StartTimer(Settings.PastDuration);
            Overrides.Subtitle($"~b~Doc:~s~ Marty... we're in {past.Name}. In {Settings.Year}! To get back, 88 mph again... and this time, we fly!", 9000);
            Log.Info($"Time machine: jumped to {past.Name}.");
        }

        private void ToSandyShores()
        {
            TrapTimer.Hide(Id);
            Flash(true);
            Ped player = Player;
            Destination present = Settings.Present;
            if (_car != null && _car.Exists() && player.IsInVehicle(_car))
            {
                float speed = _car.Speed;
                Spawner.TeleportVehicle(_car, new Vector3(present.X, present.Y, present.Z), present.Heading);
                _car.ForwardSpeed = speed * 0.5f;
            }
            else
            {
                Spawner.Teleport(present);
            }
            _oldTimers.DeleteAll();
            Yankton(false);
            _ambience.Restore();
            _ambience.Timecycle(Settings.FarWestTimecycle, 0.8f);
            Flash(false);
            _stage = Stage.Joke;
            _stageAt = Game.GameTime;
            _jokeTold = false;
            Overrides.Subtitle($"~b~Doc:~s~ Great Scott, Marty! {Settings.FarWestYear}! The Wild West!", 5000);
        }

        private void SpawnOldTimers()
        {
            List<Model> cars = Spawner.LoadAll(Settings.OldCars);
            Model driver = Spawner.LoadModel(Settings.OldDriver);
            if (cars.Count == 0) return;
            Vector3 center = _car.Position;
            for (int i = 0; i < Settings.OldCarCount; i++)
            {
                Vector3 around = center + new Vector3(Spawner.Range(-80f, 80f), Spawner.Range(-80f, 80f), 0f);
                Vector3 street = World.GetNextPositionOnStreet(around);
                if (street.DistanceTo(around) > 60f || street.DistanceTo(center) < 8f) continue;
                Vehicle old = Vehicle.Create(Spawner.Pick(cars), street, Spawner.Range(0f, 360f));
                if (old == null) continue;
                _oldTimers.Add(old);
                Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, old, 5f);
                if (driver.Hash == 0 || i % 2 == 1) continue;
                Ped ped = old.CreatePedOnSeat(VehicleSeat.Driver, driver);
                if (ped == null) continue;
                _oldTimers.Add(ped);
                Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER, ped, old, 12f, 786603);
            }
        }

        private void Yankton(bool on)
        {
            _yankton = on;
            foreach (string ipl in YanktonIpls)
            {
                if (on && !Function.Call<bool>(Hash.IS_IPL_ACTIVE, ipl)) Function.Call(Hash.REQUEST_IPL, ipl);
                else if (!on && Function.Call<bool>(Hash.IS_IPL_ACTIVE, ipl)) Function.Call(Hash.REMOVE_IPL, ipl);
            }
            Function.Call(Hash.SET_MINIMAP_IN_PROLOGUE, on);
        }

        private static void Flash(bool start)
        {
            Function.Call(Hash.FORCE_LIGHTNING_FLASH);
            if (start)
            {
                Overrides.Beep("Hit_Out", "PLAYER_SWITCH_CUSTOM_SOUNDSET");
                GTA.UI.Screen.FadeOut(300);
                int deadline = System.Environment.TickCount + 1000;
                while (!GTA.UI.Screen.IsFadedOut && System.Environment.TickCount < deadline) Script.Wait(0);
            }
            else
            {
                GTA.UI.Screen.FadeIn(600);
            }
        }

        private void ClearFires()
        {
            foreach (int fire in _fires) Function.Call(Hash.REMOVE_SCRIPT_FIRE, fire);
            _fires.Clear();
        }
    }
}
