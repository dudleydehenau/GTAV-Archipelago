using GTA;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal static class Abilities
    {
        private const int Interval = 2000;
        private static int _next;
        private static bool[] _allowed;

        public static bool IsAllowed(PlayableCharacter character) => _allowed == null || _allowed[(int)character];

        public static bool Restricted => _allowed != null;

        public static void SetAllowed(bool[] allowed)
        {
            bool[] next = allowed == null ? null : (bool[])allowed.Clone();
            if (Same(_allowed, next)) return;
            _allowed = next;
            _next = 0;
            if (next == null) Log.Info("Special abilities: all three allowed.");
            else Log.Info($"Special abilities allowed: Michael {next[0]}, Franklin {next[1]}, Trevor {next[2]}.");
        }

        public static void Tick(EnvironmentConfig config, bool replay)
        {
            if (Game.GameTime < _next) return;
            if (!config.UnlockSpecialAbilities && _allowed == null) return;
            _next = Game.GameTime + Interval;
            if (Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE)) return;
            foreach (PlayableCharacter character in Characters.All)
            {
                int model = Characters.ModelOf(character).Hash;
                bool unlocked = Function.Call<bool>(Hash.IS_SPECIAL_ABILITY_UNLOCKED, model);
                if (IsAllowed(character))
                {
                    if (unlocked || !config.UnlockSpecialAbilities && _allowed == null) continue;
                    Function.Call(Hash.SPECIAL_ABILITY_UNLOCK, model, 0);
                    Log.Info($"Special ability of {character} unlocked.");
                }
                else if (unlocked)
                {
                    Function.Call(Hash.SPECIAL_ABILITY_LOCK, model, 0);
                    Log.Info($"Special ability of {character} locked until its Archipelago item arrives.");
                }
            }
            Ped ped = Game.Player.Character;
            PlayableCharacter? current = Characters.Current;
            if (ped == null || !ped.Exists() || current == null) return;
            if (!IsAllowed(current.Value))
            {
                if (Function.Call<bool>(Hash.IS_SPECIAL_ABILITY_ACTIVE, Game.Player, 0))
                    Function.Call(Hash.SPECIAL_ABILITY_DEACTIVATE_FAST, Game.Player, 0);
                if (Function.Call<bool>(Hash.IS_SPECIAL_ABILITY_ENABLED, Game.Player, 0))
                    Function.Call(Hash.ENABLE_SPECIAL_ABILITY, Game.Player, false, 0);
                return;
            }
            if (replay || !config.UnlockSpecialAbilities && _allowed == null) return;
            if (Function.Call<bool>(Hash.IS_CUTSCENE_PLAYING)) return;
            if (!Function.Call<bool>(Hash.IS_SPECIAL_ABILITY_ENABLED, Game.Player, 0))
                Function.Call(Hash.ENABLE_SPECIAL_ABILITY, Game.Player, true, 0);
        }

        private static bool Same(bool[] a, bool[] b)
        {
            if (a == null || b == null) return a == b;
            for (int i = 0; i < 3; i++)
                if (a[i] != b[i]) return false;
            return true;
        }
    }
}
