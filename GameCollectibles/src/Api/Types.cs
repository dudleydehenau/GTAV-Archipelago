using GTA.Math;

namespace GameCollectibles
{
    public enum CollectibleKind
    {
        SpaceshipPart,
        LetterScrap,
        NuclearWaste,
        SubmarinePart,
    }

    public sealed class CollectibleInfo
    {
        internal CollectibleInfo(string id, CollectibleKind kind, int number, Vector3 position, bool collected)
        {
            Id = id;
            Kind = kind;
            Number = number;
            Position = position;
            Collected = collected;
        }

        public string Id { get; }
        public CollectibleKind Kind { get; }
        public int Number { get; }
        public Vector3 Position { get; }
        public bool Collected { get; }
    }
}
