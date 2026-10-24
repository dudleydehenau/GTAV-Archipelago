using System.Collections.Generic;
using GameTraps.Core;
using GTA;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class Purge : Trap
    {
        private const string AlarmName = "PRISON_ALARMS";
        private readonly Squad _hunters = new Squad();
        private readonly List<Ped> _crowd = new List<Ped>();
        private readonly Ambience _ambience = new Ambience();
        private int _purgeGroup;
        private int _until;
        private int _nextCrowd;
        private bool _alarm;

        public Purge(TrapsConfig config) : base(config)
        {
        }

        private PurgeConfig Settings => Config.Purge;
        public override string Id => TrapIds.Purge;
        public override string Title => "The Purge";
        public override string Subtitle => "All crime is legal.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Police | TrapChannel.Weather;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft ? "not while flying" : null;
            return reason == null;
        }

        public override void Start()
        {
            _crowd.Clear();
            _ambience.Night(Settings.Hour);
            NoPolice.Begin();
            if (Settings.Alarm && Function.Call<bool>(Hash.PREPARE_ALARM, AlarmName))
            {
                Function.Call(Hash.START_ALARM, AlarmName, false);
                _alarm = true;
            }
            Overrides.Beep("Bed", "WastedSounds");
            Overrides.Subtitle("This is not a test. Commencing at the siren, all crime is legal for 60 seconds.", 7000);

            var result = new OutputArgument();
            Function.Call(Hash.ADD_RELATIONSHIP_GROUP, "GAMETRAPS_PURGE", result);
            _purgeGroup = result.GetResult<int>();
            foreach (string name in new[] { "CIVMALE", "CIVFEMALE", "PLAYER", "GANG_1", "GANG_2", "GANG_9", "GANG_10", "AMBIENT_GANG_LOST", "AMBIENT_GANG_BALLAS", "AMBIENT_GANG_FAMILY" })
            {
                int other = Safety.Joaat(name);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, _purgeGroup, other);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, other, _purgeGroup);
            }
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, _purgeGroup, _purgeGroup);

            _hunters.Accuracy = 15;
            _hunters.Load(Settings.Models, Settings.Weapons);
            for (int i = 0; i < Settings.Hunters; i++) _hunters.SpawnAround(40f);
            _until = StartTimer(Settings.Duration);
            _nextCrowd = 0;
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            if (now >= _until || Player.IsDead) return false;
            NoPolice.Keep();
            int left = (_until - now + 999) / 1000;
            Overrides.Banner($"PURGE  {left / 60}:{left % 60:00}", System.Drawing.Color.Red, 40f, 0.8f);
            _hunters.Process(Settings.Hunters, 40f, true);
            if (now < _nextCrowd) return true;
            _nextCrowd = now + 2000;
            _crowd.RemoveAll(p => !p.Exists() || p.IsDead);
            int room = Settings.CrowdMax - _crowd.Count;
            if (room <= 0) return true;
            foreach (Ped ped in Spawner.AmbientPeds(Settings.CrowdRadius, Settings.CrowdMax, p => !_crowd.Contains(p) && !p.IsInVehicle()))
            {
                if (room-- <= 0) break;
                _crowd.Add(ped);
                Spawner.Give(ped, Spawner.Pick(Settings.CrowdWeapons));
                Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, ped, _purgeGroup);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 46, true);
                Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, ped, 0, false);
                ped.BlockPermanentEvents = true;
                Function.Call(Hash.TASK_COMBAT_HATED_TARGETS_AROUND_PED, ped, 60f, 0);
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            NoPolice.End();
            if (_alarm) Function.Call(Hash.STOP_ALARM, AlarmName, true);
            _alarm = false;
            foreach (Ped ped in _crowd)
            {
                if (!ped.Exists() || ped.IsDead) continue;
                Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, ped, Safety.Joaat("CIVMALE"));
                Function.Call(Hash.REMOVE_ALL_PED_WEAPONS, ped, true);
                ped.BlockPermanentEvents = false;
                Function.Call(Hash.CLEAR_PED_TASKS, ped);
                Function.Call(Hash.TASK_WANDER_STANDARD, ped, 10f, 10);
            }
            _crowd.Clear();
            _hunters.Clear(true);
            _ambience.Restore();
            if (!interrupted) Overrides.Subtitle("The Purge is over. Thank you for your participation.", 5000);
        }
    }
}
