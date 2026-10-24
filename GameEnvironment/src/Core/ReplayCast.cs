using GTA;

namespace GameEnvironment.Core
{
    internal static class ReplayCast
    {
        private const int Interval = 250;
        private static int _next;

        public static void Tick(StoryGlobals globals)
        {
            if (Game.GameTime < _next) return;
            _next = Game.GameTime + Interval;
            foreach (PlayableCharacter character in Characters.All)
            {
                if (!globals.IsCharacterIntroduced(character)) globals.SetCharacterIntroduced(character);
                if (globals.IsCharacterAvailable(character)) continue;
                globals.SetCharacterAvailable(character, true);
                Log.Info($"Replay: {character} made available for mission switches.");
            }
        }
    }
}
