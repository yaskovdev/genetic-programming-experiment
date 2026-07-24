namespace GeneticProgrammingExperiment.Viewer;

using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using global::GeneticProgrammingExperiment;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer;
    private PushProgram _activeProgram = PushProgram.Parse(PushProgram.Empty);
    private World _world = null!;
    private CancellationTokenSource? _evolutionCancellation;
    private bool _isEvolving;
    private bool _isRunning;

    public MainWindow()
    {
        InitializeComponent();

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(140)
        };
        _timer.Tick += OnTimerTick;

        Loaded += OnLoaded;
        Closed += OnClosed;
        ResetWorld();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SetRunning(true);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _timer.Stop();
        _evolutionCancellation?.Cancel();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        Advance();
    }

    private void OnPlayPauseClick(object sender, RoutedEventArgs e)
    {
        SetRunning(!_isRunning);
    }

    private void OnStepClick(object sender, RoutedEventArgs e)
    {
        SetRunning(false);
        Advance();
    }

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        ResetWorld();
    }

    private async void OnEvolveClick(object sender, RoutedEventArgs e)
    {
        if (_isEvolving)
        {
            return;
        }

        SetRunning(false);
        _isEvolving = true;
        SetControlsEnabled(false);
        EvolutionStatusTextBlock.Text = "Starting evolution";

        var cancellation = new CancellationTokenSource();
        _evolutionCancellation = cancellation;
        var progress = new Progress<EvolutionProgress>(DisplayEvolutionProgress);
        var evolutionCompleted = false;
        try
        {
            var engine = new EvolutionEngine();
            var result = await Task.Run(() => engine.Run(progress: progress, cancellationToken: cancellation.Token));
            _activeProgram = result.BestProgram;
            EvolutionStatusTextBlock.Text =
                $"Generation {result.Generation}   Fitness {result.Fitness:F0}   Food {result.AverageFoodEaten:F1}";
            ResetWorld();
            evolutionCompleted = true;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            EvolutionStatusTextBlock.Text = "Evolution cancelled";
        }
        finally
        {
            if (ReferenceEquals(_evolutionCancellation, cancellation))
            {
                _evolutionCancellation = null;
            }

            cancellation.Dispose();
            _isEvolving = false;
            SetControlsEnabled(true);
            if (evolutionCompleted)
            {
                SetRunning(true);
            }
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (_isEvolving)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Space:
                SetRunning(!_isRunning);
                e.Handled = true;
                break;
            case Key.Right:
                SetRunning(false);
                Advance();
                e.Handled = true;
                break;
            case Key.R:
                ResetWorld();
                e.Handled = true;
                break;
            case Key.E:
                OnEvolveClick(sender, e);
                e.Handled = true;
                break;
        }
    }

    private void Advance()
    {
        Display(_world.Step());
    }

    private void ResetWorld()
    {
        _world = World.CreateDefault(_activeProgram);
        var snapshot = _world.Snapshot;
        WorldViewControl.Reset(snapshot);
        DisplayStatus(snapshot);
        ProgramTextBox.Text = snapshot.Agent.Program;
    }

    private void Display(WorldSnapshot snapshot)
    {
        WorldViewControl.Display(snapshot);
        DisplayStatus(snapshot);
        if (!snapshot.Agent.IsAlive)
        {
            SetRunning(false);
        }
    }

    private void DisplayStatus(WorldSnapshot snapshot)
    {
        var action = snapshot.Agent.LastAction switch
        {
            AgentAction.None => "none",
            AgentAction.MoveForward => "move",
            AgentAction.TurnRight => "turn right",
            AgentAction.Eat => "eat",
            _ => throw new ArgumentOutOfRangeException()
        };

        var life = snapshot.Agent.IsAlive ? $"Energy {snapshot.Agent.Energy,3}/{snapshot.Agent.MaximumEnergy}" : "DEAD";
        StatusTextBlock.Text = $"Tick {snapshot.Tick,5}   {life}   Eaten {snapshot.Agent.FoodEaten,3}   Position ({snapshot.Agent.X,2}, {snapshot.Agent.Y,2})   {action}";
        EnergyProgressBar.Maximum = snapshot.Agent.MaximumEnergy;
        EnergyProgressBar.Value = snapshot.Agent.Energy;
    }

    private void DisplayEvolutionProgress(EvolutionProgress progress)
    {
        EvolutionStatusTextBlock.Text =
            $"Evolving {progress.Generation}/{progress.TotalGenerations}   Best {progress.BestFitness:F0}   Food {progress.BestAverageFoodEaten:F1}";
        EnergyProgressBar.Maximum = progress.TotalGenerations;
        EnergyProgressBar.Value = progress.Generation;
    }

    private void SetRunning(bool isRunning)
    {
        if (isRunning && (_isEvolving || !_world.Snapshot.Agent.IsAlive))
        {
            isRunning = false;
        }

        _isRunning = isRunning;
        PlayPauseButton.Content = isRunning ? "Pause" : "Play";

        if (isRunning)
        {
            _timer.Start();
        }
        else
        {
            _timer.Stop();
        }
    }

    private void SetControlsEnabled(bool isEnabled)
    {
        PlayPauseButton.IsEnabled = isEnabled;
        StepButton.IsEnabled = isEnabled;
        ResetButton.IsEnabled = isEnabled;
        EvolveButton.IsEnabled = isEnabled;
    }
}
