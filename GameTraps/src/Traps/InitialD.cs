using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class InitialD : Trap
    {
        private readonly EntityTracker _entities = new EntityTracker();
        private readonly Ambience _ambience = new Ambience();
        private Vehicle _car;
        private Ped _rival;
        private Vector3 _lastVelocity;
        private float _water;
        private float _slosh;
        private bool _spilled;
        private int _until;
        private bool _delivered;
        private Vector3 _shop;
        private Blip _shopBlip;
        private int _nextOrder;
        private int _dejaVuUntil;

        public InitialD(TrapsConfig config) : base(config)
        {
        }

        private InitialDConfig Settings => Config.InitialD;
        public override string Id => TrapIds.InitialD;
        public override string Title => "Initial D";
        public override string Subtitle => "Tofu delivery on the mountain pass.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Player | TrapChannel.Weather;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft || PlayerInWater ? "waiting for you to be on land" : null;
            return reason == null;
        }

        public override void Start()
        {
            _water = 100f;
            _slosh = 0f;
            _spilled = false;
            _rival = null;
            Model model = Spawner.LoadAny(Settings.Models);
            if (model.Hash == 0) return;
            Ped player = Player;
            Spawner.WarpOutOfVehicle(player);
            GTA.UI.Screen.FadeOut(800);
            int deadline = System.Environment.TickCount + 2000;
            while (!GTA.UI.Screen.IsFadedOut && System.Environment.TickCount < deadline) Script.Wait(0);
            _car = Vehicle.Create(model, player.Position + new Vector3(0f, 0f, 30f), 0f);
            if (_car == null)
            {
                GTA.UI.Screen.FadeIn(500);
                return;
            }
            _entities.Add(_car);
            Function.Call(Hash.SET_VEHICLE_MOD_KIT, _car, 0);
            Function.Call(Hash.SET_VEHICLE_COLOURS, _car, 111, 0);
            Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, _car, "AKINA");
            int engine = Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS, _car, 11);
            if (engine > 0) Function.Call(Hash.SET_VEHICLE_MOD, _car, 11, engine - 1, false);
            Function.Call(Hash.TOGGLE_VEHICLE_MOD, _car, 18, true);
            Function.Call(Hash.SET_PED_INTO_VEHICLE, player, _car, -1);
            Destination start = Spawner.Pick(Settings.Destinations);
            Spawner.TeleportVehicle(_car, new Vector3(start.X, start.Y, start.Z), start.Heading);
            Function.Call(Hash.SET_VEHICLE_ENGINE_ON, _car, true, true, false);
            _ambience.Night(Settings.Hour);

            SpawnRival();
            PlaceShop();
            GTA.UI.Screen.FadeIn(800);
            _lastVelocity = Vector3.Zero;
            _until = StartTimer(Settings.Duration);
            _nextOrder = 0;
            Overrides.Subtitle("~y~Bunta:~s~ The tofu goes down the mountain. Don't spill the water.", 6000);
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            Ped player = Player;
            if (now >= _until || player.IsDead || _car == null || !_car.Exists() || _car.IsDead) return false;
            Function.Call(Hash.DRAW_MARKER, 1, _shop.X, _shop.Y, _shop.Z - 1f, 0f, 0f, 0f, 0f, 0f, 0f,
                8f, 8f, 3f, 255, 210, 0, 120, false, false, 2, false, 0, 0, false);
            if (player.IsInVehicle(_car) && _car.Position.DistanceTo2D(_shop) < Settings.DeliveryRadius)
            {
                _delivered = true;
                return false;
            }
            float dt = System.Math.Max(0.001f, Game.LastFrameTime);
            Vector3 velocity = _car.Velocity;
            if (player.IsInVehicle(_car) && !_spilled)
            {
                Vector3 change = velocity - _lastVelocity;
                change.Z = 0f;
                float accel = System.Math.Min(60f, change.Length() / dt);
                _slosh += (accel - _slosh) * System.Math.Min(1f, dt * 6f);
                if (_slosh > Settings.SpillThreshold) _water -= (_slosh - Settings.SpillThreshold) * Settings.SpillRate * dt;
                if (_water <= 0f) Spill();
            }
            if (Settings.Drift && player.IsInVehicle(_car)) Drift(velocity);
            _lastVelocity = _car.Velocity;

            System.Drawing.Color color = _water > 50f ? System.Drawing.Color.DeepSkyBlue : _water > 20f ? System.Drawing.Color.Orange : System.Drawing.Color.Red;
            Overrides.Banner($"Water: {System.Math.Max(0f, _water):0}%", color, 600f, 0.8f);

            Vector3 flat = new Vector3(velocity.X, velocity.Y, 0f);
            if (flat.Length() > 12f)
            {
                float slip = Vector3.Dot(flat.Normalized, _car.ForwardVector);
                if (slip < 0.9f) _dejaVuUntil = now + 800;
            }
            if (now < _dejaVuUntil) Overrides.Banner("DEJA VU!", System.Drawing.Color.Yellow, 120f, 1.3f);

            if (now >= _nextOrder && _rival != null && _rival.Exists() && !_rival.IsDead)
            {
                _nextOrder = now + 3000;
                Function.Call(Hash.TASK_VEHICLE_CHASE, _rival, player);
                Function.Call(Hash.SET_TASK_VEHICLE_CHASE_IDEAL_PURSUIT_DISTANCE, _rival, 0f);
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            if (_car != null && _car.Exists()) Function.Call(Hash.SET_VEHICLE_REDUCE_GRIP, _car, false);
            _shopBlip?.Delete();
            _shopBlip = null;
            if (!interrupted && !Player.IsDead)
            {
                if (!_delivered)
                {
                    Overrides.Subtitle("~y~Bunta:~s~ Too late. The customer went to the supermarket.", 6000);
                }
                else if (_spilled)
                {
                    Overrides.Subtitle("~y~Bunta:~s~ Delivered... but the water's gone and the tofu is ruined. Not a cent.", 6000);
                }
                else
                {
                    int pay = (int)(Settings.Reward * System.Math.Max(0f, System.Math.Min(100f, _water)) / 100f);
                    Wallet.Give(pay);
                    Overrides.Beep("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                    Overrides.Subtitle($"~y~Bunta:~s~ Tofu delivered with {_water:0}% of the water. Here's ${pay}.", 6000);
                }
            }
            _ambience.Restore();
            _car = null;
            _rival = null;
            if (interrupted) _entities.DeleteAll();
            else _entities.Forget();
        }

        private void Drift(Vector3 velocity)
        {
            if (!_car.IsOnAllWheels) return;
            Vector3 flat = new Vector3(velocity.X, velocity.Y, 0f);
            float speed = flat.Length();
            Vector3 last = new Vector3(_lastVelocity.X, _lastVelocity.Y, 0f);
            if (speed < 8f || last.Length() < 8f) return;
            float steer = System.Math.Abs(Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, 59));
            bool handbrake = Function.Call<bool>(Hash.IS_CONTROL_PRESSED, 0, 76);
            if (!handbrake && steer < 0.5f) return;
            Vector3 forward = new Vector3(_car.ForwardVector.X, _car.ForwardVector.Y, 0f).Normalized;
            float angle = (float)(System.Math.Acos(System.Math.Max(-1f, System.Math.Min(1f, Vector3.Dot(flat.Normalized, forward)))) * 180.0 / System.Math.PI);
            if (angle < Settings.MaxDriftAngle)
            {
                Vector3 blended = flat.Normalized * (1f - Settings.DriftSlide) + last.Normalized * Settings.DriftSlide;
                Vector3 slide = blended.Normalized * speed;
                _car.Velocity = new Vector3(slide.X, slide.Y, velocity.Z);
            }
            Vector3 spin = _car.WorldRotationVelocity;
            if (System.Math.Abs(spin.Z) > Settings.MaxYawRate)
                _car.WorldRotationVelocity = new Vector3(spin.X, spin.Y, System.Math.Sign(spin.Z) * Settings.MaxYawRate);
        }

        private void PlaceShop()
        {
            _delivered = false;
            Destination shop = Settings.Delivery;
            _shop = new Vector3(shop.X, shop.Y, shop.Z);
            var node = new OutputArgument();
            if (Function.Call<bool>(Hash.GET_CLOSEST_VEHICLE_NODE, _shop.X, _shop.Y, _shop.Z, node, 1, 3f, 0))
            {
                Vector3 found = node.GetResult<Vector3>();
                if (found.DistanceTo(_shop) < 150f) _shop = found;
            }
            _shopBlip = Blip.Create(_shop);
            _shopBlip.Sprite = BlipSprite.Standard;
            _shopBlip.Color = BlipColor.Yellow;
            _shopBlip.Name = shop.Name;
            _shopBlip.ShowRoute = true;
        }

        private void Spill()
        {
            _spilled = true;
            _water = 0f;
            Function.Call(Hash.SET_VEHICLE_ENGINE_HEALTH, _car, 250f);
            Overrides.Subtitle("~y~Bunta:~s~ You spilled the water. No tofu tonight.", 5000);
        }

        private void SpawnRival()
        {
            Model model = Spawner.LoadAny(Settings.RivalModels);
            Model driver = Spawner.LoadModel(Settings.RivalDriver);
            if (model.Hash == 0 || driver.Hash == 0) return;
            Vector3 behind = _car.Position - _car.ForwardVector * 15f;
            Vehicle rival = Vehicle.Create(model, behind, _car.Heading);
            if (rival == null) return;
            _entities.Add(rival);
            Function.Call(Hash.SET_VEHICLE_MOD_KIT, rival, 0);
            Function.Call(Hash.SET_VEHICLE_COLOURS, rival, 0, 0);
            Function.Call(Hash.SET_VEHICLE_ENGINE_ON, rival, true, true, false);
            Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, rival, 5f);
            _rival = rival.CreatePedOnSeat(VehicleSeat.Driver, driver);
            if (_rival == null) return;
            _entities.Add(_rival);
            _rival.BlockPermanentEvents = true;
            Function.Call(Hash.SET_DRIVER_ABILITY, _rival, 1f);
            Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, _rival, 1f);
        }
    }
}
