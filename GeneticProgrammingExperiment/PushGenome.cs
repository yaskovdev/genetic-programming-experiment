namespace GeneticProgrammingExperiment;

public sealed class PushGenome
{
    public const int DefaultMaximumPoints = 64;

    private static readonly int[] IntegerLiterals = [-1, 0, 1, 2, 4, 8, 16, 32, 50, 100];
    private static readonly PushInstruction[] Instructions = Enum.GetValues<PushInstruction>();
    private static readonly PushInstruction[] Actions =
        [PushInstruction.ActionMoveForward, PushInstruction.ActionTurnRight, PushInstruction.ActionEat];

    private readonly PushCodeBlock _root;

    private PushGenome(PushCodeBlock root)
    {
        _root = root;
        PointCount = CountPoints(root);
    }

    public int PointCount { get; }

    public PushProgram Develop() => PushProgram.FromRoot(CloneBlock(_root));

    public static PushGenome FromProgram(PushProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);
        return new PushGenome(CloneBlock(program.Root));
    }

    public static PushGenome CreateRandom(int seed, int maximumPoints = DefaultMaximumPoints)
    {
        ValidateMaximumPoints(maximumPoints);
        return CreateRandom(new DeterministicRandom(unchecked((ulong)seed)), maximumPoints);
    }

    public PushGenome Mutate(int seed, int maximumPoints = DefaultMaximumPoints)
    {
        ValidateMaximumPoints(maximumPoints);
        return Mutate(new DeterministicRandom(unchecked((ulong)seed)), maximumPoints);
    }

    internal static PushGenome CreateRandom(DeterministicRandom random, int maximumPoints)
    {
        for (var attempt = 0; attempt < 32; attempt++)
        {
            var root = MutableNode.Block();
            var elementCount = 1 + random.NextInt(Math.Min(8, maximumPoints - 1));
            for (var index = 0; index < elementCount; index++)
            {
                root.Children!.Add(CreateRandomNode(random, 0));
            }

            EnsureAction(root, random);
            var immutableRoot = root.ToBlock();
            if (CountPoints(immutableRoot) <= maximumPoints)
            {
                return new PushGenome(immutableRoot);
            }
        }

        return FromProgram(PushProgram.Parse("(action.move-forward)"));
    }

    internal PushGenome Mutate(DeterministicRandom random, int maximumPoints)
    {
        for (var attempt = 0; attempt < 16; attempt++)
        {
            var root = MutableNode.From(_root);
            ApplyMutation(root, random);

            var immutableRoot = root.ToBlock();
            var pointCount = CountPoints(immutableRoot);
            if (pointCount > 1 && pointCount <= maximumPoints)
            {
                return new PushGenome(immutableRoot);
            }
        }

        return PointCount <= maximumPoints
            ? this
            : CreateRandom(random, maximumPoints);
    }

    private static void ApplyMutation(MutableNode root, DeterministicRandom random)
    {
        switch (random.NextInt(5))
        {
            case 0:
                ReplaceNode(root, random);
                break;
            case 1:
                InsertNode(root, random);
                break;
            case 2:
                DeleteNode(root, random);
                break;
            case 3:
                DuplicateNode(root, random);
                break;
            case 4:
                ChangeAtom(root, random);
                break;
        }
    }

    private static void ReplaceNode(MutableNode root, DeterministicRandom random)
    {
        var locations = GetLocations(root);
        if (locations.Count == 0)
        {
            InsertNode(root, random);
            return;
        }

        var location = locations[random.NextInt(locations.Count)];
        location.Parent.Children![location.Index] = CreateRandomNode(random, location.Depth);
    }

    private static void InsertNode(MutableNode root, DeterministicRandom random)
    {
        var blocks = GetBlocks(root);
        var block = blocks[random.NextInt(blocks.Count)];
        var index = random.NextInt(block.Node.Children!.Count + 1);
        block.Node.Children.Insert(index, CreateRandomNode(random, block.Depth));
    }

    private static void DeleteNode(MutableNode root, DeterministicRandom random)
    {
        var locations = GetLocations(root);
        if (locations.Count == 0)
        {
            return;
        }

        var location = locations[random.NextInt(locations.Count)];
        location.Parent.Children!.RemoveAt(location.Index);
    }

    private static void DuplicateNode(MutableNode root, DeterministicRandom random)
    {
        var locations = GetLocations(root);
        if (locations.Count == 0)
        {
            InsertNode(root, random);
            return;
        }

        var location = locations[random.NextInt(locations.Count)];
        location.Parent.Children!.Insert(location.Index + 1, location.Parent.Children[location.Index].Clone());
    }

    private static void ChangeAtom(MutableNode root, DeterministicRandom random)
    {
        var atoms = GetAtoms(root);
        if (atoms.Count == 0)
        {
            InsertNode(root, random);
            return;
        }

        var atom = atoms[random.NextInt(atoms.Count)];
        if (atom.Kind == MutableNodeKind.Integer)
        {
            var replacement = IntegerLiterals[random.NextInt(IntegerLiterals.Length)];
            atom.Integer = replacement == atom.Integer ? atom.Integer + 1 : replacement;
        }
        else
        {
            var replacement = Instructions[random.NextInt(Instructions.Length)];
            if (replacement == atom.Instruction)
            {
                replacement = Instructions[((int)replacement + 1) % Instructions.Length];
            }

            atom.Instruction = replacement;
        }
    }

    private static MutableNode CreateRandomNode(DeterministicRandom random, int depth)
    {
        if (depth < 3 && random.NextChance(18))
        {
            var block = MutableNode.Block();
            var childCount = 1 + random.NextInt(3);
            for (var index = 0; index < childCount; index++)
            {
                block.Children!.Add(CreateRandomNode(random, depth + 1));
            }

            return block;
        }

        if (random.NextChance(25))
        {
            return MutableNode.IntegerLiteral(IntegerLiterals[random.NextInt(IntegerLiterals.Length)]);
        }

        return MutableNode.InstructionAtom(Instructions[random.NextInt(Instructions.Length)]);
    }

    private static void EnsureAction(MutableNode root, DeterministicRandom random)
    {
        if (GetAtoms(root).Any(node => node.Kind == MutableNodeKind.Instruction && Actions.Contains(node.Instruction)))
        {
            return;
        }

        root.Children!.Add(MutableNode.InstructionAtom(Actions[random.NextInt(Actions.Length)]));
    }

    private static List<NodeLocation> GetLocations(MutableNode root)
    {
        var result = new List<NodeLocation>();
        AddLocations(root, 0, result);
        return result;
    }

    private static void AddLocations(MutableNode block, int depth, List<NodeLocation> result)
    {
        for (var index = 0; index < block.Children!.Count; index++)
        {
            var child = block.Children[index];
            result.Add(new NodeLocation(block, index, depth));
            if (child.Kind == MutableNodeKind.Block)
            {
                AddLocations(child, depth + 1, result);
            }
        }
    }

    private static List<BlockLocation> GetBlocks(MutableNode root)
    {
        var result = new List<BlockLocation>();
        AddBlocks(root, 0, result);
        return result;
    }

    private static void AddBlocks(MutableNode block, int depth, List<BlockLocation> result)
    {
        result.Add(new BlockLocation(block, depth));
        foreach (var child in block.Children!)
        {
            if (child.Kind == MutableNodeKind.Block)
            {
                AddBlocks(child, depth + 1, result);
            }
        }
    }

    private static List<MutableNode> GetAtoms(MutableNode root)
    {
        var result = new List<MutableNode>();
        AddAtoms(root, result);
        return result;
    }

    private static void AddAtoms(MutableNode node, List<MutableNode> result)
    {
        if (node.Kind != MutableNodeKind.Block)
        {
            result.Add(node);
            return;
        }

        foreach (var child in node.Children!)
        {
            AddAtoms(child, result);
        }
    }

    private static int CountPoints(PushElement element) =>
        element is PushCodeBlock block
            ? 1 + block.Elements.Sum(CountPoints)
            : 1;

    private static PushCodeBlock CloneBlock(PushCodeBlock block) =>
        new(block.Elements.Select(CloneElement).ToArray());

    private static PushElement CloneElement(PushElement element) =>
        element switch
        {
            PushCodeBlock block => CloneBlock(block),
            PushInteger integer => new PushInteger(integer.Value),
            PushInstructionElement instruction => new PushInstructionElement(instruction.Instruction),
            _ => throw new ArgumentOutOfRangeException(nameof(element), element, "Unknown Push element.")
        };

    private static void ValidateMaximumPoints(int maximumPoints)
    {
        if (maximumPoints < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumPoints), "A genome must allow at least two points.");
        }
    }

    private readonly record struct NodeLocation(MutableNode Parent, int Index, int Depth);

    private readonly record struct BlockLocation(MutableNode Node, int Depth);

    private enum MutableNodeKind
    {
        Block,
        Integer,
        Instruction
    }

    private sealed class MutableNode
    {
        private MutableNode(MutableNodeKind kind)
        {
            Kind = kind;
        }

        public MutableNodeKind Kind { get; }

        public List<MutableNode>? Children { get; private init; }

        public int Integer { get; set; }

        public PushInstruction Instruction { get; set; }

        public static MutableNode Block() =>
            new(MutableNodeKind.Block)
            {
                Children = []
            };

        public static MutableNode IntegerLiteral(int value) =>
            new(MutableNodeKind.Integer)
            {
                Integer = value
            };

        public static MutableNode InstructionAtom(PushInstruction instruction) =>
            new(MutableNodeKind.Instruction)
            {
                Instruction = instruction
            };

        public static MutableNode From(PushElement element) =>
            element switch
            {
                PushCodeBlock block => new MutableNode(MutableNodeKind.Block)
                {
                    Children = block.Elements.Select(From).ToList()
                },
                PushInteger integer => IntegerLiteral(integer.Value),
                PushInstructionElement instruction => InstructionAtom(instruction.Instruction),
                _ => throw new ArgumentOutOfRangeException(nameof(element), element, "Unknown Push element.")
            };

        public MutableNode Clone() =>
            Kind switch
            {
                MutableNodeKind.Block => new MutableNode(MutableNodeKind.Block)
                {
                    Children = Children!.Select(child => child.Clone()).ToList()
                },
                MutableNodeKind.Integer => IntegerLiteral(Integer),
                MutableNodeKind.Instruction => InstructionAtom(Instruction),
                _ => throw new ArgumentOutOfRangeException()
            };

        public PushCodeBlock ToBlock()
        {
            if (Kind != MutableNodeKind.Block)
            {
                throw new InvalidOperationException("Only a block node can become a Push code block.");
            }

            return new PushCodeBlock(Children!.Select(child => child.ToElement()).ToArray());
        }

        private PushElement ToElement() =>
            Kind switch
            {
                MutableNodeKind.Block => ToBlock(),
                MutableNodeKind.Integer => new PushInteger(Integer),
                MutableNodeKind.Instruction => new PushInstructionElement(Instruction),
                _ => throw new ArgumentOutOfRangeException()
            };
    }
}
