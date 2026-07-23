namespace GeneticProgrammingExperiment;

public sealed record WorldSnapshot(int Width, int Height, long Tick, AgentSnapshot Agent);

public sealed record AgentSnapshot(
    int X,
    int Y,
    Direction Direction,
    AgentAction LastAction,
    int InstructionsExecuted,
    bool ReachedInstructionLimit,
    string Program);
