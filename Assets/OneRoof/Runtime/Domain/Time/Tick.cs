using System;

namespace OneRoof.Domain.Time
{
    /// <summary>Non-negative, fixed-step simulation time.</summary>
    [Serializable]
    public readonly struct Tick : IEquatable<Tick>, IComparable<Tick>
    {
        public Tick(long value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Ticks cannot be negative.");
            }

            Value = value;
        }

        public long Value { get; }

        public int CompareTo(Tick other) => Value.CompareTo(other.Value);

        public bool Equals(Tick other) => Value == other.Value;

        public override bool Equals(object obj) => obj is Tick other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString();

        public static bool operator ==(Tick left, Tick right) => left.Equals(right);

        public static bool operator !=(Tick left, Tick right) => !left.Equals(right);
    }
}
