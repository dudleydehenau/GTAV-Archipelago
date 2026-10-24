namespace GameCollectibles.Core
{
    internal static class GameText
    {
        public static string Kind(CollectibleKind kind)
        {
            switch (kind)
            {
                case CollectibleKind.SpaceshipPart: return "Spaceship parts";
                case CollectibleKind.LetterScrap: return "Letter scraps";
                case CollectibleKind.NuclearWaste: return "Nuclear waste";
                default: return "Submarine parts";
            }
        }
    }
}
