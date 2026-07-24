namespace GeneticProgrammingExperiment;

public sealed class World
{
    public const int MaximumEnergy = 100;
    public const int DefaultInitialEnergy = 50;
    public const int BasalEnergyCost = 1;
    public const int MovementEnergyCost = 1;
    public const int FoodEnergy = 16;
    public const int MaximumFoodPerCell = 1;
    public const int FoodRegrowthPeriod = 80;

    private const int InstructionLimit = 64;
    private const int FertileBlockPercentage = 55;
    private const int FertileCellPercentage = 60;
    private const int FoodPatchSize = 4;

    private readonly Agent _agent;
    private readonly bool[] _fertile;
    private readonly byte[] _food;
    private readonly uint _foodSeed;
    private readonly int[][] _regrowthSchedule;

    public World(
        int width,
        int height,
        int agentX,
        int agentY,
        Direction direction,
        PushProgram program,
        int initialEnergy = DefaultInitialEnergy,
        IEnumerable<FoodPlacement>? initialFood = null,
        int foodSeed = 0)
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

        if (initialEnergy < 0 || initialEnergy > MaximumEnergy)
        {
            throw new ArgumentOutOfRangeException(nameof(initialEnergy), $"Initial energy must be between 0 and {MaximumEnergy}.");
        }

        ArgumentNullException.ThrowIfNull(program);

        Width = width;
        Height = height;
        _foodSeed = unchecked((uint)foodSeed);
        _fertile = new bool[width * height];
        _food = new byte[width * height];
        _agent = new Agent(agentX, agentY, direction, program, initialEnergy);

        if (initialFood is null)
        {
            PopulateInitialFood();
        }
        else
        {
            PlaceFood(initialFood);
        }

        _regrowthSchedule = BuildRegrowthSchedule();
    }

    public int Width { get; }

    public int Height { get; }

    public long Tick { get; private set; }

    public bool IsAgentAlive => _agent.IsAlive;

    public int AgentEnergy => _agent.Energy;

    public int AgentFoodEaten => _agent.FoodEaten;

    public WorldSnapshot Snapshot => CreateSnapshot();

    public static World CreateDefault() => CreateDefault(PushProgram.Parse(PushProgram.Empty));

    public static World CreateDefault(PushProgram program, int foodSeed = 0) =>
        new(24, 16, 15, 4, Direction.East, program, foodSeed: foodSeed);

    public WorldSnapshot Step()
    {
        Advance();
        return CreateSnapshot();
    }

    public void Advance()
    {
        RegrowFood();

        if (!_agent.IsAlive)
        {
            Tick++;
            return;
        }

        _agent.Energy -= BasalEnergyCost;
        if (_agent.Energy <= 0)
        {
            KillAgent(clearExecutionMetadata: true);
            Tick++;
            return;
        }

        var sensors = CreateSensors();
        var execution = _agent.Interpreter.Execute(_agent.Program, sensors, InstructionLimit);
        Apply(execution.Action);

        _agent.LastAction = execution.Action;
        _agent.InstructionsExecuted = execution.InstructionsExecuted;
        _agent.ReachedInstructionLimit = execution.ReachedInstructionLimit;
        if (_agent.Energy <= 0)
        {
            KillAgent(clearExecutionMetadata: false);
        }
        Tick++;
    }

    private PushSensors CreateSensors()
    {
        var (aheadX, aheadY) = GetAheadPosition();
        return new PushSensors(Tick, FoodAt(_agent.X, _agent.Y), FoodAt(aheadX, aheadY), _agent.Energy);
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
                _agent.Energy -= MovementEnergyCost;
                var (aheadX, aheadY) = GetAheadPosition();
                _agent.X = aheadX;
                _agent.Y = aheadY;
                break;
            case AgentAction.Eat:
                Eat();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown agent action.");
        }
    }

    private void Eat()
    {
        var index = ToIndex(_agent.X, _agent.Y);
        if (_food[index] == 0)
        {
            return;
        }

        _food[index]--;
        _agent.Energy = Math.Min(MaximumEnergy, _agent.Energy + FoodEnergy);
        _agent.FoodEaten++;
    }

    private void KillAgent(bool clearExecutionMetadata)
    {
        _agent.Energy = 0;
        _agent.IsAlive = false;
        if (clearExecutionMetadata)
        {
            _agent.LastAction = AgentAction.None;
            _agent.InstructionsExecuted = 0;
            _agent.ReachedInstructionLimit = false;
        }
    }

    private void PopulateInitialFood()
    {
        var blockColumns = (Width + FoodPatchSize - 1) / FoodPatchSize;
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var index = ToIndex(x, y);
                var blockIndex = y / FoodPatchSize * blockColumns + x / FoodPatchSize;
                var fertileBlock = Hash(Seed((uint)blockIndex, 0x9E3779B9u)) % 100 < FertileBlockPercentage;
                var fertileCell = Hash(Seed((uint)index, 0xC2B2AE35u)) % 100 < FertileCellPercentage;
                if (fertileBlock && fertileCell)
                {
                    _fertile[index] = true;
                    _food[index] = MaximumFoodPerCell;
                }
            }
        }
    }

    private void PlaceFood(IEnumerable<FoodPlacement> placements)
    {
        foreach (var placement in placements)
        {
            if (placement.X < 0 || placement.X >= Width)
            {
                throw new ArgumentOutOfRangeException(nameof(placements), $"Food X coordinate {placement.X} is outside the world.");
            }

            if (placement.Y < 0 || placement.Y >= Height)
            {
                throw new ArgumentOutOfRangeException(nameof(placements), $"Food Y coordinate {placement.Y} is outside the world.");
            }

            if (placement.Amount < 0 || placement.Amount > MaximumFoodPerCell)
            {
                throw new ArgumentOutOfRangeException(nameof(placements), $"Food amount must be between 0 and {MaximumFoodPerCell}.");
            }

            var index = ToIndex(placement.X, placement.Y);
            _fertile[index] = placement.Amount > 0;
            _food[index] = (byte)placement.Amount;
        }
    }

    private void RegrowFood()
    {
        var scheduledCells = _regrowthSchedule[(int)(Tick % FoodRegrowthPeriod)];
        foreach (var index in scheduledCells)
        {
            if (_food[index] == 0)
            {
                _food[index] = MaximumFoodPerCell;
            }
        }
    }

    private int[][] BuildRegrowthSchedule()
    {
        var schedule = new List<int>[FoodRegrowthPeriod];
        for (var phase = 0; phase < schedule.Length; phase++)
        {
            schedule[phase] = [];
        }

        for (var index = 0; index < _fertile.Length; index++)
        {
            if (!_fertile[index])
            {
                continue;
            }

            var offset = (int)(Hash(Seed((uint)index, 0x85EBCA6Bu)) % FoodRegrowthPeriod);
            var phase = (FoodRegrowthPeriod - offset) % FoodRegrowthPeriod;
            schedule[phase].Add(index);
        }

        return schedule.Select(cells => cells.ToArray()).ToArray();
    }

    private WorldSnapshot CreateSnapshot() =>
        new(
            Width,
            Height,
            Tick,
            (byte[])_food.Clone(),
            new AgentSnapshot(
                _agent.X,
                _agent.Y,
                _agent.Direction,
                _agent.Energy,
                MaximumEnergy,
                _agent.IsAlive,
                _agent.FoodEaten,
                _agent.LastAction,
                _agent.InstructionsExecuted,
                _agent.ReachedInstructionLimit,
                _agent.Program.Source));

    private (int X, int Y) GetAheadPosition()
    {
        var (deltaX, deltaY) = _agent.Direction switch
        {
            Direction.North => (0, -1),
            Direction.East => (1, 0),
            Direction.South => (0, 1),
            Direction.West => (-1, 0),
            _ => throw new ArgumentOutOfRangeException()
        };

        return (Wrap(_agent.X + deltaX, Width), Wrap(_agent.Y + deltaY, Height));
    }

    private int FoodAt(int x, int y) => _food[ToIndex(x, y)];

    private int ToIndex(int x, int y) => y * Width + x;

    private static int Wrap(int value, int size)
    {
        var remainder = value % size;
        return remainder < 0 ? remainder + size : remainder;
    }

    private static uint Hash(uint value)
    {
        value ^= value >> 16;
        value *= 0x7FEB352Du;
        value ^= value >> 15;
        value *= 0x846CA68Bu;
        value ^= value >> 16;
        return value;
    }

    private uint Seed(uint value, uint salt) =>
        value + salt + _foodSeed * 0x27D4EB2Du;

    private sealed class Agent(int x, int y, Direction direction, PushProgram program, int energy)
    {
        public int X { get; set; } = x;

        public int Y { get; set; } = y;

        public Direction Direction { get; set; } = direction;

        public int Energy { get; set; } = energy;

        public bool IsAlive { get; set; } = energy > 0;

        public int FoodEaten { get; set; }

        public PushProgram Program { get; } = program;

        public PushInterpreter Interpreter { get; } = new();

        public AgentAction LastAction { get; set; }

        public int InstructionsExecuted { get; set; }

        public bool ReachedInstructionLimit { get; set; }
    }
}
