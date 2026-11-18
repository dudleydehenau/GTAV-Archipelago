using GTA;
using GTA.Math;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal sealed class HoldUps
    {
        private const int Interval = 1000;
        private const float LaunchRange = 300f;
        private const int RobberyFlowFlag = 56;
        private static readonly int ScriptHash = unchecked((int)StringHash.AtStringHash("shoprobberies", 0u));

        private static readonly Vector3[] Stores =
        {
            new Vector3(-711.8212f, -915.9057f, 18.2377f), new Vector3(-52.7185f, -1756.175f, 28.4432f),
            new Vector3(1159.441f, -325.6666f, 68.2272f), new Vector3(1699.429f, 4928.642f, 41.0858f),
            new Vector3(-1822.926f, 788.9531f, 137.212f), new Vector3(1166.427f, 2703.528f, 37.1574f),
            new Vector3(-2973.414f, 390.6885f, 14.0433f), new Vector3(-1225.86f, -903.5782f, 11.3263f),
            new Vector3(1140.659f, -981.0806f, 45.4158f), new Vector3(-1490.275f, -382.8514f, 39.1634f),
            new Vector3(-3240.719f, 1004.508f, 11.8468f), new Vector3(-3039.249f, 589.3831f, 6.9251f),
            new Vector3(544.4275f, 2672.061f, 41.1726f), new Vector3(2558.754f, 385.599f, 107.6391f),
            new Vector3(2681.511f, 3282.763f, 54.2573f), new Vector3(1731.153f, 6411.633f, 34.0373f),
            new Vector3(1964.931f, 3741.207f, 31.3599f), new Vector3(29.0707f, -1348.773f, 28.5101f),
            new Vector3(376.8503f, 323.9777f, 102.5825f),
        };

        private readonly bool _eventsOff;
        private readonly StoryGlobals _globals;
        private bool _flagRaised;
        private Blip[] _blips;
        private int _next;
        private int _store = -2;
        private bool _running;

        public static bool NearStore { get; private set; }

        public HoldUps(StoryGlobals globals, bool eventsOff)
        {
            _globals = globals;
            _eventsOff = eventsOff;
        }

        public void Tick(int now)
        {
            if (_blips == null) DrawBlips();
            if (!_flagRaised && _globals.Available)
            {
                _flagRaised = true;
                if (_globals.RaiseFlowFlag(RobberyFlowFlag)) Log.Info("Store robberies allowed (story flag 56 raised).");
            }
            if (!_eventsOff) return;
            bool running = StoryGlobals.IsScriptRunning(ScriptHash);
            if (running != _running)
            {
                _running = running;
                Log.Info(running ? "The game's store robbery script is running (the clerk should be there)." : "The game's store robbery script stopped.");
            }
            if (now < _next) return;
            _next = now + Interval;
            Ped player = Game.Player.Character;
            int store = player == null || !player.Exists() || player.IsDead || Function.Call<bool>(Hash.GET_MISSION_FLAG) ? -1 : Nearest(player.Position, LaunchRange);
            if (store != _store)
            {
                bool wasOn = _store >= 0;
                _store = store;
                NearStore = store >= 0;
                if (store >= 0 && !wasOn)
                {
                    Function.Call(Hash.SET_RANDOM_EVENT_FLAG, true);
                    Log.Info($"Near store {store}: the game's store robbery is allowed here, the clerk can be held up.");
                }
                else if (store < 0 && wasOn)
                {
                    Function.Call(Hash.SET_RANDOM_EVENT_FLAG, false);
                }
            }
        }

        public void Release()
        {
            if (_store >= 0 && _eventsOff) Function.Call(Hash.SET_RANDOM_EVENT_FLAG, false);
            _store = -2;
            NearStore = false;
        }

        private static int Nearest(Vector3 here, float range)
        {
            int best = -1;
            float bestDistance = range;
            for (int i = 0; i < Stores.Length; i++)
            {
                float distance = Stores[i].DistanceTo(here);
                if (distance >= bestDistance) continue;
                best = i;
                bestDistance = distance;
            }
            return best;
        }

        private void DrawBlips()
        {
            _blips = new Blip[Stores.Length];
            for (int i = 0; i < Stores.Length; i++)
            {
                Blip blip = World.CreateBlip(Stores[i]);
                blip.Sprite = BlipSprite.Store;
                blip.Color = BlipColor.Red;
                blip.IsShortRange = true;
                blip.Name = "Hold Up a Store";
                _blips[i] = blip;
            }
            Log.Info($"{Stores.Length} stores to hold up put on the map.");
        }
    }
}
