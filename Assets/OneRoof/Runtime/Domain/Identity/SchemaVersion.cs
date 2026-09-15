using System;

namespace OneRoof.Domain.Identity
{
    /// <summary>Positive version number for persisted schemas and authored content.</summary>
    [Serializable]
    public readonly struct SchemaVersion : IEquatable<SchemaVersion>, IComparable<SchemaVersion>
    {
        public SchemaVersion(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Schema versions must be positive.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool IsValid => Value > 0;

        public void EnsureValid()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Schema versions must be positive.");
            }
        }

        public int CompareTo(SchemaVersion other) => Value.CompareTo(other.Value);

        public bool Equals(SchemaVersion other) => Value == other.Value;

        public override bool Equals(object obj) => obj is SchemaVersion other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => Value.ToString();

        public static bool operator ==(SchemaVersion left, SchemaVersion right) => left.Equals(right);

        public static bool operator !=(SchemaVersion left, SchemaVersion right) => !left.Equals(right);

        public static bool operator <(SchemaVersion left, SchemaVersion right) => left.Value < right.Value;

        public static bool operator <=(SchemaVersion left, SchemaVersion right) => left.Value <= right.Value;

        public static bool operator >(SchemaVersion left, SchemaVersion right) => left.Value > right.Value;

        public static bool operator >=(SchemaVersion left, SchemaVersion right) => left.Value >= right.Value;
    }
}
