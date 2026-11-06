using GTA.Math;

namespace GameCollectibles
{
    public enum CollectibleKind
    {
        SpaceshipPart,
        LetterScrap,
        NuclearWaste,
        SubmarinePart,
        MonkeyMosaic,
        PeyotePlant,
        EpsilonTract,
    }

    public enum FeatKind
    {
        StuntJump,
        UnderBridge,
        KnifeFlight,
        Landmark,
        Skill,
        Escape,
        Activity,
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

    public sealed class FeatInfo
    {
        internal FeatInfo(string id, FeatKind kind, string name, Vector3 position, bool done)
        {
            Id = id;
            Kind = kind;
            Name = name;
            Position = position;
            Done = done;
        }

        public string Id { get; }
        public FeatKind Kind { get; }
        public string Name { get; }
        public Vector3 Position { get; }
        public bool Done { get; }
    }
}
