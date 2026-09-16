using System;

namespace OneRoof.Domain.Population
{
    /// <summary>
    /// Immutable snapshot of one need's current satisfaction for a resident.
    /// Satisfaction is clamped to [0, 1]: 0 = fully deprived, 1 = fully satisfied.
    /// </summary>
    [Serializable]
    public readonly struct NeedState : IEquatable<NeedState>
    {
        public NeedState(NeedKind kind, float satisfaction)
        {
            if (satisfaction < 0f || satisfaction > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(satisfaction),
                    satisfaction,
                    "Satisfaction must be in the range [0, 1].");
            }

            Kind = kind;
            Satisfaction = satisfaction;
        }

        public NeedKind Kind { get; }

        /// <summary>Current satisfaction level. 0 = fully deprived, 1 = fully satisfied.</summary>
        public float Satisfaction { get; }

        /// <summary>True when satisfaction is above the mid-point threshold.</summary>
        public bool IsSatisfied => Satisfaction >= 0.5f;

        public NeedState WithSatisfaction(float satisfaction) => new NeedState(Kind, satisfaction);

        public bool Equals(NeedState other) => Kind == other.Kind && Satisfaction.Equals(other.Satisfaction);

        public override bool Equals(object obj) => obj is NeedState other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ Satisfaction.GetHashCode();
            }
        }

        public override string ToString() => $"{Kind}: {Satisfaction:P0}";
    }
}
