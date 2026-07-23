namespace GeneticProgrammingExperiment.Tests;

using global::GeneticProgrammingExperiment;

[TestClass]
public sealed class EvolutionTest
{
    [TestMethod]
    public void RandomGenomesAndMutationsShouldBeDeterministicAndBounded()
    {
        var first = PushGenome.CreateRandom(123);
        var second = PushGenome.CreateRandom(123);
        var firstMutation = first.Mutate(456);
        var secondMutation = second.Mutate(456);
        var oversized = PushGenome.FromProgram(PushProgram.Parse(World.DefaultProgramSource));
        var reduced = oversized.Mutate(789, 2);

        Assert.AreEqual(first.Develop().Source, second.Develop().Source);
        Assert.AreEqual(firstMutation.Develop().Source, secondMutation.Develop().Source);
        Assert.IsLessThanOrEqualTo(PushGenome.DefaultMaximumPoints, firstMutation.PointCount);
        Assert.IsLessThanOrEqualTo(2, reduced.PointCount);
    }

    [TestMethod]
    public void FoodSeedsShouldProduceDifferentDeterministicLayouts()
    {
        var program = PushProgram.Parse("()");
        var first = World.CreateDefault(program, 1).Snapshot;
        var second = World.CreateDefault(program, 2).Snapshot;

        var differenceFound = false;
        for (var y = 0; y < first.Height && !differenceFound; y++)
        {
            for (var x = 0; x < first.Width; x++)
            {
                if (first.FoodAt(x, y) != second.FoodAt(x, y))
                {
                    differenceFound = true;
                    break;
                }
            }
        }

        Assert.IsTrue(differenceFound);
    }

    [TestMethod]
    public void EvolutionShouldBeDeterministic()
    {
        var options = new EvolutionOptions
        {
            PopulationSize = 24,
            Generations = 6,
            EpisodeTicks = 160,
            TournamentSize = 4,
            EliteCount = 2,
            FoodSeeds = [3, 7],
            RandomSeed = 9876
        };
        var engine = new EvolutionEngine();

        var first = engine.Run(options);
        var second = engine.Run(options);

        Assert.AreEqual(first.BestProgram.Source, second.BestProgram.Source);
        Assert.AreEqual(first.Fitness, second.Fitness);
        Assert.IsGreaterThan(0, first.AverageFoodEaten);
    }

    [TestMethod]
    public void EvolutionShouldRejectASeedAboveThePointLimit()
    {
        var options = new EvolutionOptions
        {
            PopulationSize = 4,
            Generations = 1,
            TournamentSize = 2,
            EliteCount = 1,
            MaximumProgramPoints = 2
        };

        Assert.ThrowsExactly<ArgumentException>(() => new EvolutionEngine().Run(options));
    }
}
