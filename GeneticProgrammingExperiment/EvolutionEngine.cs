namespace GeneticProgrammingExperiment;

public sealed class EvolutionEngine
{
    public EvolutionResult Run(
        EvolutionOptions? options = null,
        IProgress<EvolutionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new EvolutionOptions();
        options.Validate();

        var random = new DeterministicRandom(unchecked((ulong)options.RandomSeed));
        var seedGenome = PushGenome.FromProgram(PushProgram.Parse(options.SeedProgramSource));
        if (seedGenome.PointCount > options.MaximumProgramPoints)
        {
            throw new ArgumentException(
                $"The seed program contains {seedGenome.PointCount} points, exceeding the configured maximum of {options.MaximumProgramPoints}.",
                nameof(options.SeedProgramSource));
        }

        var population = CreateInitialPopulation(seedGenome, options, random, cancellationToken);

        Candidate best = population[0];
        for (var generation = 0; generation <= options.Generations; generation++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            population.Sort(CompareCandidates);
            best = population[0];

            progress?.Report(
                new EvolutionProgress(
                    generation,
                    options.Generations,
                    best.Fitness,
                    population.Average(candidate => candidate.Fitness),
                    best.AverageFoodEaten,
                    best.AverageTicksAlive,
                    best.Program));

            if (generation == options.Generations)
            {
                break;
            }

            population = CreateNextGeneration(population, options, random, cancellationToken);
        }

        return new EvolutionResult(best.Program, best.Fitness, best.AverageFoodEaten, best.AverageTicksAlive, options.Generations);
    }

    private static List<Candidate> CreateInitialPopulation(
        PushGenome seedGenome,
        EvolutionOptions options,
        DeterministicRandom random,
        CancellationToken cancellationToken)
    {
        var population = new List<Candidate>(options.PopulationSize)
        {
            Evaluate(seedGenome, options, cancellationToken)
        };

        while (population.Count < options.PopulationSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PushGenome genome;
            if (random.NextChance(options.RandomGenomePercentage))
            {
                genome = PushGenome.CreateRandom(random, options.MaximumProgramPoints);
            }
            else
            {
                genome = seedGenome;
                var mutationCount = 1 + random.NextInt(options.MaximumMutationsPerChild);
                for (var mutation = 0; mutation < mutationCount; mutation++)
                {
                    genome = genome.Mutate(random, options.MaximumProgramPoints);
                }
            }

            population.Add(Evaluate(genome, options, cancellationToken));
        }

        return population;
    }

    private static List<Candidate> CreateNextGeneration(
        IReadOnlyList<Candidate> population,
        EvolutionOptions options,
        DeterministicRandom random,
        CancellationToken cancellationToken)
    {
        var next = new List<Candidate>(options.PopulationSize);
        for (var elite = 0; elite < options.EliteCount; elite++)
        {
            next.Add(population[elite]);
        }

        while (next.Count < options.PopulationSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parent = SelectTournament(population, options.TournamentSize, random);
            var childGenome = parent.Genome;
            var mutationCount = 1 + random.NextInt(options.MaximumMutationsPerChild);
            for (var mutation = 0; mutation < mutationCount; mutation++)
            {
                childGenome = childGenome.Mutate(random, options.MaximumProgramPoints);
            }

            next.Add(Evaluate(childGenome, options, cancellationToken));
        }

        return next;
    }

    private static Candidate SelectTournament(IReadOnlyList<Candidate> population, int tournamentSize, DeterministicRandom random)
    {
        var best = population[random.NextInt(population.Count)];
        for (var contestant = 1; contestant < tournamentSize; contestant++)
        {
            var candidate = population[random.NextInt(population.Count)];
            if (CompareCandidates(candidate, best) < 0)
            {
                best = candidate;
            }
        }

        return best;
    }

    private static Candidate Evaluate(PushGenome genome, EvolutionOptions options, CancellationToken cancellationToken)
    {
        var program = genome.Develop();
        double totalFitness = 0;
        double totalFoodEaten = 0;
        double totalTicksAlive = 0;

        foreach (var foodSeed in options.FoodSeeds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var world = World.CreateDefault(program, foodSeed);
            while (world.Tick < options.EpisodeTicks && world.IsAgentAlive)
            {
                world.Advance();
            }

            totalFoodEaten += world.AgentFoodEaten;
            totalTicksAlive += world.Tick;
            totalFitness += world.AgentFoodEaten * 100 + world.Tick + world.AgentEnergy;
        }

        var worldCount = options.FoodSeeds.Length;
        var averageFitness = totalFitness / worldCount - genome.PointCount * 0.05;
        return new Candidate(
            genome,
            program,
            averageFitness,
            totalFoodEaten / worldCount,
            totalTicksAlive / worldCount);
    }

    private static int CompareCandidates(Candidate left, Candidate right)
    {
        var fitnessComparison = right.Fitness.CompareTo(left.Fitness);
        if (fitnessComparison != 0)
        {
            return fitnessComparison;
        }

        return left.Genome.PointCount.CompareTo(right.Genome.PointCount);
    }

    private sealed record Candidate(
        PushGenome Genome,
        PushProgram Program,
        double Fitness,
        double AverageFoodEaten,
        double AverageTicksAlive);
}
