namespace GeneticProgrammingExperiment.Tests;

using global::GeneticProgrammingExperiment;

[TestClass]
public sealed class WorldTest
{
    [TestMethod]
    public void ShouldWalkInASquare()
    {
        var world = World.CreateDefault();
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
    }

    [TestMethod]
    public void ShouldWrapMovementAtTheWorldEdge()
    {
        var program = PushProgram.Parse("(action.move-forward)");
        var world = new World(4, 3, 0, 1, Direction.West, program);

        var snapshot = world.Step();

        Assert.AreEqual(3, snapshot.Agent.X);
        Assert.AreEqual(1, snapshot.Agent.Y);
        Assert.AreEqual(AgentAction.MoveForward, snapshot.Agent.LastAction);
    }

    [TestMethod]
    public void ShouldChooseTheExpectedConditionalBranch()
    {
        var program = PushProgram.Parse(World.DefaultProgramSource);
        var interpreter = new PushInterpreter();

        var turn = interpreter.Execute(program, 0, 64);
        var move = interpreter.Execute(program, 1, 64);

        Assert.AreEqual(AgentAction.TurnRight, turn.Action);
        Assert.AreEqual(AgentAction.MoveForward, move.Action);
        Assert.IsFalse(turn.ReachedInstructionLimit);
        Assert.IsFalse(move.ReachedInstructionLimit);
    }
}
