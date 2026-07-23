namespace GeneticProgrammingExperiment;

public sealed class PushInterpreter
{
    private const int DefaultIntegerStackCapacity = 64;
    private const int DefaultBooleanStackCapacity = 64;
    private const int DefaultExecutionStackCapacity = 256;

    private readonly FixedStack<int> _integers = new(DefaultIntegerStackCapacity);
    private readonly FixedStack<bool> _booleans = new(DefaultBooleanStackCapacity);
    private readonly FixedStack<PushElement> _execution = new(DefaultExecutionStackCapacity);

    public PushExecutionResult Execute(PushProgram program, PushSensors sensors, int instructionLimit)
    {
        ArgumentNullException.ThrowIfNull(program);
        if (instructionLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(instructionLimit), "The instruction limit must be positive.");
        }

        _execution.Clear();
        _execution.TryPush(program.Root);

        var action = AgentAction.None;
        var instructionsExecuted = 0;
        while (_execution.Count > 0 && instructionsExecuted < instructionLimit && action == AgentAction.None)
        {
            var element = _execution.Pop();
            instructionsExecuted++;

            switch (element)
            {
                case PushInteger integer:
                    _integers.TryPush(integer.Value);
                    break;
                case PushCodeBlock block:
                    Expand(block);
                    break;
                case PushInstructionElement instruction:
                    action = Execute(instruction.Instruction, sensors);
                    break;
            }
        }

        var reachedInstructionLimit = action == AgentAction.None && _execution.Count > 0;
        return new PushExecutionResult(action, instructionsExecuted, reachedInstructionLimit);
    }

    private AgentAction Execute(PushInstruction instruction, PushSensors sensors)
    {
        switch (instruction)
        {
            case PushInstruction.SensorTick:
                _integers.TryPush(unchecked((int)sensors.Tick));
                break;
            case PushInstruction.SensorFoodHere:
                _integers.TryPush(sensors.FoodHere);
                break;
            case PushInstruction.SensorFoodAhead:
                _integers.TryPush(sensors.FoodAhead);
                break;
            case PushInstruction.SensorEnergy:
                _integers.TryPush(sensors.Energy);
                break;
            case PushInstruction.IntegerModulo:
                IntegerModulo();
                break;
            case PushInstruction.IntegerEquals:
                IntegerEquals();
                break;
            case PushInstruction.IntegerGreaterThan:
                IntegerGreaterThan();
                break;
            case PushInstruction.ExecIf:
                ExecIf();
                break;
            case PushInstruction.ActionTurnRight:
                return AgentAction.TurnRight;
            case PushInstruction.ActionMoveForward:
                return AgentAction.MoveForward;
            case PushInstruction.ActionEat:
                return AgentAction.Eat;
            default:
                throw new ArgumentOutOfRangeException(nameof(instruction), instruction, "Unknown Push instruction.");
        }

        return AgentAction.None;
    }

    private void Expand(PushCodeBlock block)
    {
        if (_execution.RemainingCapacity < block.Elements.Length)
        {
            return;
        }

        for (var index = block.Elements.Length - 1; index >= 0; index--)
        {
            _execution.TryPush(block.Elements[index]);
        }
    }

    private void IntegerModulo()
    {
        if (_integers.Count < 2 || _integers.Peek() == 0)
        {
            return;
        }

        var divisor = _integers.Pop();
        var dividend = _integers.Pop();
        var result = dividend == int.MinValue && divisor == -1 ? 0 : dividend % divisor;
        _integers.TryPush(result);
    }

    private void IntegerEquals()
    {
        if (_integers.Count < 2)
        {
            return;
        }

        var right = _integers.Pop();
        var left = _integers.Pop();
        _booleans.TryPush(left == right);
    }

    private void IntegerGreaterThan()
    {
        if (_integers.Count < 2)
        {
            return;
        }

        var right = _integers.Pop();
        var left = _integers.Pop();
        _booleans.TryPush(left > right);
    }

    private void ExecIf()
    {
        if (_booleans.Count == 0 || _execution.Count < 2)
        {
            return;
        }

        var condition = _booleans.Pop();
        var whenTrue = _execution.Pop();
        var whenFalse = _execution.Pop();
        _execution.TryPush(condition ? whenTrue : whenFalse);
    }

    private sealed class FixedStack<T>(int capacity)
    {
        private readonly T[] _items = new T[capacity];

        public int Count { get; private set; }

        public int RemainingCapacity => _items.Length - Count;

        public bool TryPush(T item)
        {
            if (Count == _items.Length)
            {
                return false;
            }

            _items[Count++] = item;
            return true;
        }

        public T Pop()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("The stack is empty.");
            }

            var index = --Count;
            var item = _items[index];
            _items[index] = default!;
            return item;
        }

        public T Peek()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("The stack is empty.");
            }

            return _items[Count - 1];
        }

        public void Clear()
        {
            Array.Clear(_items, 0, Count);
            Count = 0;
        }
    }
}

public readonly record struct PushExecutionResult(AgentAction Action, int InstructionsExecuted, bool ReachedInstructionLimit);
