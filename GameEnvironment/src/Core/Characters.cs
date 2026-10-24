using System;
using System.Collections.Generic;
using GTA;

namespace GameEnvironment.Core
{
    internal static class Characters
    {
        public static readonly PlayableCharacter[] All = { PlayableCharacter.Michael, PlayableCharacter.Franklin, PlayableCharacter.Trevor };

        public static Model ModelOf(PlayableCharacter character)
        {
            switch (character)
            {
                case PlayableCharacter.Franklin:
                    return new Model(PedHash.Franklin);
                case PlayableCharacter.Trevor:
                    return new Model(PedHash.Trevor);
                default:
                    return new Model(PedHash.Michael);
            }
        }

        public static PlayableCharacter? FromPed(Ped ped)
        {
            if (ped == null || !ped.Exists()) return null;
            int hash = ped.Model.Hash;
            foreach (PlayableCharacter character in All)
                if (ModelOf(character).Hash == hash) return character;
            return null;
        }

        public static PlayableCharacter? Current => FromPed(Game.Player.Character);

        public static bool TryParse(string text, out PlayableCharacter character)
        {
            character = PlayableCharacter.Michael;
            if (string.IsNullOrWhiteSpace(text)) return false;
            return Enum.TryParse(text.Trim(), true, out character) && Enum.IsDefined(typeof(PlayableCharacter), character);
        }

        public static bool IsValid(int index) => index >= 0 && index < 3;

        public static string Name(PlayableCharacter character) => character.ToString();

        public static IReadOnlyList<PlayableCharacter> Where(Func<PlayableCharacter, bool> predicate)
        {
            var list = new List<PlayableCharacter>();
            foreach (PlayableCharacter character in All)
                if (predicate(character)) list.Add(character);
            return list;
        }
    }
}
