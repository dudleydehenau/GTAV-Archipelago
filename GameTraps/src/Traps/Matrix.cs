using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class TheMatrix : Trap
    {
        private readonly Squad _agents = new Squad();
        private int _until;

        public TheMatrix(TrapsConfig config) : base(config)
        {
        }

        private MatrixConfig Settings => Config.Matrix;
        public override string Id => TrapIds.Matrix;
        public override string Title => "The Matrix";
        public override string Subtitle => "There is no spoon.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Vision | TrapChannel.Time | TrapChannel.Police;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft ? "not while flying" : null;
            return reason == null;
        }

        public override void Start()
        {
            Overrides.Timecycle(Settings.Timecycle, 0.6f);
            Function.Call(Hash.SET_TIME_SCALE, Settings.TimeScale);
            NoPolice.Begin();
            _agents.Accuracy = Settings.Accuracy;
            _agents.Poofs = false;
            _agents.Spawned = OnSpawned;
            _agents.Died = OnDied;
            _agents.Load(Settings.Models, Settings.Weapons);
            for (int i = 0; i < Settings.Agents; i++) _agents.SpawnAround(25f);
            _until = StartTimer(Settings.Duration);
            Overrides.Subtitle("~g~Mr. Anderson... welcome back. We missed you.", 6000);
        }

        public override bool Update()
        {
            if (Game.GameTime >= _until || Player.IsDead) return false;
            NoPolice.Keep();
            Function.Call(Hash.SET_TIME_SCALE, Settings.TimeScale);
            _agents.Process(0, 25f, false);
            return true;
        }

        public override void Stop(bool interrupted)
        {
            Function.Call(Hash.SET_TIME_SCALE, 1f);
            Overrides.ClearTimecycle();
            NoPolice.End();
            _agents.Spawned = null;
            _agents.Died = null;
            _agents.Clear(true);
        }

        private void OnSpawned(Ped agent)
        {
            agent.MaxHealth = 300;
            agent.Health = 300;
            Function.Call(Hash.SET_PED_COMBAT_ABILITY, agent, 2);
        }

        private void OnDied(Ped dead)
        {
            Vector3 position = dead.Position;
            for (int i = 0; i < 2 && _agents.Alive < Settings.MaxAgents; i++)
            {
                Vector3 spot = Spawner.GroundAround(position, Spawner.Range(2f, 5f));
                Ped copy = _agents.Spawn(spot);
                if (copy != null) Spawner.Ptfx("scr_rcbarry1", "scr_alien_teleport", spot, 0.8f);
            }
            Overrides.Subtitle($"~g~Agents: {_agents.Alive}", 2000);
        }
    }
}
