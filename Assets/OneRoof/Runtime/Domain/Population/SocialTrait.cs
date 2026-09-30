using System;

namespace OneRoof.Domain.Population
{
    /// <summary>
    /// Personality archetype governing interpersonal attraction, chemistry, and behavioral friction.
    /// Pure C# domain record, fully deterministic.
    /// </summary>
    public enum SocialTraitKind
    {
        HopelessRomantic,
        Charismatic,
        Flirt,
        HotHeaded,
        GrudgeHolder,
        Abrasive,
        Loyal,
        Jealous
    }

    /// <summary>
    /// A single immutable social trait attached to a resident.
    /// </summary>
    [Serializable]
    public readonly struct SocialTrait : IEquatable<SocialTrait>
    {
        public SocialTrait(SocialTraitKind kind)
        {
            Kind = kind;
        }

        public SocialTraitKind Kind { get; }

        public bool Equals(SocialTrait other) => Kind == other.Kind;

        public override bool Equals(object obj) => obj is SocialTrait other && Equals(other);

        public override int GetHashCode() => (int)Kind;

        public override string ToString() => Kind.ToString();

        public static bool operator ==(SocialTrait left, SocialTrait right) => left.Equals(right);

        public static bool operator !=(SocialTrait left, SocialTrait right) => !left.Equals(right);
    }
}
