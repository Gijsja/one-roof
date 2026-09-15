using System;

namespace OneRoof.Domain.Identity
{
    /// <summary>Immutable authored-content identity in <c>namespace:name</c> form.</summary>
    [Serializable]
    public readonly struct ContentId : IEquatable<ContentId>, IComparable<ContentId>
    {
        public ContentId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Content IDs cannot be empty.", nameof(value));
            }

            var separator = value.IndexOf(':');
            if (separator <= 0 || separator == value.Length - 1 || separator != value.LastIndexOf(':') || ContainsWhitespace(value))
            {
                throw new ArgumentException("Content IDs must use the namespace:name form without whitespace.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public void EnsureValid()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Content IDs must be constructed from namespace:name.");
            }
        }

        public int CompareTo(ContentId other) => StringComparer.Ordinal.Compare(Value, other.Value);

        public bool Equals(ContentId other) => StringComparer.Ordinal.Equals(Value, other.Value);

        public override bool Equals(object obj) => obj is ContentId other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(ContentId left, ContentId right) => left.Equals(right);

        public static bool operator !=(ContentId left, ContentId right) => !left.Equals(right);

        private static bool ContainsWhitespace(string value)
        {
            foreach (var character in value)
            {
                if (char.IsWhiteSpace(character))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
