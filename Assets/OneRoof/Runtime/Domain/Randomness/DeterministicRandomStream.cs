using System;

namespace OneRoof.Domain.Randomness
{
    /// <summary>Saveable xorshift64 stream with a stable, documented sequence.</summary>
    public sealed class DeterministicRandomStream : IRandomStream
    {
        private const ulong ZeroSeedReplacement = 0x9E3779B97F4A7C15UL;
        private readonly ulong _seed;
        private ulong _state;
        private long _position;

        public DeterministicRandomStream(ulong seed)
        {
            _seed = seed;
            _state = seed == 0 ? ZeroSeedReplacement : seed;
        }

        private DeterministicRandomStream(RandomStreamState state)
        {
            _seed = state.Seed;
            _state = state.State;
            _position = state.Position;
        }

        public RandomStreamState State => new RandomStreamState(_seed, _state, _position);

        public static DeterministicRandomStream Restore(RandomStreamState state) => new DeterministicRandomStream(state);

        public uint NextUInt32()
        {
            var value = NextUInt64();
            return (uint)(value >> 32);
        }

        public int NextInt(int minimumInclusive, int maximumExclusive)
        {
            if (minimumInclusive >= maximumExclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumExclusive), "The maximum must be greater than the minimum.");
            }

            var range = (ulong)((long)maximumExclusive - minimumInclusive);
            var limit = ulong.MaxValue - (ulong.MaxValue % range);
            ulong sample;
            do
            {
                sample = NextUInt64();
            }
            while (sample >= limit);

            return (int)(minimumInclusive + (long)(sample % range));
        }

        private ulong NextUInt64()
        {
            var value = _state;
            value ^= value << 13;
            value ^= value >> 7;
            value ^= value << 17;
            _state = value;
            _position = checked(_position + 1);
            return value;
        }
    }
}
