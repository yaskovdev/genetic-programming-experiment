namespace GeneticProgrammingExperiment.Tests;

using global::GeneticProgrammingExperiment;

[TestClass]
public sealed class WorldTest
{
    private const string DefaultProgramSource =
        "(sensor.food-here 0 integer.> exec.if (action.eat) (sensor.tick 8 integer.% 0 integer.= exec.if (action.turn-right) (action.move-forward)))";

    private const string WalkingProgramSource =
        "(sensor.tick 8 integer.% 0 integer.= exec.if (action.turn-right) (action.move-forward))";

    [TestMethod]
    public void ShouldWalkInASquareWithoutFood()
    {
        var program = PushProgram.Parse(WalkingProgramSource);
        var world = new World(24, 16, 15, 4, Direction.East, program, World.MaximumEnergy, []);
        var initial = world.Snapshot;

        for (var tick = 0; tick < 32; tick++)
        {
            world.Step();
        }

        var final = world.Snapshot;
        Assert.AreEqual(32, final.Tick);
        Assert.AreEqual(initial.Agent.X, final.Agent.X);
        Assert.AreEqual(initial.Agent.Y, final.Agent.Y);
        Assert.AreEqual(initial.Agent.Direction, final.Agent.Direction);
        Assert.AreEqual(40, final.Agent.Energy);
        Assert.IsTrue(final.Agent.IsAlive);
    }

    [TestMethod]
    public void ShouldWrapMovementAtTheWorldEdgeAndConsumeEnergy()
    {
        var program = PushProgram.Parse("(action.move-forward)");
        var world = new World(4, 3, 0, 1, Direction.West, program, 10, []);

        var snapshot = world.Step();

        Assert.AreEqual(3, snapshot.Agent.X);
        Assert.AreEqual(1, snapshot.Agent.Y);
        Assert.AreEqual(8, snapshot.Agent.Energy);
        Assert.AreEqual(AgentAction.MoveForward, snapshot.Agent.LastAction);
    }

    [TestMethod]
    public void ShouldEatFoodAndGainEnergy()
    {
        var program = PushProgram.Parse("(action.eat)");
        var world = new World(4, 3, 1, 1, Direction.North, program, 10, [new FoodPlacement(1, 1)]);

        var snapshot = world.Step();

        Assert.AreEqual(25, snapshot.Agent.Energy);
        Assert.AreEqual(1, snapshot.Agent.FoodEaten);
        Assert.AreEqual(0, snapshot.FoodAt(1, 1));
        Assert.AreEqual(AgentAction.Eat, snapshot.Agent.LastAction);
    }

    [TestMethod]
    public void ShouldDieWhenMetabolismConsumesTheLastEnergy()
    {
        var world = new World(4, 3, 1, 1, Direction.North, PushProgram.Parse("()"), 1, []);

        var snapshot = world.Step();

        Assert.AreEqual(0, snapshot.Agent.Energy);
        Assert.IsFalse(snapshot.Agent.IsAlive);
        Assert.AreEqual(AgentAction.None, snapshot.Agent.LastAction);
    }

    [TestMethod]
    public void ShouldPreserveTheActionThatConsumesTheLastEnergy()
    {
        var world = new World(4, 3, 0, 1, Direction.West, PushProgram.Parse("(action.move-forward)"), 2, []);

        var snapshot = world.Step();

        Assert.IsFalse(snapshot.Agent.IsAlive);
        Assert.AreEqual(3, snapshot.Agent.X);
        Assert.AreEqual(AgentAction.MoveForward, snapshot.Agent.LastAction);
        Assert.IsGreaterThan(0, snapshot.Agent.InstructionsExecuted);
    }

    [TestMethod]
    public void ShouldChooseForagingBranchesFromSensors()
    {
        var program = PushProgram.Parse(DefaultProgramSource);
        var interpreter = new PushInterpreter();

        var eat = interpreter.Execute(program, new PushSensors(0, 1, 0, 50), 64);
        var turn = interpreter.Execute(program, new PushSensors(0, 0, 0, 50), 64);
        var move = interpreter.Execute(program, new PushSensors(1, 0, 0, 50), 64);

        Assert.AreEqual(AgentAction.Eat, eat.Action);
        Assert.AreEqual(AgentAction.TurnRight, turn.Action);
        Assert.AreEqual(AgentAction.MoveForward, move.Action);
        Assert.IsFalse(eat.ReachedInstructionLimit);
        Assert.IsFalse(turn.ReachedInstructionLimit);
        Assert.IsFalse(move.ReachedInstructionLimit);
    }

    [TestMethod]
    public void ShouldExposeFoodAheadAndEnergySensors()
    {
        var interpreter = new PushInterpreter();
        var foodAheadProgram = PushProgram.Parse("(sensor.food-ahead 0 integer.> exec.if (action.move-forward) (action.turn-right))");
        var energyProgram = PushProgram.Parse("(sensor.energy 10 integer.> exec.if (action.eat) (action.turn-right))");

        var foodAhead = interpreter.Execute(foodAheadProgram, new PushSensors(0, 0, 1, 5), 64);
        var enoughEnergy = interpreter.Execute(energyProgram, new PushSensors(0, 0, 0, 11), 64);

        Assert.AreEqual(AgentAction.MoveForward, foodAhead.Action);
        Assert.AreEqual(AgentAction.Eat, enoughEnergy.Action);
    }

    [TestMethod]
    public void ShouldRegrowFoodOnlyOnFertileCells()
    {
        var world = new World(2, 1, 0, 0, Direction.North, PushProgram.Parse("(action.eat)"), World.MaximumEnergy, [new FoodPlacement(0, 0)]);

        for (var tick = 0; tick < World.FoodRegrowthPeriod + 1; tick++)
        {
            world.Step();
        }

        var snapshot = world.Snapshot;
        Assert.IsGreaterThan(1, snapshot.Agent.FoodEaten);
        Assert.AreEqual(0, snapshot.FoodAt(1, 0));
    }

    [TestMethod]
    public void DefaultForagerShouldDie()
    {
        var world = World.CreateDefault();

        for (var tick = 0; tick < 10_000 && world.Snapshot.Agent.IsAlive; tick++)
        {
            world.Step();
        }

        var snapshot = world.Snapshot;
        Assert.IsFalse(snapshot.Agent.IsAlive, $"The baseline forager didn't die at tick {snapshot.Tick} after eating {snapshot.Agent.FoodEaten} food.");
        Assert.AreEqual(0, snapshot.Agent.FoodEaten);
    }
}
