namespace GeneticProgrammingExperiment;

public sealed class World
{
    public const string DefaultProgramSource =
        "(sensor.tick 8 integer.% 0 integer.= exec.if (action.turn-right) (action.move-forward))";

    private const int InstructionLimit = 64;

    private readonly Agent _agent;

    public World(int width, int height, int agentX, int agentY, Direction direction, PushProgram program)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "The world width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "The world height must be positive.");
        }

        if (agentX < 0 || agentX >= width)
        {
            throw new ArgumentOutOfRangeException(nameof(agentX), "The agent must be inside the world.");
        }

        if (agentY < 0 || agentY >= height)
        {
            throw new ArgumentOutOfRangeException(nameof(agentY), "The agent must be inside the world.");
        }

        ArgumentNullException.ThrowIfNull(program);

        Width = width;
        Height = height;
        _agent = new Agent(agentX, agentY, direction, program);
    }

    public int Width { get; }

    public int Height { get; }

    public long Tick { get; private set; }

    public WorldSnapshot Snapshot => CreateSnapshot();

    public static World CreateDefault() =>
        new(24, 16, 15, 4, Direction.East, PushProgram.Parse(DefaultProgramSource));

    public WorldSnapshot Step()
    {
        var execution = _agent.Interpreter.Execute(_agent.Program, Tick, InstructionLimit);
        Apply(execution.Action);

        _agent.LastAction = execution.Action;
        _agent.InstructionsExecuted = execution.InstructionsExecuted;
        _agent.ReachedInstructionLimit = execution.ReachedInstructionLimit;
        Tick++;

        return CreateSnapshot();
    }

    private void Apply(AgentAction action)
    {
        switch (action)
        {
            case AgentAction.None:
                break;
            case AgentAction.TurnRight:
                _agent.Direction = (Direction)(((int)_agent.Direction + 1) % 4);
                break;
            case AgentAction.MoveForward:
                var (deltaX, deltaY) = _agent.Direction switch
                {
                    Direction.North => (0, -1),
                    Direction.East => (1, 0),
                    Direction.South => (0, 1),
                    Direction.West => (-1, 0),
                    _ => throw new ArgumentOutOfRangeException()
                };

                _agent.X = Wrap(_agent.X + deltaX, Width);
                _agent.Y = Wrap(_agent.Y + deltaY, Height);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown agent action.");
        }
    }

    private WorldSnapshot CreateSnapshot() =>
        new(
            Width,
            Height,
            Tick,
            new AgentSnapshot(
                _agent.X,
                _agent.Y,
                _agent.Direction,
                _agent.LastAction,
                _agent.InstructionsExecuted,
                _agent.ReachedInstructionLimit,
                _agent.Program.Source));

    private static int Wrap(int value, int size)
    {
        var remainder = value % size;
        return remainder < 0 ? remainder + size : remainder;
    }

    private sealed class Agent(int x, int y, Direction direction, PushProgram program)
    {
        public int X { get; set; } = x;

        public int Y { get; set; } = y;

        public Direction Direction { get; set; } = direction;

        public PushProgram Program { get; } = program;

        public PushInterpreter Interpreter { get; } = new();

        public AgentAction LastAction { get; set; }

        public int InstructionsExecuted { get; set; }

        public bool ReachedInstructionLimit { get; set; }
    }
}
