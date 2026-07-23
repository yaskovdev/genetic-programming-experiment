# Genetic Programming Experiment

An artificial-life experiment in which spatially embodied agents are controlled by evolvable Push programs.

The long-term goal is to explore genomes that can evolve not only behavior, but also an agent's structure, sensors, actuators, and internal organization. The first goal is deliberately smaller: create an ecology in which Push-controlled agents live, reproduce, mutate, and compete without an externally assigned fitness function.

## Current Prototype

The first vertical slice is implemented in C# and .NET 10. It contains:

- A deterministic 24 by 16 toroidal grid
- One agent with a direction and position
- A small bounded Push-style interpreter
- A WPF viewer with play, pause, single-step, reset, and a visible movement trail
- Tests for the Push conditional, square movement, and toroidal wrapping

The agent is controlled by this immutable Push program:

```push
(sensor.tick 8 integer.% 0 integer.= exec.if (action.turn-right) (action.move-forward))
```

Every eighth tick it turns right. On all other ticks it moves forward, so it repeatedly walks a square.

Run the viewer from the repository root:

```powershell
dotnet run --project .\GeneticProgrammingExperiment.Viewer
```

Controls:

- `Space`: play or pause
- `Right Arrow`: execute one tick
- `R`: reset the world

The solution keeps the simulator independent of WPF:

| Project | Responsibility |
| --- | --- |
| `GeneticProgrammingExperiment` | World, agent state, Push parser, and interpreter |
| `GeneticProgrammingExperiment.Viewer` | Interactive WPF visualization |
| `GeneticProgrammingExperiment.Tests` | Deterministic behavior tests |

There is no evolution, energy, food, or reproduction yet. This prototype establishes the execution and visualization path on which those systems can be added.

## Motivation

The earlier [Robocode experiment](https://github.com/yaskovdev/genetic-programming-robocode) evolved a Push program that reliably defeated a difficult opponent. That experiment demonstrated that genetic programming could discover an effective controller, but its evolutionary objective was fixed: maximize battle performance against one robot.

This project asks a different question:

> What can evolve when agents must persist in a shared world and reproductive success, rather than a score chosen by the experimenter, determines fitness?

A more elaborate genome alone is unlikely to produce more interesting evolution. Interesting behavior also needs an ecology that creates changing opportunities, competition, trade-offs, and niches. For that reason, this project will begin with evolvable Push programs in a minimal ecology. Artificial DNA and evolved bodies will be introduced only after the basic evolutionary system works.

## Core Principles

1. **Ecology before genome complexity.** Begin with a simple, mutation-tolerant controller and make the world capable of producing meaningful selection pressures.
2. **Endogenous reproduction.** Agents reproduce inside the simulation. There are no externally managed generations.
3. **No explicit fitness function.** Fitness is the number of viable descendants an agent leaves in the evolving population.
4. **Local interaction.** Agents sense, consume resources, compete, and reproduce locally. Spatial structure should permit niches and preserve diversity.
5. **Heritable variation.** Mutation occurs when an agent reproduces. Insertions, deletions, substitutions, and segment duplications allow both refinement and structural growth.
6. **Bounded computation.** Every activation has a finite instruction budget. A malformed or unproductive program remains safe to execute.
7. **Reproducibility.** A run is fully determined by its configuration and random seed. Important runs can be replayed from snapshots.
8. **Incremental complexity.** New ecological and genetic mechanisms are added one at a time so their effects can be measured.

## First Experiment

### World

The first world will be a headless, discrete, two-dimensional toroidal grid.

Each cell may contain:

- A renewable quantity of food
- At most one agent
- Additional terrain or chemical state in later experiments

Time advances in ticks. Agent activation order is shuffled using the run's seeded random-number generator, which avoids a permanent first-mover advantage while preserving reproducibility.

Food grows in spatial patches rather than appearing uniformly. Patchiness creates locations worth finding, occupying, and defending. Resource growth and carrying capacity must be configurable because they determine whether the world favors exploration, competition, or population collapse.

The simulator and visualizer should be separate. The simulation must be able to run quickly without rendering, while saved snapshots can be inspected by a simple viewer.

### Agents

The initial agent has:

- A position and heading
- Energy
- Age
- A heritable genome
- A Push interpreter with persistent data stacks
- A lineage identifier

Agents pay a small metabolic cost each tick. Movement and reproduction have additional costs. Death occurs when energy reaches zero or a configurable maximum age is reached.

The agent's body and available sensors are fixed during the first experiment. Only its controller evolves.

### Controller Cycle

During each activation:

1. The world places sensor values on the appropriate Push input stacks.
2. The agent's Push program executes with a fixed instruction limit.
3. The first successful action instruction ends the activation.
4. If no action is produced, the agent does nothing.
5. Data stacks persist between activations, providing memory. Execution state is reset so the genome begins from a known entry point each time.

The initial sensors should remain small and physical:

- Own energy
- Food on the current cell
- Food ahead and to either side
- Whether the cell ahead is occupied
- Whether nearby cells are available for reproduction
- The result of the previous action

The initial actions are:

- Turn left
- Turn right
- Move forward
- Eat
- Reproduce
- Do nothing

These are low-level capabilities, not strategies. Concepts such as foraging, guarding a resource patch, following another agent, or alternating between exploration and feeding must emerge from combinations of instructions and memory.

### Reproduction

Reproduction is requested by the controller and completed by the world only when:

- The parent has enough energy
- A neighboring cell is empty
- The reproduction action can pay its configured cost

The parent's energy is divided between parent and child. The child receives a mutated copy of the parent's genome, a new lineage record, and empty Push stacks.

The world does not choose parents, preserve elites, run tournaments, or replace populations. An agent's genome remains represented only while its descendants remain alive.

### Genome

The first genome should be a linear, Plushy-style representation that develops into a valid Push program. This retains Push's tolerance of arbitrary code while making mutation and segment duplication straightforward.

Initial mutation operators:

- Instruction substitution
- Instruction insertion
- Instruction deletion
- Segment duplication
- Segment deletion

Mutation rates and genome-size limits must be configurable. Genome copying can eventually consume energy proportional to genome length, but that cost should be introduced only after a viable ecology has been established.

The initial population should descend from a small, hand-written, barely viable ancestor rather than a population of completely random programs. Starting with a replicator separates the study of subsequent evolution from the much harder problem of spontaneously discovering reproduction.

## What To Measure

Measurements must observe evolution without affecting selection.

Each run should record:

- Population size, births, and deaths
- Total food and total stored agent energy
- Genome lengths and mutation events
- Parent-child relationships and lineage lifetimes
- Genotypic diversity
- Spatial distribution of lineages
- Frequency and success rate of each action
- Periodic world and agent snapshots

The first sign of success is not a champion agent. It is sustained evolutionary dynamics such as:

- Multiple lineages coexisting for substantial periods
- Strategies changing after environmental or ecological changes
- Heritable behaviors spreading through the population
- New variants creating or occupying distinct niches
- Long-lived innovations appearing after the initial adaptation period

Every experiment should use several random seeds. A striking result from one run may be an accident, while repeated population-level patterns are evidence.

## Roadmap

### Phase 1: Deterministic Simulation

- Implement the grid, food dynamics, agent state, and tick scheduler.
- Add seeded randomness, configuration files, snapshots, and replay.
- Keep controllers fixed while validating energy conservation and world rules.
- Add a minimal non-evolving controller that can survive under generous conditions.

### Phase 2: Push-Controlled Life

- Give each agent its own Push interpreter state.
- Define sensor inputs and safe action instructions.
- Add the instruction budget and action protocol.
- Create a minimal viable Push ancestor.
- Confirm that the ancestor can maintain a population without mutation.

### Phase 3: Evolution

- Add genome inheritance and mutation at reproduction.
- Track ancestry, mutations, and population diversity.
- Run replicated long-duration experiments.
- Compare spatial and well-mixed populations.
- Compare mutation operators, especially runs with and without segment duplication.

### Phase 4: Richer Ecology

Add one mechanism per experiment, not all at once:

- Several resource types with incompatible nutritional strategies
- Resource seasons or moving resource patches
- Predation or energy stealing
- Energy sharing
- Pheromones or short local messages
- Waste, toxins, or decomposing bodies
- Terrain modification and simple niche construction

Each addition should create a trade-off or a new interaction, rather than merely adding another score-producing task.

### Phase 5: Event-Driven Controllers

If repeatedly running one Push entry point becomes limiting, evolve tagged modules that respond to events such as:

- Food detected
- Movement blocked
- Message received
- Energy threshold crossed
- Reproduction succeeded or failed
- Damage received

This can follow ideas from SignalGP while retaining Push instructions inside each module.

### Phase 6: Artificial DNA and Development

Only after behavioral evolution and ecological dynamics are established, replace the fixed body with a developmental genome.

A possible artificial gene contains:

```text
tag + activation condition + product + parameters
```

Gene products may:

- Create a body part or internal component
- Add a sensor or actuator
- Connect components
- Express a tagged Push module
- Emit or respond to a regulatory signal
- Change the activation of other genes

Development starts from one core cell and interprets the genome to construct a valid body and controller. Gene duplication, deletion, and regulatory mutations can then alter morphology and behavior together.

The first developmental system should use a small set of components and enforce physical costs. Larger bodies require more material and maintenance. Sensors, actuators, computation, and reproduction all consume energy. Without such trade-offs, evolution has little reason to discover meaningful organization.

## Questions For Early Experiments

- Does spatially local reproduction preserve more diversity than global placement?
- Does patchy food produce specialized movement or territorial behavior?
- Does persistent Push state improve survival compared with stateless execution?
- Do segment duplications lead to useful increases in genome length?
- What computation cost prevents waste without suppressing complex control?
- Can different strategies coexist without introducing explicit species labels?
- Do ecological changes trigger adaptation from standing diversity or population replacement?

## Explicit Non-Goals For The First Version

- Evolving morphology
- Sexual reproduction
- Neural controllers
- Complex chemistry
- A large instruction set
- High-fidelity graphics
- Distributed or GPU execution
- Optimizing a manually designed fitness score

These may become useful later, but none is required to discover whether the basic ecology supports sustained evolution.

## Suggested Architecture

The implementation should keep the following concerns independent:

| Component | Responsibility |
| --- | --- |
| World | Grid state, resources, time, and action resolution |
| Agent | Physical state, energy, age, heading, and controller |
| Controller | Sensor input, bounded execution, memory, and action output |
| Genome | Heritable representation and development into a controller |
| Mutation | Configurable mutation operators used during reproduction |
| Lineage tracker | Parent-child history and inherited changes |
| Experiment runner | Configuration, seeds, repetitions, and checkpoints |
| Telemetry | Metrics and event recording that do not affect the world |
| Viewer | Read-only visualization of live or saved states |

The controller and genome interfaces should not depend on Push. This permits later comparisons with SignalGP, neural controllers, or developmental genomes without rewriting the ecology.

## References

- [Push, PushGP, and Pushpop](http://faculty.hampshire.edu/lspector/push.html)
- [Comparison of Linear Genome Representations for Software Synthesis](http://faculty.hampshire.edu/lspector/pubs/GPTP_2019_Plush_Plushy_preprint.pdf)
- [Avida digital evolution platform](https://avida.devosoft.org/)
- [SignalGP documentation](https://empirical.readthedocs.io/en/latest/tutorials/SignalGP.html)
- [DISHTINY](https://github.com/mmore500/dishtiny)
- [An Overview of Open-Ended Evolution](https://arxiv.org/abs/1909.04430)
