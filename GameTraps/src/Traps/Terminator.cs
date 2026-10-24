using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class Terminator : Trap
    {
        private const uint FullAuto = 0xC6EE6B4C;
        private readonly EntityTracker _entities = new EntityTracker();
        private Ped _cyborg;
        private Vehicle _bike;
        private Model _model;
        private Model _bikeModel;
        private int _until;
        private int _nextOrder;

        public Terminator(TrapsConfig config) : base(config)
        {
        }

        private TerminatorConfig Settings => Config.Terminator;
        public override string Id => TrapIds.Terminator;
        public override string Title => "Terminator";
        public override string Subtitle => "It can't be bargained with.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Police;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft || PlayerInWater ? "waiting for you to be on land" : null;
            return reason == null;
        }

        public override void Start()
        {
            _model = Spawner.LoadModel(Settings.Model);
            if (_model.Hash == 0) return;
            _bikeModel = Spawner.LoadModel(Settings.BikeModel);
            Arrive(Settings.SpawnDistance);
            NoPolice.Begin();
            _until = StartTimer(Settings.Duration);
            Overrides.Subtitle("~r~I'm looking for Sarah Connor.", 5000);
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            Ped player = Player;
            if (now >= _until || player.IsDead) return false;
            NoPolice.Keep();
            if (_cyborg == null || !_cyborg.Exists())
            {
                Arrive(Settings.SpawnDistance);
                return true;
            }
            float distance = _cyborg.Position.DistanceTo(player.Position);
            if (distance > Settings.CatchUpDistance)
            {
                Spawner.Poof(_cyborg.Position);
                _cyborg.Delete();
                _cyborg = null;
                Overrides.Subtitle("~r~I'll be back.", 3000);
                Arrive(Settings.SpawnDistance);
                return true;
            }
            Function.Call(Hash.SET_PED_MOVE_RATE_OVERRIDE, _cyborg, 0.85f);
            if (now < _nextOrder) return true;
            _nextOrder = now + 2500;
            if (_cyborg.IsInVehicle())
            {
                if (!player.IsInVehicle() && distance < 20f)
                    Function.Call(Hash.TASK_LEAVE_VEHICLE, _cyborg, _cyborg.CurrentVehicle, 0);
                else
                    Function.Call(Hash.TASK_VEHICLE_CHASE, _cyborg, player);
                return true;
            }
            if (player.IsInVehicle() && distance > 30f && _bike != null && _bike.Exists() && _bike.Position.DistanceTo(_cyborg.Position) < 25f)
            {
                Function.Call(Hash.TASK_ENTER_VEHICLE, _cyborg, _bike, 5000, -1, 2f, 1, 0);
                return true;
            }
            if (player.IsInVehicle() && distance < 8f)
                Function.Call(Hash.TASK_COMBAT_PED, _cyborg, player, 0, 16);
            else
                Function.Call(Hash.TASK_GO_TO_ENTITY_WHILE_AIMING_AT_ENTITY, _cyborg, player, player, 1f, distance < 25f, 2f, 4f, true, false, FullAuto);
            return true;
        }

        public override void Stop(bool interrupted)
        {
            NoPolice.End();
            if (_cyborg != null && _cyborg.Exists()) Spawner.Poof(_cyborg.Position);
            _cyborg = null;
            _entities.DeleteAll();
            if (!interrupted) Overrides.Subtitle("~r~Hasta la vista, baby.", 4000);
        }

        private void Arrive(float distance)
        {
            Ped player = Player;
            Vector3 position = Spawner.GroundAround(player.Position, distance);
            Function.Call(Hash.FORCE_LIGHTNING_FLASH);
            Spawner.Ptfx("scr_rcbarry1", "scr_alien_teleport", position, 1.5f);
            Function.Call(Hash.SHAKE_GAMEPLAY_CAM, "SMALL_EXPLOSION_SHAKE", 0.4f);
            if (player.IsInVehicle() && _bikeModel.Hash != 0)
            {
                Vector3 street = Spawner.StreetBehind(distance);
                _bike = Vehicle.Create(_bikeModel, street, Spawner.HeadingTowards(street, player.Position));
                if (_bike != null)
                {
                    _entities.Add(_bike);
                    Function.Call(Hash.SET_VEHICLE_COLOURS, _bike, 0, 0);
                    position = street;
                }
            }
            _cyborg = _bike != null && _bike.Exists() && _bike.Position.DistanceTo(position) < 1f
                ? _bike.CreatePedOnSeat(VehicleSeat.Driver, _model)
                : Ped.Create(_model, position, Spawner.HeadingTowards(position, player.Position));
            if (_cyborg == null) return;
            _entities.Add(_cyborg);
            Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, _cyborg);
            if (Function.Call<int>(Hash.GET_NUMBER_OF_PED_PROP_DRAWABLE_VARIATIONS, _cyborg, 1) > 0)
                Function.Call(Hash.SET_PED_PROP_INDEX, _cyborg, 1, 0, 0, true);
            _cyborg.IsInvincible = true;
            _cyborg.CanRagdoll = false;
            Function.Call(Hash.SET_PED_SUFFERS_CRITICAL_HITS, _cyborg, false);
            Function.Call(Hash.SET_PED_CAN_BE_DRAGGED_OUT, _cyborg, false);
            Spawner.Give(_cyborg, Settings.Weapon);
            Function.Call(Hash.SET_PED_ACCURACY, _cyborg, Settings.Accuracy);
            Spawner.MakeHostile(_cyborg, true, false);
            Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, _cyborg, 1);
            Function.Call(Hash.SET_PED_HELMET, _cyborg, false);
            _nextOrder = 0;
        }
    }
}
