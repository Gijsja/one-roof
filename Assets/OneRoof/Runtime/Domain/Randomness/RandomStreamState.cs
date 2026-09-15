using System;

namespace OneRoof.Domain.Randomness
{
    /// <summary>Serializable checkpoint for a deterministic random stream.</summary>
    [Serializable]
    public readonly struct RandomStreamState : IEquatable<RandomStreamState>
    {
        public RandomStreamState(ulong seed, ulong state, long position)
        {
            if (state == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(state), "Random stream state cannot be zero.");
            }

            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), position, "Random stream position cannot be negative.");
            }

            Seed = seed;
            State = state;
            Position = position;
        }

        public ulong Seed { get; }

        public ulong State { get; }

        public long Position { get; }

        public bool Equals(RandomStreamState other) => Seed == other.Seed && State == other.State && Position == other.Position;

        public override bool Equals(object obj) => obj is RandomStreamState other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Seed, State, Position);
    }
}
