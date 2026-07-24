namespace GeneticProgrammingExperiment;

public sealed record EvolutionOptions
{
    public int PopulationSize { get; init; } = 100;

    public int Generations { get; init; } = 1000;

    public int EpisodeTicks { get; init; } = 400;

    public int TournamentSize { get; init; } = 5;

    public int EliteCount { get; init; } = 2;

    public int MaximumMutationsPerChild { get; init; } = 3;

    public int RandomGenomePercentage { get; init; } = 20;

    public int MaximumProgramPoints { get; init; } = PushGenome.DefaultMaximumPoints;

    public int RandomSeed { get; init; } = 12345;

    public int[] FoodSeeds { get; init; } = [11, 29, 47];

    public string SeedProgramSource { get; init; } = PushProgram.Empty;

    internal void Validate()
    {
        if (PopulationSize < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(PopulationSize), "The population must contain at least two genomes.");
        }

        if (Generations < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Generations), "At least one generation is required.");
        }

        if (EpisodeTicks < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(EpisodeTicks), "An episode must contain at least one tick.");
        }

        if (TournamentSize < 2 || TournamentSize > PopulationSize)
        {
            throw new ArgumentOutOfRangeException(nameof(TournamentSize), "Tournament size must be between two and the population size.");
        }

        if (EliteCount < 1 || EliteCount >= PopulationSize)
        {
            throw new ArgumentOutOfRangeException(nameof(EliteCount), "Elite count must be positive and smaller than the population.");
        }

        if (MaximumMutationsPerChild < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumMutationsPerChild), "Each child must allow at least one mutation.");
        }

        if (RandomGenomePercentage < 0 || RandomGenomePercentage > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(RandomGenomePercentage), "Random genome percentage must be between zero and one hundred.");
        }

        if (MaximumProgramPoints < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumProgramPoints), "Programs must allow at least two points.");
        }

        if (FoodSeeds.Length == 0)
        {
            throw new ArgumentException("At least one food-layout seed is required.", nameof(FoodSeeds));
        }

        if (string.IsNullOrWhiteSpace(SeedProgramSource))
        {
            throw new ArgumentException("A seed Push program is required.", nameof(SeedProgramSource));
        }
    }
}

public sealed record EvolutionProgress(
    int Generation,
    int TotalGenerations,
    double BestFitness,
    double AverageFitness,
    double BestAverageFoodEaten,
    double BestAverageTicksAlive,
    PushProgram BestProgram);

public sealed record EvolutionResult(
    PushProgram BestProgram,
    double Fitness,
    double AverageFoodEaten,
    double AverageTicksAlive,
    int Generation);
