namespace GeneticProgrammingExperiment;

public sealed class WorldSnapshot
{
    private readonly byte[] _food;

    internal WorldSnapshot(int width, int height, long tick, byte[] food, AgentSnapshot agent)
    {
        Width = width;
        Height = height;
        Tick = tick;
        _food = food;
        Agent = agent;
    }

    public int Width { get; }

    public int Height { get; }

    public long Tick { get; }

    public AgentSnapshot Agent { get; }

    public int FoodAt(int x, int y)
    {
        if (x < 0 || x >= Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if (y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        return _food[y * Width + x];
    }
}

public sealed record AgentSnapshot(
    int X,
    int Y,
    Direction Direction,
    int Energy,
    int MaximumEnergy,
    bool IsAlive,
    int FoodEaten,
    AgentAction LastAction,
    int InstructionsExecuted,
    bool ReachedInstructionLimit,
    string Program);

public readonly record struct FoodPlacement(int X, int Y, int Amount = 1);
