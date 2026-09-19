using System;

namespace OneRoof.Domain.Population
{
    /// <summary>Small, legible modifiers for long-horizon wellbeing responses.</summary>
    public enum PersonalityFacetKind
    {
        CommuteSensitive,
        CommunityRooted,
        PrivacySeeking,
        FinanciallyCautious,
        Resilient,
        ServiceExpectant
    }

    [Serializable]
    public readonly struct PersonalityFacet : IEquatable<PersonalityFacet>
    {
        public PersonalityFacet(PersonalityFacetKind kind) { Kind = kind; }
        public PersonalityFacetKind Kind { get; }
        public bool Equals(PersonalityFacet other) => Kind == other.Kind;
        public override bool Equals(object obj) => obj is PersonalityFacet other && Equals(other);
        public override int GetHashCode() => (int)Kind;
        public override string ToString() => Kind.ToString();
    }
}
