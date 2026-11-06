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
                case CollectibleKind.MonkeyMosaic: return "Monkey mosaics";
                case CollectibleKind.PeyotePlant: return "Peyote plants";
                case CollectibleKind.EpsilonTract: return "Epsilon tracts";
                default: return "Submarine parts";
            }
        }

        public static string Feat(FeatKind kind)
        {
            switch (kind)
            {
                case FeatKind.StuntJump: return "Stunt jumps";
                case FeatKind.UnderBridge: return "Under the bridge";
                case FeatKind.KnifeFlight: return "Knife flights";
                case FeatKind.Landmark: return "Landmarks";
                case FeatKind.Skill: return "Skills";
                case FeatKind.Activity: return "Activities";
                default: return "Police escapes";
            }
        }
    }
}
