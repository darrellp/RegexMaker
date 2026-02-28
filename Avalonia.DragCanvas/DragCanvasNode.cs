using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Avalonia.DragCanvas;

public class DragCanvasNode : ContentControl
{
    private const double PortRadiusNormal = 5.0;
    private const double PortRadiusHover = 7.0;
    private const double PortSpacing = 20.0;
    private const double PortPadding = 10.0;
    private const double SelectionBorderThickness = 2.0;
    private const double DeletionBorderThickness = 3.0;

    private readonly List<PortInfo> _leftPorts = new();
    private readonly List<PortInfo> _rightPorts = new();

    // _leftConnections[i] is the list of connections for the port at index i on the left side.
    // Similarly for _rightConnections.
    // This allows for multiple connections per port (though specific implementations may restrict this)
    private bool _connectionsInitialized = false;
    private readonly List<List<DragCanvasConnection>> _leftConnections = new();
    private readonly List<List<DragCanvasConnection>> _rightConnections = new();

    private int? _hoveredPortIndex;
    private PortSide? _hoveredPortSide;
    private Point _lastPointerPosition;
    private Size _lastArrangedSize;
    private bool _isPortDragInProgress;
    private bool _isAltHovered;

    // Routed event for port clicked
    public static readonly RoutedEvent<PortClickedEventArgs> PortClickedEvent =
        RoutedEvent.Register<DragCanvasNode, PortClickedEventArgs>(
            "PortClicked",
            RoutingStrategies.Bubble);

    // Routed event for node deletion
    public static readonly RoutedEvent<NodeDeletedEventArgs> NodeDeletedEvent =
        RoutedEvent.Register<DragCanvasNode, NodeDeletedEventArgs>(
            "NodeDeleted",
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

    // Styled properties for selection
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<DragCanvasNode, bool>(
            nameof(IsSelected),
            defaultValue: false);

    public static readonly StyledProperty<IBrush?> SelectionBrushProperty =
        AvaloniaProperty.Register<DragCanvasNode, IBrush?>(
            nameof(SelectionBrush),
            defaultValue: Brushes.Blue);

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

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        internal set => SetValue(IsSelectedProperty, value);
    }

    public IBrush? SelectionBrush
    {
        get => GetValue(SelectionBrushProperty);
        internal set => SetValue(SelectionBrushProperty, value);
    }

    static DragCanvasNode()
    {
        AffectsRender<DragCanvasNode>(PortCtLeftProperty, PortCtRightProperty, IsSelectedProperty, SelectionBrushProperty);
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

        if (change.Property == PortCtLeftProperty)
        {
            SynchronizeConnectionList(_leftConnections, (int)change.NewValue!);
            InvalidateVisual();
        }
        else if (change.Property == PortCtRightProperty)
        {
            SynchronizeConnectionList(_rightConnections, (int)change.NewValue!);
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Ensures the connection list matches the port count
    /// </summary>
    private void SynchronizeConnectionList(List<List<DragCanvasConnection>> connectionsList, int newPortCount)
    {
        // If we need more connection lists, add them
        while (connectionsList.Count < newPortCount)
        {
            connectionsList.Add(new List<DragCanvasConnection>());
        }
        
        // If we have too many, we need to handle the extra connections
        // This is important - we should notify that connections are being removed
        while (connectionsList.Count > newPortCount)
        {
            var lastIndex = connectionsList.Count - 1;
            var connectionsToRemove = connectionsList[lastIndex];
            
            // Remove any connections on the port being removed
            foreach (var connection in connectionsToRemove.ToList())
            {
                // Find parent canvas to delete the connection
                var parent = FindAncestorOfType<DragCanvas>();
                parent?.DeleteConnection(connection);
            }
            
            connectionsList.RemoveAt(lastIndex);
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
        var result = base.ArrangeOverride(finalSize);

        // Only notify if the size actually changed
        if (_lastArrangedSize != finalSize)
        {
            _lastArrangedSize = finalSize;

            // Update ports immediately with the new size
            UpdatePorts();
        }

        return result;
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
        // Clear port visual information but preserve connection lists
        _leftPorts.Clear();
        _rightPorts.Clear();

        var width = _lastArrangedSize.Width;
        var height = _lastArrangedSize.Height;

        if (width <= 0 || height <= 0)
            return;

        // Create left ports
        CreatePorts(_leftPorts, _leftConnections, PortCtLeft, 0, height, PortSide.Left);

        // Create right ports
        CreatePorts(_rightPorts, _rightConnections, PortCtRight, width, height, PortSide.Right);
        _connectionsInitialized = true;
    }

    private void CreatePorts(List<PortInfo> portList, List<List<DragCanvasConnection>> portConnections, int count, double x, double height, PortSide side)
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
            
            // Initialize connection list for this port if not already done
            if (!_connectionsInitialized)
            {
                portConnections.Add(new List<DragCanvasConnection>());
            }
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // Render deletion border if Alt key is held while hovering (takes priority)
        if (_isAltHovered)
        {
            var radius = CornerRadius.TopLeft + 12;
            var rect = new Rect(0, 0, _lastArrangedSize.Width, _lastArrangedSize.Height);
            var pen = new Pen(Brushes.Red, DeletionBorderThickness);
            context.DrawRectangle(null, pen, rect);
        }
        // Render selection border if selected
        else if (IsSelected && SelectionBrush != null)
        {
            var radius = CornerRadius.TopLeft + 12; // Assuming uniform corner radius for simplicity
            var rect = new Rect(0, 0, _lastArrangedSize.Width, _lastArrangedSize.Height);
            var pen = new Pen(SelectionBrush, SelectionBorderThickness);
            context.DrawRectangle(null, pen, rect);
        }

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
        var point = e.GetCurrentPoint(this);
        
        // Check for Alt+Click on node body (not on port) to delete the node
        if (point.Properties.IsLeftButtonPressed && 
            e.KeyModifiers.HasFlag(KeyModifiers.Alt) &&
            !_hoveredPortIndex.HasValue)
        {
            var parent = FindAncestorOfType<DragCanvas>();
            if (parent != null)
            {
                var args = new NodeDeletedEventArgs(NodeDeletedEvent, this);
                RaiseEvent(args);
                e.Handled = true;
                return;
            }
        }

        // Check if clicking on a port
        if (_hoveredPortIndex.HasValue && _hoveredPortSide.HasValue)
        {
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

        // Don't update hover state if we're currently dragging a port connection
        if (_isPortDragInProgress)
            return;

        _lastPointerPosition = e.GetPosition(this);
        
        // Update Alt hover state
        bool wasAltHovered = _isAltHovered;
        _isAltHovered = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        
        if (wasAltHovered != _isAltHovered)
        {
            InvalidateVisual();
        }

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

        if (_isAltHovered)
        {
            _isAltHovered = false;
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
                var translated = this.TranslatePoint(localPos.Value, canvas);
                return translated ?? localPos.Value;
            }
            parent = parent.GetVisualParent();
        }

        return localPos.Value;
    }

    internal virtual bool AllowConnection(int portIndex, bool isLeftSide, DragCanvasNode? otherNode, int otherPortIndex, bool otherIsLeftSide)
    {
        if (isLeftSide)
        {
            if (_leftConnections[portIndex] != null && _leftConnections[portIndex].Count > 0)
            {
                // By default we only allow one connection per left port
                return false;
            }
        }
        // Only allow outputs to input connections
        return isLeftSide != otherIsLeftSide;
    }

    // Called by DragCanvas when a new connection is made to update internal state
    internal virtual void OnConnectionMade(DragCanvasConnection connection, int iPort, PortSide thisSide)
    {
        var count = thisSide == PortSide.Left ? PortCtLeft : PortCtRight;
        Debug.Assert(iPort >= 0 && iPort < count, "Port index should be valid when making a connection.");

        var connectionsList = thisSide == PortSide.Left ? _leftConnections : _rightConnections;
        
        // Ensure the connection list is initialized for this port
        while (connectionsList.Count <= iPort)
        {
            connectionsList.Add(new List<DragCanvasConnection>());
        }
        
        Debug.Assert(connectionsList[iPort] != null, "Connections list for the port should have been initialized.");
        connectionsList[iPort].Add(connection);
    }

    // Called by DragCanvas when a connection is removed to update internal state
    internal virtual void OnConnectionRemoved(DragCanvasConnection connection, int iPort, PortSide thisSide)
    {
        var count = thisSide == PortSide.Left ? PortCtLeft : PortCtRight;
        Debug.Assert(iPort >= 0 && iPort < count, "Port index should be valid when removing a connection.");

        var connectionsList = thisSide == PortSide.Left ? _leftConnections : _rightConnections;
        
        if (iPort < connectionsList.Count && connectionsList[iPort] != null)
        {
            connectionsList[iPort].Remove(connection);
        }
    }

    /// <summary>
    /// Gets all connections for a specific port
    /// </summary>
    /// <param name="portIndex">The port index</param>
    /// <param name="isLeftSide">True for left (input) ports, false for right (output) ports</param>
    /// <returns>Read-only collection of connections for the specified port</returns>
    public IReadOnlyList<DragCanvasConnection> GetConnectionsForPort(int portIndex, bool isLeftSide)
    {
        var connectionsList = isLeftSide ? _leftConnections : _rightConnections;
        
        if (portIndex < 0 || portIndex >= connectionsList.Count)
            return Array.Empty<DragCanvasConnection>();
        
        return connectionsList[portIndex].AsReadOnly();
    }

    /// <summary>
    /// Gets all connections for this node (both incoming and outgoing)
    /// </summary>
    public IEnumerable<DragCanvasConnection> GetAllConnections()
    {
        foreach (var connectionList in _leftConnections)
        {
            foreach (var connection in connectionList)
            {
                yield return connection;
            }
        }

        foreach (var connectionList in _rightConnections)
        {
            foreach (var connection in connectionList)
            {
                yield return connection;
            }
        }
    }

    /// <summary>
    /// Gets all incoming connections (left side ports)
    /// </summary>
    public IEnumerable<DragCanvasConnection> GetIncomingConnections()
    {
        foreach (var connectionList in _leftConnections)
        {
            foreach (var connection in connectionList)
            {
                yield return connection;
            }
        }
    }

    /// <summary>
    /// Gets all outgoing connections (right side ports)
    /// </summary>
    public IEnumerable<DragCanvasConnection> GetOutgoingConnections()
    {
        foreach (var connectionList in _rightConnections)
        {
            foreach (var connection in connectionList)
            {
                yield return connection;
            }
        }
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

    private T? FindAncestorOfType<T>() where T : class
    {
        Visual? current = this.GetVisualParent();
        while (current != null)
        {
            if (current is T ancestor)
                return ancestor;
            current = current.GetVisualParent();
        }
        return null;
    }

    private class PortInfo
    {
        public Point Center { get; set; }
        public int Index { get; set; }
        public PortSide Side { get; set; }
    }

    public enum PortSide
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

/// <summary>
/// Event arguments for node deletion events
/// </summary>
public class NodeDeletedEventArgs : RoutedEventArgs
{
    public NodeDeletedEventArgs(RoutedEvent routedEvent, DragCanvasNode node)
        : base(routedEvent)
    {
        Node = node;
    }

    public DragCanvasNode Node { get; }
}