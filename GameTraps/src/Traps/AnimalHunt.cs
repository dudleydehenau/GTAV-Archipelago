using System.Collections.Generic;
using System.Linq;
using GameTraps.Core;
using GTA;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class AnimalHunt : Trap
    {
        private readonly EntityTracker _entities = new EntityTracker();
        private readonly List<Ped> _hunters = new List<Ped>();
        private int _until;
        private int _nextOrder;

        public AnimalHunt(TrapsConfig config) : base(config)
        {
        }

        private AnimalHuntConfig Settings => Config.AnimalHunt;
        public override string Id => TrapIds.AnimalHunt;
        public override string Title => "Hunting season";
        public override string Subtitle => "You are the prey.";
        public override bool Enabled => Settings.Enabled;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft ? "not while flying" : null;
            return reason == null;
        }

        public override void Start()
        {
            _hunters.Clear();
            List<Model> models = Spawner.LoadAll(Settings.Models);
            if (models.Count == 0) return;
            Ped player = Player;
            for (int i = 0; i < Settings.Count; i++)
            {
                var position = Spawner.GroundAround(player.Position, Spawner.Range(Settings.SpawnDistance * 0.8f, Settings.SpawnDistance * 1.2f));
                Ped animal = Ped.Create(Spawner.Pick(models), position, 0f);
                if (animal == null) continue;
                _entities.Add(animal);
                _hunters.Add(animal);
                Function.Call(Hash.SET_PED_RELATIONSHIP_GROUP_HASH, animal, Spawner.HostileGroup());
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, animal, 46, true);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, animal, 5, true);
                Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, animal, 0, false);
                animal.BlockPermanentEvents = true;
                Blip blip = animal.AddBlip();
                blip.Color = BlipColor.Red;
                blip.Scale = 0.7f;
                Attack(animal);
            }
            _until = StartTimer(Settings.Duration);
            _nextOrder = Game.GameTime + 3000;
            Log.Info($"Animal hunt: {_hunters.Count} animals.");
        }

        public override bool Update()
        {
            if (Player.IsDead) return false;
            if (Game.GameTime >= _until) return false;
            List<Ped> alive = _hunters.Where(h => h.Exists() && !h.IsDead).ToList();
            foreach (Ped dead in _hunters.Where(h => h.Exists() && h.IsDead)) dead.AttachedBlip?.Delete();
            if (alive.Count == 0) return false;
            if (Game.GameTime >= _nextOrder)
            {
                _nextOrder = Game.GameTime + 3000;
                foreach (Ped hunter in alive)
                    if (!Function.Call<bool>(Hash.IS_PED_IN_COMBAT, hunter, Player)) Attack(hunter);
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            Ped player = Player;
            foreach (Ped hunter in _hunters.Where(h => h.Exists() && !h.IsDead))
            {
                hunter.AttachedBlip?.Delete();
                Function.Call(Hash.CLEAR_PED_TASKS, hunter);
                Function.Call(Hash.TASK_SMART_FLEE_PED, hunter, player, 200f, -1, false, false);
            }
            _hunters.Clear();
            if (interrupted) _entities.DeleteAll();
            else _entities.Forget();
        }

        private static void Attack(Ped hunter) => Function.Call(Hash.TASK_COMBAT_PED, hunter, Player, 0, 16);
    }
}
