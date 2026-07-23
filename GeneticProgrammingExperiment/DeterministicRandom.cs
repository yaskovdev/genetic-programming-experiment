namespace GeneticProgrammingExperiment;

internal sealed class DeterministicRandom(ulong seed)
{
    private ulong _state = seed;

    public ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15ul;
        var value = _state;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9ul;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBul;
        return value ^ (value >> 31);
    }

    public int NextInt(int exclusiveMaximum)
    {
        if (exclusiveMaximum <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum), "The exclusive maximum must be positive.");
        }

        return (int)(NextUInt64() % (uint)exclusiveMaximum);
    }

    public bool NextChance(int percentage)
    {
        if (percentage < 0 || percentage > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage), "The percentage must be between 0 and 100.");
        }

        return NextInt(100) < percentage;
    }
}
