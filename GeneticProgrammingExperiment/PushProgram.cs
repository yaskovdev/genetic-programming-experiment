namespace GeneticProgrammingExperiment;

using System.Globalization;

public sealed class PushProgram
{
    private PushProgram(string source, PushCodeBlock root)
    {
        Source = source;
        Root = root;
    }

    public string Source { get; }

    internal PushCodeBlock Root { get; }

    public static PushProgram Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var normalizedSource = source.Trim();
        if (normalizedSource.Length == 0)
        {
            throw new FormatException("A Push program cannot be empty.");
        }

        var parser = new Parser(normalizedSource);
        return new PushProgram(normalizedSource, parser.Parse());
    }

    public override string ToString() => Source;

    private sealed class Parser(string source)
    {
        private int _position;

        public PushCodeBlock Parse()
        {
            SkipWhitespace();
            if (!TryTake('('))
            {
                throw Error("A Push program must begin with '('.");
            }

            var root = ParseBlock();
            SkipWhitespace();
            if (_position != source.Length)
            {
                throw Error("Unexpected content after the root program.");
            }

            return root;
        }

        private PushCodeBlock ParseBlock()
        {
            var elements = new List<PushElement>();
            while (true)
            {
                SkipWhitespace();
                if (_position >= source.Length)
                {
                    throw Error("A closing ')' is missing.");
                }

                if (TryTake(')'))
                {
                    return new PushCodeBlock(elements.ToArray());
                }

                if (TryTake('('))
                {
                    elements.Add(ParseBlock());
                    continue;
                }

                elements.Add(ParseAtom(ReadToken()));
            }
        }

        private PushElement ParseAtom(string token)
        {
            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return new PushInteger(value);
            }

            var instruction = token switch
            {
                "sensor.tick" => PushInstruction.SensorTick,
                "integer.%" => PushInstruction.IntegerModulo,
                "integer.=" => PushInstruction.IntegerEquals,
                "exec.if" => PushInstruction.ExecIf,
                "action.turn-right" => PushInstruction.ActionTurnRight,
                "action.move-forward" => PushInstruction.ActionMoveForward,
                _ => throw Error($"Unknown Push instruction '{token}'.")
            };

            return new PushInstructionElement(instruction);
        }

        private string ReadToken()
        {
            var start = _position;
            while (_position < source.Length && !char.IsWhiteSpace(source[_position]) && source[_position] is not '(' and not ')')
            {
                _position++;
            }

            if (start == _position)
            {
                throw Error("An instruction or literal was expected.");
            }

            return source[start.._position];
        }

        private void SkipWhitespace()
        {
            while (_position < source.Length && char.IsWhiteSpace(source[_position]))
            {
                _position++;
            }
        }

        private bool TryTake(char expected)
        {
            if (_position >= source.Length || source[_position] != expected)
            {
                return false;
            }

            _position++;
            return true;
        }

        private FormatException Error(string message) => new($"{message} Position: {_position}.");
    }
}

internal abstract record PushElement;

internal sealed record PushInteger(int Value) : PushElement;

internal sealed record PushInstructionElement(PushInstruction Instruction) : PushElement;

internal sealed record PushCodeBlock(PushElement[] Elements) : PushElement;

internal enum PushInstruction
{
    SensorTick,
    IntegerModulo,
    IntegerEquals,
    ExecIf,
    ActionTurnRight,
    ActionMoveForward
}
