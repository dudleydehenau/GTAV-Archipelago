using GTA;

namespace GameEnvironment.Core
{
    internal sealed class PhoneBook
    {
        private const int Interval = 3000;
        private static readonly int[] StoryContacts =
        {
            0, 1, 2, 12, 14, 15, 17, 18, 19, 20, 23, 24, 26, 29, 30, 32, 35, 36, 37,
        };

        private readonly StoryGlobals _globals;
        private int _next;
        private bool _warned;

        public PhoneBook(StoryGlobals globals)
        {
            _globals = globals;
        }

        public void Tick()
        {
            if (Game.GameTime < _next || !_globals.Available) return;
            _next = Game.GameTime + Interval;
            if (!_globals.ContactsSupported)
            {
                if (!_warned)
                {
                    _warned = true;
                    Log.Warn("Phone contacts not supported by this game build: the phone keeps the game's contacts.");
                }
                return;
            }
            int added = _globals.AddContacts(StoryContacts);
            if (added > 0) Log.Info($"Phone contacts restored ({added} entries): missions that send a photo or make a call find their contact.");
        }
    }
}
