using System.Collections.Generic;

namespace GameEnvironment.Core
{
    internal sealed class GunShops
    {
        private const int First = 28;
        private const int Last = 38;
        private const int OpenBit = 1;
        private readonly StoryGlobals _globals;
        private readonly HashSet<int> _opened = new HashSet<int>();
        private bool _warned;

        public GunShops(StoryGlobals globals)
        {
            _globals = globals;
        }

        public void Tick(bool mission)
        {
            if (!_globals.Available) return;
            if (!_globals.ShopsSupported)
            {
                if (!_warned)
                {
                    _warned = true;
                    Log.Warn("Shop flags not found for this game build: Ammu-Nation stays as the game sets it.");
                }
                return;
            }
            if (mission) Open();
            else Close();
        }

        public void Close()
        {
            if (_opened.Count == 0 || !_globals.Available || !_globals.ShopsSupported) return;
            foreach (int shop in _opened)
            {
                int flags = _globals.GetShopFlags(shop);
                if ((flags & OpenBit) != 0) _globals.SetShopFlags(shop, flags & ~OpenBit);
            }
            _opened.Clear();
            Log.Info("Ammu-Nation closed again (mission over).");
        }

        private void Open()
        {
            int count = 0;
            for (int shop = First; shop <= Last; shop++)
            {
                int flags = _globals.GetShopFlags(shop);
                if ((flags & OpenBit) != 0) continue;
                _globals.SetShopFlags(shop, flags | OpenBit);
                _opened.Add(shop);
                count++;
            }
            if (count > 0) Log.Info($"Ammu-Nation opened for the mission ({count} shops).");
        }
    }
}
