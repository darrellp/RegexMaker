using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using System;
using System.Diagnostics;

namespace Avalonia.DragCanvas;

/// <summary>
/// Represents a visual connection line between two DragCanvasNode ports
/// </summary>
public class DragCanvasConnection : Control
{
    private const double StrokeThickness = 2.0;
    private const double HitTestThickness = 15.0; // Wider area for easier clicking

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

    private bool _isHovered;

    static DragCanvasConnection()
    {
        AffectsRender<DragCanvasConnection>(StartPointProperty, EndPointProperty, StrokeProperty, IsTemporaryProperty);
        StartPointProperty.Changed.AddClassHandler<DragCanvasConnection>((x, e) => x.OnPointsChanged());
        EndPointProperty.Changed.AddClassHandler<DragCanvasConnection>((x, e) => x.OnPointsChanged());
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
        // Make connections hit-testable for Alt-click deletion
        IsHitTestVisible = true;
        Cursor = new Cursor(StandardCursorType.Hand);
        
        // Connections should not be draggable - they update based on their connected nodes
        DragCanvas.SetCanBeDragged(this, false);
    }

    private void OnPointsChanged()
    {
        // Update canvas position when points change
        var minX = Math.Min(StartPoint.X, EndPoint.X);
        var minY = Math.Min(StartPoint.Y, EndPoint.Y);
        
        Canvas.SetLeft(this, minX - HitTestThickness);
        Canvas.SetTop(this, minY - HitTestThickness);
        
        InvalidateMeasure();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _isHovered = true;
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _isHovered = false;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);
        
        // Check for Alt+Click
        if (point.Properties.IsLeftButtonPressed && 
            e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            // Find parent DragCanvas and request deletion
            var parent = FindAncestorOfType<DragCanvas>();
            parent?.DeleteConnection(this);
            e.Handled = true;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // Calculate the offset needed to transform canvas coordinates to local coordinates
        var minX = Math.Min(StartPoint.X, EndPoint.X) - HitTestThickness;
        var minY = Math.Min(StartPoint.Y, EndPoint.Y) - HitTestThickness;
        
        var localStart = new Point(StartPoint.X - minX, StartPoint.Y - minY);
        var localEnd = new Point(EndPoint.X - minX, EndPoint.Y - minY);

        // Draw invisible hit test area first (wider)
        var hitTestGeometry = new StreamGeometry();
        using (var hitContext = hitTestGeometry.Open())
        {
            var dx = localEnd.X - localStart.X;
            var dy = localEnd.Y - localStart.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            
            if (length > 0)
            {
                var perpX = -dy / length * HitTestThickness / 2;
                var perpY = dx / length * HitTestThickness / 2;

                hitContext.BeginFigure(new Point(localStart.X + perpX, localStart.Y + perpY), true);
                hitContext.LineTo(new Point(localEnd.X + perpX, localEnd.Y + perpY));
                hitContext.LineTo(new Point(localEnd.X - perpX, localEnd.Y - perpY));
                hitContext.LineTo(new Point(localStart.X - perpX, localStart.Y - perpY));
                hitContext.EndFigure(true);
            }
        }
        
        // Draw the invisible hit test area with transparent brush
        context.DrawGeometry(Brushes.Transparent, null, hitTestGeometry);

        // Draw the visible line
        var strokeBrush = _isHovered ? Brushes.Red : Stroke;
        var pen = new Pen(strokeBrush, StrokeThickness);

        // Draw dashed line for temporary connections
        if (IsTemporary)
        {
            pen = new Pen(strokeBrush, StrokeThickness, new DashStyle(new[] { 4.0, 2.0 }, 0));
        }

        context.DrawLine(pen, localStart, localEnd);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Calculate bounds that encompass the line with hit test thickness
        var minX = Math.Min(StartPoint.X, EndPoint.X);
        var maxX = Math.Max(StartPoint.X, EndPoint.X);
        var minY = Math.Min(StartPoint.Y, EndPoint.Y);
        var maxY = Math.Max(StartPoint.Y, EndPoint.Y);

        var width = maxX - minX + HitTestThickness * 2;
        var height = maxY - minY + HitTestThickness * 2;

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return finalSize;
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
            // OnPointsChanged() will be called automatically via property changed handlers
        }
    }

    public ConnectionInfo GetConnectionInfo()
    {
        if (SourceNode == null || TargetNode == null)
        {
            throw new InvalidOperationException("Both SourceNode and TargetNode must be set to get connection info.");
        }
        return new ConnectionInfo(SourceNode, TargetNode, SourcePortIndex, TargetPortIndex);
    }

    private T? FindAncestorOfType<T>() where T : class
    {
        var current = this.Parent;
        while (current != null)
        {
            if (current is T t)
                return t;
            current = (current as Visual)?.Parent;
        }
        return null;
    }
}

public class ConnectionInfo
{
    public DragCanvasNode SourceNode { get; }
    public DragCanvasNode TargetNode { get; }
    public int SourcePortIndex { get; }
    public int TargetPortIndex { get; }

    public ConnectionInfo(DragCanvasNode sourceNode, DragCanvasNode targetNode, int sourcePortIndex, int targetPortIndex)
    {
        SourceNode = sourceNode;
        TargetNode = targetNode;
        SourcePortIndex = sourcePortIndex;
        TargetPortIndex = targetPortIndex;
    }

    public (DragCanvasNode otherNode, int otherPortIndex) GetOtherEnd(DragCanvasNode node)
    {
        if (node == SourceNode)
        {
            return (TargetNode, TargetPortIndex);
        }
        else if (node == TargetNode)
        {
            return (SourceNode, SourcePortIndex);
        }
        else
        {
            throw new ArgumentException("The provided node and port index do not match either end of the connection.");
        }
    }
}