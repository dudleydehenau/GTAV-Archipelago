using System.Collections.Generic;
using System.Linq;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class ObjectRain : Trap
    {
        private readonly SkyRain _rain = new SkyRain();
        private int _until;

        public ObjectRain(TrapsConfig config) : base(config)
        {
        }

        private RainConfig Settings => Config.ObjectRain;
        public override string Id => TrapIds.ObjectRain;
        public override string Title => "Cloudy with a chance of toilets";
        public override string Subtitle => "Look up.";
        public override bool Enabled => Settings.Enabled;

        public override void Start()
        {
            _rain.Begin(Settings.PropModels, Settings.PedModels, Settings.Count, Settings.Interval, Settings.Spread, Settings.MinHeight, Settings.MaxHeight);
            _until = StartTimer(Settings.Duration);
        }

        public override bool Update()
        {
            if (Game.GameTime >= _until || Player.IsDead) return false;
            Ped player = Player;
            Entity mover = player.IsInVehicle() ? (Entity)player.CurrentVehicle : player;
            _rain.Process(mover.Position + mover.Velocity * 1.2f);
            return true;
        }

        public override void Stop(bool interrupted) => _rain.Clear(interrupted);
    }

    internal sealed class GiantBalls : Trap
    {
        private readonly EntityTracker _entities = new EntityTracker();
        private List<Model> _models = new List<Model>();
        private int _until;
        private int _next;
        private int _count;

        public GiantBalls(TrapsConfig config) : base(config)
        {
        }

        private GiantBallsConfig Settings => Config.GiantBalls;
        public override string Id => TrapIds.GiantBalls;
        public override string Title => "Indiana Jones";
        public override string Subtitle => "Run!";
        public override bool Enabled => Settings.Enabled;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft || PlayerInWater ? "waiting for you to be on land" : null;
            return reason == null;
        }

        public override void Start()
        {
            _models = Spawner.LoadAll(Settings.Models);
            _count = 0;
            _next = 0;
            _until = StartTimer(Settings.Duration);
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            if (now >= _until || Player.IsDead) return false;
            if (now < _next || _count >= Settings.MaxBalls || _models.Count == 0) return true;
            _next = now + Settings.Interval;
            _count++;
            Ped player = Player;
            Vector3 start = Uphill(player.Position, Settings.Distance) + new Vector3(0f, 0f, 6f);
            Prop ball = Prop.Create(Spawner.Pick(_models), start, true, false);
            if (ball == null) return true;
            _entities.Add(ball);
            ball.IsPositionFrozen = false;
            Function.Call(Hash.SET_ENTITY_DYNAMIC, ball, true);
            Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, ball, true);
            Function.Call(Hash.ACTIVATE_PHYSICS, ball);
            Vector3 direction = (player.Position - start).Normalized;
            ball.Velocity = direction * Settings.Speed;
            Overrides.Beep("Bounce", "DLC_IE_JUMP_SOUNDS");
            return true;
        }

        public override void Stop(bool interrupted)
        {
            if (interrupted) _entities.DeleteAll();
            else _entities.Forget();
        }

        private static Vector3 Uphill(Vector3 center, float distance)
        {
            Vector3 best = center;
            float bestZ = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                double angle = i * System.Math.PI / 4 + Spawner.Range(-0.2f, 0.2f);
                var probe = new Vector3(center.X + (float)System.Math.Cos(angle) * distance, center.Y + (float)System.Math.Sin(angle) * distance, center.Z + 60f);
                float z = Spawner.GroundZ(probe, center.Z);
                if (z <= bestZ) continue;
                bestZ = z;
                best = new Vector3(probe.X, probe.Y, z);
            }
            return best;
        }
    }

    internal sealed class Mariachi : Trap
    {
        private readonly EntityTracker _entities = new EntityTracker();
        private readonly List<Ped> _band = new List<Ped>();
        private int _until;
        private int _nextOrder;

        public Mariachi(TrapsConfig config) : base(config)
        {
        }

        private MariachiConfig Settings => Config.Mariachi;
        public override string Id => TrapIds.Mariachi;
        public override string Title => "Mariachi";
        public override string Subtitle => "A serenade, whether you like it or not.";
        public override bool Enabled => Settings.Enabled;

        public override void Start()
        {
            _band.Clear();
            List<Model> models = Spawner.LoadAll(Settings.Models);
            if (models.Count == 0) return;
            Ped player = Player;
            for (int i = 0; i < Settings.Count; i++)
            {
                Vector3 spot = Spawner.GroundAround(player.Position, Spawner.Range(3f, 5f));
                Ped musician = Ped.Create(Spawner.Pick(models), spot, Spawner.HeadingTowards(spot, player.Position));
                if (musician == null) continue;
                _entities.Add(musician);
                _band.Add(musician);
                Function.Call(Hash.SET_PED_RANDOM_COMPONENT_VARIATION, musician, 0);
                musician.BlockPermanentEvents = true;
                musician.IsInvincible = true;
                Spawner.Poof(spot + new Vector3(0f, 0f, 0.5f));
            }
            _until = StartTimer(Settings.Duration);
            _nextOrder = 0;
            Overrides.Subtitle("~y~Ay, ay, ay, ay! Sing and don't cry...", 5000);
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            Ped player = Player;
            if (now >= _until || player.IsDead) return false;
            if (now < _nextOrder) return true;
            _nextOrder = now + 700;
            Entity mover = player.IsInVehicle() ? (Entity)player.CurrentVehicle : player;
            foreach (Ped musician in _band)
            {
                if (!musician.Exists() || musician.IsDead) continue;
                float distance = musician.Position.DistanceTo(player.Position);
                if (distance > Settings.FollowDistance)
                {
                    Vector3 spot = Spawner.GroundAround(mover.Position + mover.Velocity * 1.5f, Spawner.Range(3f, 5f));
                    Spawner.Poof(musician.Position);
                    musician.Position = spot;
                    musician.Heading = Spawner.HeadingTowards(spot, player.Position);
                    Play(musician);
                }
                else if (!Function.Call<bool>(Hash.IS_PED_USING_SCENARIO, musician, Settings.Scenario))
                {
                    musician.Heading = Spawner.HeadingTowards(musician.Position, player.Position);
                    Play(musician);
                }
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            foreach (Ped musician in _band)
                if (musician.Exists()) Spawner.Poof(musician.Position);
            _band.Clear();
            _entities.DeleteAll();
        }

        private void Play(Ped musician)
        {
            Function.Call(Hash.CLEAR_PED_TASKS_IMMEDIATELY, musician);
            Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, musician, Settings.Scenario, 0, false);
        }
    }

    internal sealed class FloatingCars : Trap
    {
        private const int Interval = 200;
        private readonly Dictionary<int, Vehicle> _floating = new Dictionary<int, Vehicle>();
        private readonly Dictionary<int, float> _baseHeight = new Dictionary<int, float>();
        private int _until;
        private int _next;

        public FloatingCars(TrapsConfig config) : base(config)
        {
        }

        private FloatingCarsConfig Settings => Config.FloatingCars;
        public override string Id => TrapIds.FloatingCars;
        public override string Title => "Flying cars";
        public override string Subtitle => "The future is here, sort of.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Gravity;

        public override void Start()
        {
            _floating.Clear();
            _baseHeight.Clear();
            _until = StartTimer(Settings.Duration);
            _next = 0;
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            if (now >= _until || Player.IsDead) return false;
            if (now < _next) return true;
            _next = now + Interval;
            Ped player = Player;
            Vehicle own = player.IsInVehicle() ? player.CurrentVehicle : null;
            foreach (Vehicle vehicle in World.GetNearbyVehicles(player.Position, Settings.Radius))
            {
                if (!vehicle.Exists() || vehicle.IsDead) continue;
                if (vehicle == own && !Settings.IncludePlayer) continue;
                Model model = vehicle.Model;
                if (model.IsPlane || model.IsHelicopter || model.IsTrain || model.IsBoat) continue;
                if (!_floating.ContainsKey(vehicle.Handle))
                {
                    _floating[vehicle.Handle] = vehicle;
                    _baseHeight[vehicle.Handle] = vehicle.Position.Z;
                    Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, vehicle, false);
                    Function.Call(Hash.SET_VEHICLE_GRAVITY, vehicle, false);
                }
                float height = vehicle.Position.Z - _baseHeight[vehicle.Handle];
                Vector3 velocity = vehicle.Velocity;
                float up = height < Settings.HoverHeight ? Settings.UpSpeed : 0.15f * (float)System.Math.Sin(now / 400.0 + vehicle.Handle);
                vehicle.Velocity = new Vector3(velocity.X * 0.95f, velocity.Y * 0.95f, up);
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            foreach (Vehicle vehicle in _floating.Values)
            {
                if (!vehicle.Exists()) continue;
                Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, vehicle, true);
                Function.Call(Hash.SET_VEHICLE_GRAVITY, vehicle, true);
            }
            _floating.Clear();
            _baseHeight.Clear();
        }
    }

    internal sealed class Kamikaze : Trap
    {
        private readonly EntityTracker _entities = new EntityTracker();
        private readonly HashSet<int> _used = new HashSet<int>();
        private List<Model> _models = new List<Model>();
        private int _until;
        private int _next;

        public Kamikaze(TrapsConfig config) : base(config)
        {
        }

        private KamikazeConfig Settings => Config.Kamikaze;
        public override string Id => TrapIds.Kamikaze;
        public override string Title => "Kamikaze pedestrians";
        public override string Subtitle => "They really want to be hit.";
        public override bool Enabled => Settings.Enabled;

        public override bool CanStart(out string reason)
        {
            reason = null;
            Ped player = Player;
            if (!player.IsInVehicle() || PlayerInAircraft || player.CurrentVehicle.Model.IsBoat)
                reason = "waiting for you to drive";
            return reason == null;
        }

        public override void Start()
        {
            _used.Clear();
            _models = Spawner.LoadAll(Settings.Models);
            _until = StartTimer(Settings.Duration);
            _next = 0;
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            Ped player = Player;
            if (now >= _until || player.IsDead) return false;
            if (now < _next || !player.IsInVehicle()) return true;
            _next = now + Settings.Interval;
            Vehicle car = player.CurrentVehicle;
            float ahead = Spawner.Range(Settings.MinAhead, Settings.MaxAhead) + car.Speed * 0.8f;
            Vector3 lane = car.Position + car.ForwardVector * ahead;
            lane.Z = Spawner.GroundZ(lane + new Vector3(0f, 0f, 5f), car.Position.Z);
            float side = Spawner.Random.Next(2) == 0 ? -1f : 1f;
            Vector3 curb = lane + car.RightVector * (side * Spawner.Range(5f, 8f));
            curb.Z = Spawner.GroundZ(curb + new Vector3(0f, 0f, 5f), lane.Z) + 0.5f;

            Ped jumper = Spawner.AmbientPeds(30f, 1, p => !_used.Contains(p.Handle) && !p.IsInVehicle() && p.Position.DistanceTo(lane) < 30f).FirstOrDefault();
            if (jumper != null)
            {
                jumper.Position = curb;
            }
            else
            {
                if (_models.Count == 0) return true;
                jumper = Ped.Create(Spawner.Pick(_models), curb, 0f);
                if (jumper == null) return true;
                _entities.Add(jumper);
                Function.Call(Hash.SET_PED_RANDOM_COMPONENT_VARIATION, jumper, 0);
            }
            _used.Add(jumper.Handle);
            jumper.BlockPermanentEvents = true;
            jumper.Heading = Spawner.HeadingTowards(curb, lane);
            Vector3 across = lane - car.RightVector * (side * 3f);
            Function.Call(Hash.TASK_GO_STRAIGHT_TO_COORD, jumper, across.X, across.Y, across.Z, 3f, -1, 0f, 0f);
            Function.Call(Hash.PLAY_PAIN, jumper, 8, 0, 0);
            return true;
        }

        public override void Stop(bool interrupted)
        {
            _used.Clear();
            if (interrupted) _entities.DeleteAll();
            else _entities.Forget();
        }
    }

    internal sealed class FlashMob : Trap
    {
        private readonly List<Ped> _dancers = new List<Ped>();
        private Emote _song;
        private int _until;
        private int _nextSong;
        private int _nextOrder;

        public FlashMob(TrapsConfig config) : base(config)
        {
        }

        private FlashMobConfig Settings => Config.FlashMob;
        public override string Id => TrapIds.FlashMob;
        public override string Title => "Flash mob";
        public override string Subtitle => "Five, six, seven, eight!";
        public override bool Enabled => Settings.Enabled;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft ? "not while flying" : null;
            return reason == null;
        }

        public override void Start()
        {
            _dancers.Clear();
            _until = StartTimer(Settings.Duration);
            _nextSong = 0;
            _nextOrder = 0;
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            Ped player = Player;
            if (now >= _until || player.IsDead) return false;
            bool newSong = now >= _nextSong;
            if (newSong)
            {
                _nextSong = now + Settings.SongLength;
                _song = Spawner.Pick(Settings.Emotes);
                _dancers.RemoveAll(d => !d.Exists() || d.IsDead);
                foreach (Ped ped in Spawner.AmbientPeds(Settings.Radius, Settings.MaxPeds - _dancers.Count, p => !_dancers.Contains(p) && !p.IsInVehicle()))
                {
                    ped.BlockPermanentEvents = true;
                    _dancers.Add(ped);
                }
            }
            if (!newSong && now < _nextOrder) return true;
            _nextOrder = now + 1000;
            foreach (Ped dancer in _dancers)
            {
                if (!dancer.Exists() || dancer.IsDead) continue;
                float distance = dancer.Position.DistanceTo(player.Position);
                bool dancing = dancer.Model.IsValid && Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, dancer, _song.Dict ?? string.Empty, _song.Name ?? string.Empty, 3);
                if (distance > Settings.FollowDistance + 4f)
                {
                    Function.Call(Hash.TASK_GO_TO_ENTITY, dancer, player, -1, Settings.FollowDistance * 0.6f, 2.5f, 0f, 0);
                }
                else if (newSong || (!dancing && !Function.Call<bool>(Hash.IS_PED_USING_ANY_SCENARIO, dancer)))
                {
                    dancer.Heading = Spawner.HeadingTowards(dancer.Position, player.Position);
                    Emotes.Play(dancer, _song);
                }
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            foreach (Ped dancer in _dancers)
            {
                if (!dancer.Exists() || dancer.IsDead) continue;
                dancer.BlockPermanentEvents = false;
                Function.Call(Hash.CLEAR_PED_TASKS, dancer);
                Function.Call(Hash.TASK_WANDER_STANDARD, dancer, 10f, 10);
            }
            _dancers.Clear();
        }
    }
}
