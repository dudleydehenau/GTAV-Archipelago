using System.Collections.Generic;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal abstract class SquadTrap : Trap
    {
        protected readonly Squad Squad = new Squad();
        private int _until;

        protected SquadTrap(TrapsConfig config) : base(config)
        {
        }

        protected abstract SquadConfig Settings { get; }
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Police;

        public override bool CanStart(out string reason)
        {
            if (Player.IsInVehicle() || Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE, Player)) reason = "waiting for you to be on foot";
            else if (PlayerInWater) reason = "waiting for you to be on land";
            else reason = null;
            return reason == null;
        }

        public override void Start()
        {
            Squad.Accuracy = Settings.Accuracy;
            Squad.MoveRate = Settings.MoveRate > 0f ? Settings.MoveRate : 1f;
            if (!Squad.Load(Settings.Models, Settings.Weapons)) return;
            for (int i = 0; i < Settings.Count; i++) Squad.SpawnAround(Settings.SpawnDistance);
            NoPolice.Begin();
            _until = StartTimer(Settings.Duration);
            Started();
            Log.Info($"{Id}: {Squad.Alive} attackers.");
        }

        public override bool Update()
        {
            if (Game.GameTime >= _until || Player.IsDead) return false;
            NoPolice.Keep();
            Squad.Process(Settings.Count, Settings.SpawnDistance, Settings.Respawn);
            Updated();
            return true;
        }

        public override void Stop(bool interrupted)
        {
            NoPolice.End();
            Squad.Clear(true);
        }

        protected virtual void Started()
        {
        }

        protected virtual void Updated()
        {
        }
    }

    internal sealed class MacheteGranny : SquadTrap
    {
        public MacheteGranny(TrapsConfig config) : base(config)
        {
        }

        protected override SquadConfig Settings => Config.MacheteGranny;
        public override string Id => TrapIds.MacheteGranny;
        public override string Title => "Florida grannies";
        public override string Subtitle => "Grandma found the machete.";

        protected override void Started() =>
            Overrides.Subtitle("~y~Leonida welcomes you.", 4000);
    }

    internal sealed class FailedHeroes : SquadTrap
    {
        private const int LeapInterval = 7000;
        private static readonly int TitanModel = Safety.Joaat("u_m_y_imporage");
        private readonly Dictionary<int, int> _leaps = new Dictionary<int, int>();

        public FailedHeroes(TrapsConfig config) : base(config)
        {
        }

        protected override SquadConfig Settings => Config.FailedHeroes;
        public override string Id => TrapIds.FailedHeroes;
        public override string Title => "Tighten";
        public override string Subtitle => "The new hero went bad.";

        protected override void Started()
        {
            _leaps.Clear();
            Overrides.Subtitle("~r~Titan:~s~ I'm Tighten! The new villain of Los Santos! ~b~Minion:~s~ Sir, this monkey suit itches my scales.", 7000);
        }

        protected override void Updated()
        {
            int now = Game.GameTime;
            Ped player = Player;
            foreach (Ped member in Squad.Members)
            {
                if (!member.Exists() || member.IsDead || member.Model.Hash != TitanModel) continue;
                if (!_leaps.TryGetValue(member.Handle, out int last))
                {
                    _leaps[member.Handle] = now;
                    continue;
                }
                if (now - last > 1200 && now - last < 1500 && member.Position.DistanceTo(player.Position) < 5f && !player.IsInVehicle())
                {
                    Function.Call(Hash.SET_PED_TO_RAGDOLL, player, 1500, 2000, 0, false, false, false);
                    Function.Call(Hash.SHAKE_GAMEPLAY_CAM, "LARGE_EXPLOSION_SHAKE", 0.3f);
                    _leaps[member.Handle] = last - 400;
                }
                if (now - last < LeapInterval) continue;
                float distance = member.Position.DistanceTo(player.Position);
                if (distance < 6f || distance > 60f) continue;
                _leaps[member.Handle] = now;
                Vector3 flat = player.Position - member.Position;
                flat.Z = 0f;
                float time = 1.3f;
                member.Velocity = flat / time + new Vector3(0f, 0f, 9.8f * time * 0.5f + 2f);
                Function.Call(Hash.PLAY_PAIN, member, 6, 0, 0);
            }
        }
    }
}
