using Avalonia.Media;

namespace Avalonia.Controls;

/// <summary>
/// Represents a visual connection line between two DragCanvasNode ports
/// </summary>
public class DragCanvasConnection : Control
{
    private const double StrokeThickness = 2.0;
    private const double HitTestPadding = 5.0;

    public static readonly StyledProperty<Point> StartPointProperty =
        AvaloniaProperty.Register<DragCanvasConnection, Point>(nameof(StartPoint));

    public static readonly StyledProperty<Point> EndPointProperty =
        AvaloniaProperty.Register<DragCanvasConnection, Point>(nameof(EndPoint));

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<DragCanvasConnection, IBrush?>(nameof(Stroke), Brushes.Black);

    public static readonly StyledProperty<DragCanvasNode?> SourceNodeProperty =
        AvaloniaProperty.Register<DragCanvasConnection, DragCanvasNode?>(nameof(SourceNode));

    public static readonly StyledProperty<DragCanvasNode?> TargetNodeProperty =
        AvaloniaProperty.Register<DragCanvasConnection, DragCanvasNode?>(nameof(TargetNode));

    public static readonly StyledProperty<int> SourcePortIndexProperty =
        AvaloniaProperty.Register<DragCanvasConnection, int>(nameof(SourcePortIndex), -1);

    public static readonly StyledProperty<int> TargetPortIndexProperty =
        AvaloniaProperty.Register<DragCanvasConnection, int>(nameof(TargetPortIndex), -1);

    public static readonly StyledProperty<bool> IsTemporaryProperty =
        AvaloniaProperty.Register<DragCanvasConnection, bool>(nameof(IsTemporary), false);

    static DragCanvasConnection()
    {
        AffectsRender<DragCanvasConnection>(StartPointProperty, EndPointProperty, StrokeProperty, IsTemporaryProperty);
    }

    public Point StartPoint
    {
        get => GetValue(StartPointProperty);
        set => SetValue(StartPointProperty, value);
    }

    public Point EndPoint
    {
        get => GetValue(EndPointProperty);
        set => SetValue(EndPointProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public DragCanvasNode? SourceNode
    {
        get => GetValue(SourceNodeProperty);
        set => SetValue(SourceNodeProperty, value);
    }

    public DragCanvasNode? TargetNode
    {
        get => GetValue(TargetNodeProperty);
        set => SetValue(TargetNodeProperty, value);
    }

    public int SourcePortIndex
    {
        get => GetValue(SourcePortIndexProperty);
        set => SetValue(SourcePortIndexProperty, value);
    }

    public int TargetPortIndex
    {
        get => GetValue(TargetPortIndexProperty);
        set => SetValue(TargetPortIndexProperty, value);
    }

    public bool IsTemporary
    {
        get => GetValue(IsTemporaryProperty);
        set => SetValue(IsTemporaryProperty, value);
    }

    public DragCanvasConnection()
    {
        ClipToBounds = false;
        // Make the connection non-hittable so it doesn't block pointer events to nodes
        IsHitTestVisible = false;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var pen = new Pen(Stroke, StrokeThickness);
        
        // Draw dashed line for temporary connections
        if (IsTemporary)
        {
            pen = new Pen(Stroke, StrokeThickness, new DashStyle(new[] { 4.0, 2.0 }, 0));
        }

        context.DrawLine(pen, StartPoint, EndPoint);
    }

    /// <summary>
    /// Updates the connection endpoints based on the current positions of connected nodes
    /// </summary>
    public void UpdateFromNodes()
    {
        if (SourceNode == null || TargetNode == null)
        {
            return;
        }

        var sourcePort = SourceNode.GetPortPosition(SourcePortIndex, isLeftSide: false);
        var targetPort = TargetNode.GetPortPosition(TargetPortIndex, isLeftSide: true);

        if (sourcePort.HasValue && targetPort.HasValue)
        {
            // Get canvas coordinates
            var sourceCanvas = SourceNode.GetPortCanvasPosition(SourcePortIndex, false);
            var targetCanvas = TargetNode.GetPortCanvasPosition(TargetPortIndex, true);
            
            StartPoint = sourceCanvas;
            EndPoint = targetCanvas;
        }
    }
}