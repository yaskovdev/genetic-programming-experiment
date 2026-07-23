namespace GeneticProgrammingExperiment.Viewer;

using System.Windows;
using System.Windows.Media;
using global::GeneticProgrammingExperiment;

public sealed class WorldView : FrameworkElement
{
    private const double BoardPadding = 24;
    private const int MaximumTrailPoints = 512;

    private static readonly Brush BoardBrush = CreateBrush(24, 30, 40);
    private static readonly Brush AgentCellBrush = CreateBrush(40, 72, 92);
    private static readonly Brush AgentBrush = CreateBrush(255, 177, 74);
    private static readonly Brush DeadAgentBrush = CreateBrush(151, 76, 76);
    private static readonly Brush FoodBrush = CreateBrush(80, 211, 124);
    private static readonly Brush FoodGlowBrush = CreateBrush(38, 91, 58);
    private static readonly Pen BorderPen = CreatePen(62, 75, 96, 1.5);
    private static readonly Pen GridPen = CreatePen(47, 58, 75, 1);
    private static readonly Pen TrailPen = CreatePen(79, 175, 255, 4);

    private readonly List<Point> _trail = [];
    private WorldSnapshot? _snapshot;

    public WorldView()
    {
        SnapsToDevicePixels = true;
        Focusable = false;
    }

    public void Reset(WorldSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _trail.Clear();
        _snapshot = null;
        Display(snapshot);
    }

    public void Display(WorldSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (_snapshot is null || _snapshot.Agent.X != snapshot.Agent.X || _snapshot.Agent.Y != snapshot.Agent.Y)
        {
            _trail.Add(new Point(snapshot.Agent.X, snapshot.Agent.Y));
            if (_trail.Count > MaximumTrailPoints)
            {
                _trail.RemoveAt(0);
            }
        }

        _snapshot = snapshot;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (_snapshot is null)
        {
            return;
        }

        var availableWidth = Math.Max(0, ActualWidth - BoardPadding * 2);
        var availableHeight = Math.Max(0, ActualHeight - BoardPadding * 2);
        var cellSize = Math.Min(availableWidth / _snapshot.Width, availableHeight / _snapshot.Height);
        if (cellSize <= 0)
        {
            return;
        }

        var boardWidth = cellSize * _snapshot.Width;
        var boardHeight = cellSize * _snapshot.Height;
        var left = (ActualWidth - boardWidth) / 2;
        var top = (ActualHeight - boardHeight) / 2;
        var board = new Rect(left, top, boardWidth, boardHeight);

        drawingContext.DrawRoundedRectangle(BoardBrush, BorderPen, board, 5, 5);
        DrawFood(drawingContext, left, top, cellSize);
        DrawGrid(drawingContext, board, cellSize);
        DrawTrail(drawingContext, left, top, cellSize);
        DrawAgent(drawingContext, left, top, cellSize, _snapshot.Agent);
    }

    private void DrawFood(DrawingContext drawingContext, double left, double top, double cellSize)
    {
        for (var y = 0; y < _snapshot!.Height; y++)
        {
            for (var x = 0; x < _snapshot.Width; x++)
            {
                if (_snapshot.FoodAt(x, y) == 0)
                {
                    continue;
                }

                var center = new Point(left + (x + 0.5) * cellSize, top + (y + 0.5) * cellSize);
                drawingContext.DrawEllipse(FoodGlowBrush, null, center, cellSize * 0.28, cellSize * 0.28);
                drawingContext.DrawEllipse(FoodBrush, null, center, cellSize * 0.17, cellSize * 0.17);
            }
        }
    }

    private void DrawGrid(DrawingContext drawingContext, Rect board, double cellSize)
    {
        for (var column = 1; column < _snapshot!.Width; column++)
        {
            var x = board.Left + column * cellSize;
            drawingContext.DrawLine(GridPen, new Point(x, board.Top), new Point(x, board.Bottom));
        }

        for (var row = 1; row < _snapshot.Height; row++)
        {
            var y = board.Top + row * cellSize;
            drawingContext.DrawLine(GridPen, new Point(board.Left, y), new Point(board.Right, y));
        }
    }

    private void DrawTrail(DrawingContext drawingContext, double left, double top, double cellSize)
    {
        if (_trail.Count < 2)
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            var previous = _trail[0];
            context.BeginFigure(ToCenter(previous, left, top, cellSize), false, false);

            for (var index = 1; index < _trail.Count; index++)
            {
                var current = _trail[index];
                if (Math.Abs(current.X - previous.X) > 1 || Math.Abs(current.Y - previous.Y) > 1)
                {
                    context.BeginFigure(ToCenter(current, left, top, cellSize), false, false);
                }
                else
                {
                    context.LineTo(ToCenter(current, left, top, cellSize), true, false);
                }

                previous = current;
            }
        }

        geometry.Freeze();
        var trailPen = TrailPen.Clone();
        trailPen.Thickness = Math.Max(2, cellSize * 0.18);
        trailPen.Freeze();
        drawingContext.DrawGeometry(null, trailPen, geometry);
    }

    private static void DrawAgent(DrawingContext drawingContext, double left, double top, double cellSize, AgentSnapshot agent)
    {
        var cell = new Rect(left + agent.X * cellSize, top + agent.Y * cellSize, cellSize, cellSize);
        drawingContext.DrawRectangle(AgentCellBrush, null, cell);

        var center = new Point(cell.Left + cellSize / 2, cell.Top + cellSize / 2);
        var direction = agent.Direction switch
        {
            Direction.North => new Vector(0, -1),
            Direction.East => new Vector(1, 0),
            Direction.South => new Vector(0, 1),
            Direction.West => new Vector(-1, 0),
            _ => throw new ArgumentOutOfRangeException()
        };
        var side = new Vector(-direction.Y, direction.X);
        var nose = center + direction * (cellSize * 0.38);
        var tail = center - direction * (cellSize * 0.25);
        var leftPoint = tail + side * (cellSize * 0.27);
        var rightPoint = tail - side * (cellSize * 0.27);

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(nose, true, true);
            context.LineTo(leftPoint, true, false);
            context.LineTo(rightPoint, true, false);
        }

        geometry.Freeze();
        drawingContext.DrawGeometry(agent.IsAlive ? AgentBrush : DeadAgentBrush, null, geometry);
    }

    private static Point ToCenter(Point position, double left, double top, double cellSize) =>
        new(left + (position.X + 0.5) * cellSize, top + (position.Y + 0.5) * cellSize);

    private static SolidColorBrush CreateBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }

    private static Pen CreatePen(byte red, byte green, byte blue, double thickness)
    {
        var pen = new Pen(CreateBrush(red, green, blue), thickness);
        pen.Freeze();
        return pen;
    }
}
