namespace GeneticProgrammingExperiment.Viewer;

using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using global::GeneticProgrammingExperiment;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer;
    private World _world = null!;
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

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
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
        }
    }

    private void Advance()
    {
        Display(_world.Step());
    }

    private void ResetWorld()
    {
        _world = World.CreateDefault();
        var snapshot = _world.Snapshot;
        WorldViewControl.Reset(snapshot);
        DisplayStatus(snapshot);
        ProgramTextBox.Text = snapshot.Agent.Program;
    }

    private void Display(WorldSnapshot snapshot)
    {
        WorldViewControl.Display(snapshot);
        DisplayStatus(snapshot);
    }

    private void DisplayStatus(WorldSnapshot snapshot)
    {
        var action = snapshot.Agent.LastAction switch
        {
            AgentAction.None => "none",
            AgentAction.MoveForward => "move",
            AgentAction.TurnRight => "turn right",
            _ => throw new ArgumentOutOfRangeException()
        };

        StatusTextBlock.Text =
            $"Tick {snapshot.Tick,5}   Position ({snapshot.Agent.X,2}, {snapshot.Agent.Y,2})   Heading {snapshot.Agent.Direction,-5}   Action {action}";
    }

    private void SetRunning(bool isRunning)
    {
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
}