using System.Collections.Generic;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class BigSmoke : Trap
    {
        private static readonly string[] English =
        {
            "I'll have two number 9s,",
            "a number 9 large,",
            "a number 6 with extra dip,",
            "a number 7,",
            "two number 45s, one with cheese,",
            "and a large soda.",
        };

        private readonly EntityTracker _entities = new EntityTracker();
        private readonly List<KeyValuePair<Prop, int>> _flying = new List<KeyValuePair<Prop, int>>();
        private List<Model> _food = new List<Model>();
        private Ped _cook;
        private int _thrown;
        private int _nextThrow;
        private int _nextLine;
        private int _line;

        public BigSmoke(TrapsConfig config) : base(config)
        {
        }

        private BigSmokeConfig Settings => Config.BigSmoke;
        public override string Id => TrapIds.BigSmoke;
        public override string Title => "Big Smoke's order";
        public override string Subtitle => "Delivery!";
        public override bool Enabled => Settings.Enabled;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft || PlayerInWater ? "waiting for you to be on land" : null;
            return reason == null;
        }

        public override void Start()
        {
            _thrown = 0;
            _flying.Clear();
            _line = 0;
            _nextLine = 0;
            _food = Spawner.LoadAll(Settings.FoodModels);
            Model cook = Spawner.LoadModel(Settings.Model);
            if (cook.Hash == 0 || _food.Count == 0) return;
            Ped player = Player;
            Entity mover = player.IsInVehicle() ? (Entity)player.CurrentVehicle : player;
            Vector3 position = Spawner.GroundAround(mover.Position + mover.ForwardVector * 6f, 1f);
            _cook = Ped.Create(cook, position, Spawner.HeadingTowards(position, player.Position));
            if (_cook == null) return;
            _entities.Add(_cook);
            _cook.BlockPermanentEvents = true;
            _cook.IsInvincible = true;
            Spawner.Poof(position);
            _nextThrow = Game.GameTime + 1500;
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            Ped player = Player;
            if (_cook == null || !_cook.Exists() || player.IsDead) return false;
            Settle(now);
            if (_line < English.Length && now >= _nextLine)
            {
                GTA.UI.Screen.ShowSubtitle("~g~" + English[_line], 2200);
                _line++;
                _nextLine = now + 2000;
            }
            if (_thrown >= Settings.Count) return now < _nextLine + 1500;
            if (now < _nextThrow) return true;
            _nextThrow = now + Settings.Interval;
            _thrown++;

            if (_cook.Position.DistanceTo(player.Position) > 10f)
            {
                Vector3 near = Spawner.GroundAround(player.Position, 5f);
                _cook.Position = near;
            }
            _cook.Heading = Spawner.HeadingTowards(_cook.Position, player.Position);
            Vector3 hand = _cook.Position + _cook.ForwardVector * 0.6f + new Vector3(0f, 0f, 0.8f);
            Prop food = Prop.Create(Spawner.Pick(_food), hand, true, false);
            if (food == null) return true;
            _entities.Add(food);
            _flying.Add(new KeyValuePair<Prop, int>(food, now + 1500));
            food.IsPositionFrozen = false;
            Function.Call(Hash.SET_ENTITY_DYNAMIC, food, true);
            Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, food, true);
            Function.Call(Hash.ACTIVATE_PHYSICS, food);
            Vector3 aim = player.Position + new Vector3(Spawner.Range(-0.4f, 0.4f), Spawner.Range(-0.4f, 0.4f), Spawner.Range(0.2f, 0.8f));
            Vector3 direction = (aim - hand).Normalized;
            food.Velocity = direction * Settings.ThrowSpeed + new Vector3(0f, 0f, 1.5f);
            return true;
        }

        private void Settle(int now)
        {
            for (int i = _flying.Count - 1; i >= 0; i--)
            {
                Prop food = _flying[i].Key;
                if (now < _flying[i].Value) continue;
                _flying.RemoveAt(i);
                if (!food.Exists()) continue;
                bool floating = Function.Call<float>(Hash.GET_ENTITY_HEIGHT_ABOVE_GROUND, food) > 0.4f && food.Velocity.Length() < 0.3f;
                if (floating) food.Delete();
            }
        }

        public override void Stop(bool interrupted)
        {
            _flying.Clear();
            if (_cook != null && _cook.Exists()) Spawner.Poof(_cook.Position);
            _cook = null;
            if (interrupted)
            {
                _entities.DeleteAll();
                return;
            }
            foreach (Entity entity in _entities.All)
                if (entity is Ped ped && ped.Exists()) ped.Delete();
            _entities.Forget();
        }
    }
}
