using GTA;
using GTA.Math;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal sealed class CableCar
    {
        private const string Script = "cablecar";
        private const int StackSize = 1424;
        private const int Interval = 1000;
        private const int LoadTimeout = 10000;

        private readonly int _hash = Game.GenerateHash(Script);
        private int _next;
        private int _loadingUntil;
        private bool _logged;

        public void Tick(int now)
        {
            if (now < _next) return;
            _next = now + (_loadingUntil > 0 ? 0 : Interval);
            if (StoryGlobals.IsScriptRunning(_hash))
            {
                _loadingUntil = 0;
                return;
            }
            if (!InArea() || Function.Call<bool>(Hash.GET_MISSION_FLAG) || Game.Player.Character.IsDead)
            {
                Release();
                return;
            }
            if (_loadingUntil == 0)
            {
                Function.Call(Hash.REQUEST_SCRIPT, Script);
                _loadingUntil = now + LoadTimeout;
            }
            if (!Function.Call<bool>(Hash.HAS_SCRIPT_LOADED, Script))
            {
                if (now >= _loadingUntil) Release();
                return;
            }
            Function.Call<int>(Hash.START_NEW_SCRIPT, Script, StackSize);
            Function.Call(Hash.SET_SCRIPT_AS_NO_LONGER_NEEDED, Script);
            _loadingUntil = 0;
            _next = now + Interval;
            if (_logged) return;
            _logged = true;
            Log.Info("Cable car started (the story would only start it later).");
        }

        private void Release()
        {
            if (_loadingUntil == 0) return;
            Function.Call(Hash.SET_SCRIPT_AS_NO_LONGER_NEEDED, Script);
            _loadingUntil = 0;
            _next = Game.GameTime + Interval;
        }

        private static bool InArea()
        {
            Vector3 at = Game.Player.Character.Position;
            return at.X >= -800f && at.X <= 640f && at.Y >= 5300f && at.Y <= 5800f;
        }
    }
}
