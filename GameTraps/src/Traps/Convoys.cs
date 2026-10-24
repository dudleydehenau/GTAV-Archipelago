using System.Linq;
using System.Collections.Generic;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal abstract class ConvoyTrap : Trap
    {
        protected readonly Convoy Convoy = new Convoy();
        private int _until;
        private int _wave;
        private int _nextWave;

        protected ConvoyTrap(TrapsConfig config) : base(config)
        {
        }

        protected abstract ConvoyConfig Settings { get; }
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Police;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft || PlayerInWater ? "waiting for you to be on land" : null;
            return reason == null;
        }

        public override void Start()
        {
            Prepare();
            int count = Convoy.Spawn(Settings);
            _wave = 1;
            _nextWave = Game.GameTime + Settings.WaveInterval;
            NoPolice.Begin();
            _until = StartTimer(Settings.Duration);
            Started();
            Log.Info($"{Id}: {count} vehicles.");
        }

        public override bool Update()
        {
            if (Game.GameTime >= _until || Player.IsDead) return false;
            NoPolice.Keep();
            Convoy.Process();
            if (_wave < Settings.Waves && (Game.GameTime >= _nextWave || Convoy.ActiveVehicles == 0))
            {
                _wave++;
                _nextWave = Game.GameTime + Settings.WaveInterval;
                int count = Convoy.Spawn(Settings);
                Log.Info($"{Id}: wave {_wave}, {count} vehicles.");
            }
            Updated();
            return true;
        }

        public override void Stop(bool interrupted)
        {
            NoPolice.End();
            Convoy.Clear(interrupted);
            Stopped(interrupted);
        }

        protected virtual void Prepare()
        {
        }

        protected virtual void Started()
        {
        }

        protected virtual void Updated()
        {
        }

        protected virtual void Stopped(bool interrupted)
        {
        }
    }

    internal sealed class PhantomWedge : ConvoyTrap
    {
        public PhantomWedge(TrapsConfig config) : base(config)
        {
        }

        protected override ConvoyConfig Settings => Config.PhantomWedge;
        public override string Id => TrapIds.PhantomWedge;
        public override string Title => "Phantom Wedge";
        public override string Subtitle => "Battering rams incoming.";

        private readonly Dictionary<int, int> _launched = new Dictionary<int, int>();

        public override bool CanStart(out string reason)
        {
            Ped player = Player;
            if (!player.IsInVehicle()) reason = "waiting for you to drive";
            else if (PlayerInAircraft || player.CurrentVehicle.Model.IsBoat || PlayerInWater) reason = "waiting for you to drive on land";
            else reason = null;
            return reason == null;
        }

        protected override void Prepare()
        {
            Convoy.Invincible = true;
            Convoy.RamSpeed = Settings.RamSpeed;
            Convoy.MaxVehicles = Settings.MaxVehicles;
            _launched.Clear();
        }

        protected override void Started() =>
            Overrides.Subtitle("Something big is coming up behind you.", 4000);

        protected override void Updated()
        {
            int now = Game.GameTime;
            foreach (Vehicle wedge in Convoy.Vehicles)
            {
                if (!wedge.Exists() || wedge.IsDead || wedge.Speed < 4f) continue;
                Vector3 nose = wedge.Position + wedge.ForwardVector * 5f;
                foreach (Vehicle victim in World.GetNearbyVehicles(nose, 6f))
                {
                    if (victim == wedge || !victim.Exists() || Convoy.Vehicles.Contains(victim)) continue;
                    Launch(victim, wedge, now, 18f);
                }
                foreach (Ped victim in World.GetNearbyPeds(nose, 4f))
                {
                    if (!victim.Exists() || victim.IsInVehicle() || victim.IsDead) continue;
                    Function.Call(Hash.SET_PED_TO_RAGDOLL, victim, 3000, 4000, 0, false, false, false);
                    Launch(victim, wedge, now, 10f);
                }
            }
        }

        private void Launch(Entity victim, Vehicle wedge, int now, float lift)
        {
            if (_launched.TryGetValue(victim.Handle, out int last) && now - last < 2500) return;
            Vector3 toVictim = victim.Position - wedge.Position;
            if (Vector3.Dot(toVictim.Normalized, wedge.ForwardVector) < 0.5f) return;
            _launched[victim.Handle] = now;
            victim.Velocity = wedge.ForwardVector * (wedge.Speed + 6f) + new Vector3(0f, 0f, lift);
            Function.Call(Hash.PLAY_SOUND_FROM_ENTITY, -1, "Crash", victim, "DLC_IE_STEAL_EXPORT_SOUNDS", false, 0);
        }

        protected override void Stopped(bool interrupted) => _launched.Clear();
    }

    internal sealed class MadMax : ConvoyTrap
    {
        public MadMax(TrapsConfig config) : base(config)
        {
        }

        protected override ConvoyConfig Settings => Config.MadMax;
        public override string Id => TrapIds.MadMax;
        public override string Title => "Fury Road";
        public override string Subtitle => "Witness me!";

        protected override void Prepare() => Convoy.DirtLevel = 15f;

        protected override void Started() =>
            Overrides.Subtitle("~o~What a lovely day!", 4000);
    }

    internal sealed class HereWeGoAgain : ConvoyTrap
    {
        private readonly EntityTracker _friends = new EntityTracker();

        public HereWeGoAgain(TrapsConfig config) : base(config)
        {
        }

        private HereWeGoAgainConfig Extra => Config.HereWeGoAgain;
        protected override ConvoyConfig Settings => Config.HereWeGoAgain;
        public override string Id => TrapIds.HereWeGoAgain;
        public override string Title => "Here we go again";
        public override string Subtitle => "Ballas, purple car, drive-by.";

        protected override void Started()
        {
            Overrides.Subtitle("~g~Ah shit, here we go again.", 5000);
            List<Model> models = Spawner.LoadAll(Extra.FriendModels ?? new List<string>());
            if (models.Count == 0 || Extra.Friends <= 0) return;
            Ped player = Player;
            int group = Function.Call<int>(Hash.GET_PED_GROUP_INDEX, player);
            for (int i = 0; i < Extra.Friends; i++)
            {
                Vector3 position = Spawner.GroundAround(player.Position, Spawner.Range(3f, 6f));
                Ped friend = Ped.Create(Spawner.Pick(models), position, player.Heading);
                if (friend == null) continue;
                _friends.Add(friend);
                Function.Call(Hash.SET_PED_RANDOM_COMPONENT_VARIATION, friend, 0);
                if (Extra.FriendWeapons != null && Extra.FriendWeapons.Count > 0) Spawner.Give(friend, Spawner.Pick(Extra.FriendWeapons));
                Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, friend, group);
                Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, friend, Function.Call<int>(Hash.GET_PED_RELATIONSHIP_GROUP_HASH, player));
                Function.Call(Hash.SET_PED_ACCURACY, friend, 40);
                friend.MaxHealth = 400;
                friend.Health = 400;
                Blip blip = friend.AddBlip();
                blip.Color = BlipColor.Green;
                blip.Scale = 0.7f;
            }
        }

        protected override void Stopped(bool interrupted)
        {
            if (interrupted)
            {
                _friends.DeleteAll();
                return;
            }
            foreach (Entity entity in _friends.All)
                if (entity is Ped friend && friend.Exists()) Function.Call(Hash.REMOVE_PED_FROM_GROUP, friend);
            _friends.ReleaseAll();
        }
    }
}
