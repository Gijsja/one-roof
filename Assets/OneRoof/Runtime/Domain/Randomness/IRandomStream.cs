namespace OneRoof.Domain.Randomness
{
    public interface IRandomStream
    {
        RandomStreamState State { get; }

        uint NextUInt32();

        int NextInt(int minimumInclusive, int maximumExclusive);
    }
}
