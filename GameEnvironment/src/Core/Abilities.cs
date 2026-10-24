using GTA;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal static class Abilities
    {
        private const int Interval = 2000;
        private static int _next;

        public static void Tick(EnvironmentConfig config, bool replay)
        {
            if (!config.UnlockSpecialAbilities || Game.GameTime < _next) return;
            _next = Game.GameTime + Interval;
            if (Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE)) return;
            foreach (PlayableCharacter character in Characters.All)
            {
                int model = Characters.ModelOf(character).Hash;
                if (Function.Call<bool>(Hash.IS_SPECIAL_ABILITY_UNLOCKED, model)) continue;
                Function.Call(Hash.SPECIAL_ABILITY_UNLOCK, model, 0);
                Log.Info($"Special ability of {character} unlocked.");
            }
            if (replay) return;
            Ped ped = Game.Player.Character;
            if (ped == null || !ped.Exists() || Characters.Current == null) return;
            if (Function.Call<bool>(Hash.IS_CUTSCENE_PLAYING)) return;
            if (!Function.Call<bool>(Hash.IS_SPECIAL_ABILITY_ENABLED, Game.Player, 0))
                Function.Call(Hash.ENABLE_SPECIAL_ABILITY, Game.Player, true, 0);
        }
    }
}
