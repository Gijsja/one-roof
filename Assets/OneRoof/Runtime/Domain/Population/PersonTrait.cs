using System;

namespace OneRoof.Domain.Population
{
    /// <summary>
    /// A single personality trait that nudges a resident's schedule preferences and social behaviour.
    /// Traits are assigned at generation time from a seeded random stream and never change.
    /// </summary>
    [Serializable]
    public readonly struct PersonTrait : IEquatable<PersonTrait>
    {
        public PersonTrait(PersonTraitKind kind)
        {
            Kind = kind;
        }

        public PersonTraitKind Kind { get; }

        public bool Equals(PersonTrait other) => Kind == other.Kind;

        public override bool Equals(object obj) => obj is PersonTrait other && Equals(other);

        public override int GetHashCode() => (int)Kind;

        public override string ToString() => Kind.ToString();
    }

    /// <summary>
    /// Enumeration of personality trait archetypes used during first-playable scope.
    /// </summary>
    public enum PersonTraitKind
    {
        /// <summary>Wakes and sleeps earlier than average; schedule start times shifted by -60 ticks.</summary>
        EarlyBird,

        /// <summary>Wakes and sleeps later than average; schedule start times shifted by +60 ticks.</summary>
        NightOwl,

        /// <summary>Prefers shorter social contact; Social need decays more slowly.</summary>
        Introvert,

        /// <summary>Seeks more social contact; Social need decays faster.</summary>
        Extrovert,

        /// <summary>Spends less on food and leisure; Budget consumption rate reduced.</summary>
        Frugal,

        /// <summary>Spends more on food and leisure; Budget consumption rate increased.</summary>
        Spendthrift,
    }
}
