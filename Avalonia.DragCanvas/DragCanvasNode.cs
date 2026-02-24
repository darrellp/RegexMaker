using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.DragCanvas;

public class DragCanvasNode : ContentControl
{
    private const double PortRadiusNormal = 5.0;
    private const double PortRadiusHover = 7.0;
    private const double PortSpacing = 20.0;
    private const double PortPadding = 10.0;

    private readonly List<PortInfo> _leftPorts = new();
    private readonly List<PortInfo> _rightPorts = new();
    private int? _hoveredPortIndex;
    private PortSide? _hoveredPortSide;
    private Point _lastPointerPosition;
    private Size _lastArrangedSize;
    private bool _isPortDragInProgress;

    // Routed event for port clicked
    public static readonly RoutedEvent<PortClickedEventArgs> PortClickedEvent =
        RoutedEvent.Register<DragCanvasNode, PortClickedEventArgs>(
            "PortClicked",
            RoutingStrategies.Bubble);

    // Styled properties for port counts
    public static readonly StyledProperty<int> PortCtLeftProperty =
        AvaloniaProperty.Register<DragCanvasNode, int>(
            nameof(PortCtLeft),
            defaultValue: 0,
            validate: value => value >= 0);

    public static readonly StyledProperty<int> PortCtRightProperty =
        AvaloniaProperty.Register<DragCanvasNode, int>(
            nameof(PortCtRight),
            defaultValue: 0,
            validate: value => value >= 0);

    public int PortCtLeft
    {
        get => GetValue(PortCtLeftProperty);
        set => SetValue(PortCtLeftProperty, value);
    }

    public int PortCtRight
    {
        get => GetValue(PortCtRightProperty);
        set => SetValue(PortCtRightProperty, value);
    }

    static DragCanvasNode()
    {
        AffectsRender<DragCanvasNode>(PortCtLeftProperty, PortCtRightProperty);
        AffectsMeasure<DragCanvasNode>(PortCtLeftProperty, PortCtRightProperty);
    }

    public DragCanvasNode()
    {
        // Don't clip - we want to see the ports even if they extend slightly beyond bounds
        ClipToBounds = false;

        // Make sure we can receive pointer events
        Background = Background ?? Brushes.Transparent;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PortCtLeftProperty || change.Property == PortCtRightProperty)
        {
            InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var contentSize = base.MeasureOverride(availableSize);

        // Calculate minimum height needed for ports
        var maxPorts = Math.Max(PortCtLeft, PortCtRight);
        var minHeightForPorts = CalculateMinHeightForPorts(maxPorts);

        // Ensure we have enough height for the ports
        var finalHeight = Math.Max(contentSize.Height, minHeightForPorts);
        var finalWidth = Math.Max(contentSize.Width, 60); // Minimum width for ports

        return new Size(finalWidth, finalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _lastArrangedSize = finalSize;
        return base.ArrangeOverride(finalSize);
    }

    private double CalculateMinHeightForPorts(int portCount)
    {
        if (portCount == 0) return 0;

        // Calculate height needed for ports with fixed spacing
        // Top padding + (portCount * spacing) + bottom padding
        return 2 * PortPadding + (portCount - 1) * PortSpacing;
    }

    private void UpdatePorts()
    {
        _leftPorts.Clear();
        _rightPorts.Clear();

        var width = _lastArrangedSize.Width;
        var height = _lastArrangedSize.Height;

        if (width <= 0 || height <= 0)
            return;

        // Create left ports
        CreatePorts(_leftPorts, PortCtLeft, 0, height, PortSide.Left);

        // Create right ports
        CreatePorts(_rightPorts, PortCtRight, width, height, PortSide.Right);
    }

    private static void CreatePorts(List<PortInfo> portList, int count, double x, double height, PortSide side)
    {
        if (count == 0) return;

        // Calculate total height needed for all ports with fixed spacing
        var totalPortsHeight = (count - 1) * PortSpacing;

        // Center the ports vertically
        var startY = (height - totalPortsHeight) / 2;

        for (int i = 0; i < count; i++)
        {
            var y = startY + i * PortSpacing;
            portList.Add(new PortInfo
            {
                Center = new Point(x, y),
                Index = i,
                Side = side
            });
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // Update ports before rendering to ensure correct positioning
        UpdatePorts();

        // Render all ports
        RenderPorts(context, _leftPorts);
        RenderPorts(context, _rightPorts);
    }

    private void RenderPorts(DrawingContext context, List<PortInfo> ports)
    {
        foreach (var port in ports)
        {
            // Check if this port is hovered by comparing index and side
            var isHovered = _hoveredPortIndex.HasValue &&
                           _hoveredPortSide.HasValue &&
                           port.Index == _hoveredPortIndex.Value &&
                           port.Side == _hoveredPortSide.Value;

            var radius = isHovered ? PortRadiusHover : PortRadiusNormal;
            var brush = isHovered ? Brushes.Blue : Brushes.Black;

            context.DrawEllipse(brush, null, port.Center, radius, radius);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        // Check if clicking on a port
        if (_hoveredPortIndex.HasValue && _hoveredPortSide.HasValue)
        {
            var point = e.GetCurrentPoint(this);
            if (point.Properties.IsLeftButtonPressed)
            {
                _isPortDragInProgress = true;

                // Get port position in canvas coordinates
                var portPosition = GetPortCanvasPosition(_hoveredPortIndex.Value, _hoveredPortSide.Value == PortSide.Left);

                var args = new PortClickedEventArgs(
                    PortClickedEvent,
                    this,
                    _hoveredPortIndex.Value,
                    _hoveredPortSide.Value == PortSide.Left,
                    portPosition);

                RaiseEvent(args);
                e.Handled = true;
                return;
            }
        }

        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _isPortDragInProgress = false;
        base.OnPointerReleased(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        _lastPointerPosition = e.GetPosition(this);
        UpdateHoveredPort(_lastPointerPosition);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        if (_hoveredPortIndex.HasValue)
        {
            _hoveredPortIndex = null;
            _hoveredPortSide = null;
            InvalidateVisual();
        }
    }

    private void UpdateHoveredPort(Point pointerPosition)
    {
        const double hoverDistance = 20.0; // Distance within which port is considered hovered

        PortInfo? newHoveredPort = null;
        double minDistanceSq = hoverDistance * hoverDistance;

        // Ensure ports are up to date
        if (_leftPorts.Count == 0 && _rightPorts.Count == 0)
        {
            UpdatePorts();
        }

        // If our x position isn't near the left or right side there's no need to check further.
        if (pointerPosition.X > hoverDistance/2 && pointerPosition.X < _lastArrangedSize.Width - hoverDistance/2)
        {
            {
                _hoveredPortIndex = null;
                _hoveredPortSide = null;
                InvalidateVisual();
            }
            return;
        }

        // Check all ports
        foreach (var port in _leftPorts.Concat(_rightPorts))
        {
            var distanceSq = DistanceSq(pointerPosition, port.Center);
            if (distanceSq < minDistanceSq)
            {
                minDistanceSq = distanceSq;
                newHoveredPort = port;
            }
        }

        // Check if hover state changed
        bool hasChanged = false;

        if (newHoveredPort == null)
        {
            if (_hoveredPortIndex.HasValue)
            {
                hasChanged = true;
                _hoveredPortIndex = null;
                _hoveredPortSide = null;
            }
        }
        else
        {
            if (!_hoveredPortIndex.HasValue ||
                _hoveredPortIndex.Value != newHoveredPort.Index ||
                _hoveredPortSide != newHoveredPort.Side)
            {
                hasChanged = true;
                _hoveredPortIndex = newHoveredPort.Index;
                _hoveredPortSide = newHoveredPort.Side;
            }
        }

        if (hasChanged)
        {
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Gets the position of a specific port in local coordinates
    /// </summary>
    public Point? GetPortPosition(int portIndex, bool isLeftSide)
    {
        // Ensure ports are current
        if (_leftPorts.Count == 0 && _rightPorts.Count == 0)
        {
            UpdatePorts();
        }

        var portList = isLeftSide ? _leftPorts : _rightPorts;

        if (portIndex < 0 || portIndex >= portList.Count)
            return null;

        return portList[portIndex].Center;
    }

    /// <summary>
    /// Gets the position of a specific port in canvas coordinates
    /// </summary>
    public Point GetPortCanvasPosition(int portIndex, bool isLeftSide)
    {
        var localPos = GetPortPosition(portIndex, isLeftSide);
        if (!localPos.HasValue)
            return default;

        // Transform to canvas coordinates - find parent DragCanvas
        Visual? parent = this.GetVisualParent();
        while (parent != null)
        {
            if (parent is DragCanvas canvas)
            {
                return this.TranslatePoint(localPos.Value, canvas) ?? localPos.Value;
            }
            parent = parent.GetVisualParent();
        }

        return localPos.Value;
    }

    /// <summary>
    /// Gets the currently hovered port information
    /// </summary>
    public (int index, bool isLeftSide)? GetHoveredPort()
    {
        if (!_hoveredPortIndex.HasValue || !_hoveredPortSide.HasValue)
            return null;

        return (_hoveredPortIndex.Value, _hoveredPortSide.Value == PortSide.Left);
    }

    /// <summary>
    /// Manually sets the hover state for a specific port (used during connection dragging)
    /// </summary>
    public void SetPortHover(int portIndex, bool isLeftSide)
    {
        _hoveredPortIndex = portIndex;
        _hoveredPortSide = isLeftSide ? PortSide.Left : PortSide.Right;
        InvalidateVisual();
    }

    /// <summary>
    /// Clears the port hover state
    /// </summary>
    public void ClearPortHover()
    {
        if (_hoveredPortIndex.HasValue)
        {
            _hoveredPortIndex = null;
            _hoveredPortSide = null;
            InvalidateVisual();
        }
    }

    public bool IsPortDragInProgress => _isPortDragInProgress;

    private static double DistanceSq(Point p1, Point p2)
    {
        var dx = p1.X - p2.X;
        var dy = p1.Y - p2.Y;
        return dx * dx + dy * dy;
    }

    private class PortInfo
    {
        public Point Center { get; set; }
        public int Index { get; set; }
        public PortSide Side { get; set; }
    }

    private enum PortSide
    {
        Left,
        Right
    }
}

/// <summary>
/// Event arguments for port click events
/// </summary>
public class PortClickedEventArgs : RoutedEventArgs
{
    public PortClickedEventArgs(
        RoutedEvent routedEvent,
        DragCanvasNode node,
        int portIndex,
        bool isLeftSide,
        Point canvasPosition)
        : base(routedEvent)
    {
        Node = node;
        PortIndex = portIndex;
        IsLeftSide = isLeftSide;
        CanvasPosition = canvasPosition;
    }

    public DragCanvasNode Node { get; }
    public int PortIndex { get; }
    public bool IsLeftSide { get; }
    public Point CanvasPosition { get; }
}