using System;

namespace OneRoof.Domain.Identity
{
    /// <summary>Stable, save-local identity for a runtime entity.</summary>
    [Serializable]
    public readonly struct EntityId : IEquatable<EntityId>, IComparable<EntityId>
    {
        public EntityId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Entity IDs must be positive.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool IsValid => Value > 0;

        public void EnsureValid()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Entity IDs must be positive.");
            }
        }

        public int CompareTo(EntityId other) => Value.CompareTo(other.Value);

        public bool Equals(EntityId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is EntityId other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => Value.ToString();

        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);

        public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);
    }
}
