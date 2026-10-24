using System.Collections.Generic;
using System.Linq;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class Clowns : Trap
    {
        private const string VisionIn = "DrugsTrevorClownsFightIn";
        private const string Vision = "DrugsTrevorClownsFight";
        private const string VisionOut = "DrugsTrevorClownsFightOut";
        private const int RespawnDelay = 1500;
        private readonly EntityTracker _entities = new EntityTracker();
        private readonly List<Ped> _clowns = new List<Ped>();
        private Model _clown;
        private uint _weapon;
        private int _until;
        private int _visionUntil;
        private int _nextSpawn;
        private bool _vision;

        public Clowns(TrapsConfig config) : base(config)
        {
        }

        private ClownsConfig Settings => Config.Clowns;
        public override string Id => TrapIds.Clowns;
        public override string Title => "Send in the clowns";
        public override string Subtitle => "Was that peyote?";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Vision | TrapChannel.Police;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft || PlayerInWater ? "waiting for you to be on land" : null;
            return reason == null;
        }

        public override void Start()
        {
            _clowns.Clear();
            _clown = Spawner.LoadModel(Settings.ClownModel);
            if (_clown.Hash == 0) return;
            Model van = Spawner.LoadModel(Settings.VanModel);
            Ped player = Player;
            _weapon = unchecked((uint)Safety.Joaat(string.IsNullOrEmpty(Settings.Weapon) ? "WEAPON_UNARMED" : Settings.Weapon));

            Vehicle vehicle = null;
            Vector3 origin = Spawner.StreetBehind(Settings.SpawnDistance);
            if (van.Hash != 0)
            {
                vehicle = Vehicle.Create(van, origin, Spawner.HeadingTowards(origin, player.Position));
                if (vehicle != null) _entities.Add(vehicle);
            }

            for (int i = 0; i < Settings.Count; i++)
            {
                Ped ped = null;
                int seat = i - 1;
                if (vehicle != null && Function.Call<bool>(Hash.IS_VEHICLE_SEAT_FREE, vehicle, seat, false))
                    ped = vehicle.CreatePedOnSeat((VehicleSeat)seat, _clown);
                if (ped == null) ped = Ped.Create(_clown, Spawner.GroundAround(origin, Spawner.Range(2f, 4f)), 0f);
                Arm(ped);
            }
            Spawner.Poof(origin + new Vector3(0f, 0f, 1f));
            ScreenEffects.Start(VisionIn, false);
            ScreenEffects.Start(Vision, true);
            _vision = true;
            NoPolice.Begin();
            _until = StartTimer(Settings.Duration);
            _visionUntil = Settings.VisionDuration > 0 ? Game.GameTime + Settings.VisionDuration : _until;
            _nextSpawn = 0;
            Log.Info($"Clowns: {_clowns.Count} clowns.");
        }

        public override bool Update()
        {
            if (_vision && Game.GameTime >= _visionUntil) EndVision();
            Ped player = Player;
            if (player.IsDead || Game.GameTime >= _until) return false;
            NoPolice.Keep();
            foreach (Ped dead in _clowns.Where(c => c.Exists() && c.IsDead).ToList())
            {
                dead.AttachedBlip?.Delete();
                _clowns.Remove(dead);
                if (_nextSpawn == 0) _nextSpawn = Game.GameTime + RespawnDelay;
            }
            _clowns.RemoveAll(c => !c.Exists());
            if (_clown.Hash != 0 && _clowns.Count < Settings.Count && _nextSpawn != 0 && Game.GameTime >= _nextSpawn)
            {
                Vector3 position = Spawner.GroundAround(player.Position, Spawner.Range(12f, 30f));
                Ped ped = Ped.Create(_clown, position, Spawner.HeadingTowards(position, player.Position));
                Spawner.Poof(position);
                Arm(ped);
                _nextSpawn = _clowns.Count < Settings.Count ? Game.GameTime + RespawnDelay : 0;
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            NoPolice.End();
            if (_vision) EndVision();
            foreach (Ped clown in _clowns)
                if (clown.Exists() && !clown.IsDead) Spawner.Poof(clown.Position);
            _clowns.Clear();
            _entities.DeleteAll();
        }

        private void Arm(Ped ped)
        {
            if (ped == null) return;
            _entities.Add(ped);
            _clowns.Add(ped);
            Function.Call(Hash.GIVE_WEAPON_TO_PED, ped, _weapon, 1, false, true);
            Spawner.MakeHostile(ped, true);
        }

        private void EndVision()
        {
            _vision = false;
            ScreenEffects.FadeOut(Vision, VisionOut, 2500);
        }
    }
}
