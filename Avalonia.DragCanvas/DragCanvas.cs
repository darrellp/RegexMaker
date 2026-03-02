using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Collections.Specialized;
using static Avalonia.DragCanvas.DragCanvasNode;
using System.Text.Json;

namespace Avalonia.DragCanvas;

public class DragCanvas : Canvas
{
    private Control? elementBeingDragged;
    private Point origCursorLocation;
    private double origHorizOffset, origVertOffset;
    private bool modifyLeftOffset, modifyTopOffset;
    private bool hasMovedBeyondThreshold;

    // Connection management
    private DragCanvasConnection? _temporaryConnection;
    private DragCanvasNode? _connectionSourceNode;
    private int _connectionSourcePortIndex;
    private bool _connectionSourceIsLeftSide;
    private readonly List<DragCanvasConnection> _connections = new();
    private DragCanvasNode? _lastHoveredNodeDuringConnection;
    private (int index, bool isLeftSide)? _lastHoveredPortDuringConnection;

    // Selection management
    private DragCanvasNode? _selectedNode;

    public bool IsDragInProgress { get; private set; }

    // Routed event for connections created
    public static readonly RoutedEvent<ConnectionEventArgs> ConnectionCreatedEvent =
        RoutedEvent.Register<DragCanvas, ConnectionEventArgs>(
            "ConnectionCreated",
            RoutingStrategies.Bubble);

    // CLR event wrapper for XAML binding
    public event EventHandler<ConnectionEventArgs>? ConnectionCreated
    {
        add => AddHandler(ConnectionCreatedEvent, value);
        remove => RemoveHandler(ConnectionCreatedEvent, value);
    }


    // Routed event for node selection
    public static readonly RoutedEvent<NodeSelectedEventArgs> NodeSelectedEvent =
        RoutedEvent.Register<DragCanvas, NodeSelectedEventArgs>(
            "NodeSelected",
            RoutingStrategies.Bubble);

    // CLR event wrapper for XAML binding
    public event EventHandler<NodeSelectedEventArgs>? NodeSelected
    {
        add => AddHandler(NodeSelectedEvent, value);
        remove => RemoveHandler(NodeSelectedEvent, value);
    }

    // Routed event for connection deletion
    public static readonly RoutedEvent<ConnectionEventArgs> ConnectionDeletedEvent =
        RoutedEvent.Register<DragCanvas, ConnectionEventArgs>(
            "ConnectionDeleted",
            RoutingStrategies.Bubble);

    // CLR event wrapper for XAML binding
    public event EventHandler<ConnectionEventArgs>? ConnectionDeleted
    {
        add => AddHandler(ConnectionDeletedEvent, value);
        remove => RemoveHandler(ConnectionDeletedEvent, value);
    }

    // Routed event for node deletion
    public static readonly RoutedEvent<NodeDeletedEventArgs> NodeDeletedEvent =
        RoutedEvent.Register<DragCanvas, NodeDeletedEventArgs>(
            "NodeDeleted",
            RoutingStrategies.Bubble);

    // CLR event wrapper for XAML binding
    public event EventHandler<NodeDeletedEventArgs>? NodeDeleted
    {
        add => AddHandler(NodeDeletedEvent, value);
        remove => RemoveHandler(NodeDeletedEvent, value);
    }

    // Attached property for CanBeDragged
    public static readonly AttachedProperty<bool> CanBeDraggedProperty =
        AvaloniaProperty.RegisterAttached<DragCanvas, Control, bool>(
            "CanBeDragged",
            defaultValue: true);

    public static bool GetCanBeDragged(Control element) =>
        element.GetValue(CanBeDraggedProperty);

    public static void SetCanBeDragged(Control element, bool value) =>
        element.SetValue(CanBeDraggedProperty, value);

    // StyledProperty for AllowDragging
    public static readonly StyledProperty<bool> AllowDraggingProperty =
        AvaloniaProperty.Register<DragCanvas, bool>(
            nameof(AllowDragging),
            defaultValue: true);

    public bool AllowDragging
    {
        get => GetValue(AllowDraggingProperty);
        set => SetValue(AllowDraggingProperty, value);
    }

    public static readonly StyledProperty<IBrush?> NodeBackgroundProperty =
    AvaloniaProperty.Register<DragCanvas, IBrush?>(
        nameof(NodeBackground));

    public static readonly StyledProperty<IBrush?> NodeForegroundProperty =
        AvaloniaProperty.Register<DragCanvas, IBrush?>(
            nameof(NodeForeground));

    public IBrush? NodeBackground
    {
        get => GetValue(NodeBackgroundProperty);
        set => SetValue(NodeBackgroundProperty, value);
    }

    public IBrush? NodeForeground
    {
        get => GetValue(NodeForegroundProperty);
        set => SetValue(NodeForegroundProperty, value);
    }

    // StyledProperty for SelectColor
    public static readonly StyledProperty<Media.IBrush?> SelectColorProperty =
        AvaloniaProperty.Register<DragCanvas, Media.IBrush?>(
            nameof(SelectColor),
            defaultValue: Media.Brushes.Blue);

    public Media.IBrush? SelectColor
    {
        get => GetValue(SelectColorProperty);
        set => SetValue(SelectColorProperty, value);
    }

    // Property for SelectedNode
    public DragCanvasNode? SelectedNode
    {
        get => _selectedNode;
        private set
        {
            if (_selectedNode != value)
            {
                // Clear previous selection
                if (_selectedNode != null)
                {
                    _selectedNode.IsSelected = false;
                }

                _selectedNode = value;

                // Set new selection
                if (_selectedNode != null)
                {
                    _selectedNode.IsSelected = true;
                    _selectedNode.SelectionBrush = SelectColor;

                    // Raise selection event
                    var eventArgs = new NodeSelectedEventArgs(NodeSelectedEvent, _selectedNode);
                    RaiseEvent(eventArgs);
                }
            }
        }
    }

    public DragCanvas()
    {
        // Subscribe to port clicked events
        AddHandler(DragCanvasNode.PortClickedEvent, OnPortClicked);
        
        // Subscribe to node deletion events
        AddHandler(DragCanvasNode.NodeDeletedEvent, OnNodeDeleteRequested);
    }

    protected override void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        base.ChildrenChanged(sender, e);

        // Subscribe to layout updates for newly added nodes
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is DragCanvasNode node)
                {
                    node.LayoutUpdated += OnNodeLayoutUpdated;
                }
            }
        }

        // Unsubscribe from layout updates for removed nodes
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is DragCanvasNode node)
                {
                    node.LayoutUpdated -= OnNodeLayoutUpdated;
                }
            }
        }
    }

    private void OnNodeLayoutUpdated(object? sender, EventArgs e)
    {
        if (sender is DragCanvasNode node)
        {
            // Update all connections involving this node when its layout changes
            UpdateConnectionsForNode(node);
        }
    }

    private void OnPortClicked(object? sender, PortClickedEventArgs e)
    {
        // Start creating a connection
        _connectionSourceNode = e.Node;
        _connectionSourcePortIndex = e.PortIndex;
        _connectionSourceIsLeftSide = e.IsLeftSide;

        // Create temporary connection line
        _temporaryConnection = new DragCanvasConnection
        {
            StartPoint = e.CanvasPosition,
            EndPoint = e.CanvasPosition,
            IsTemporary = true,
            Stroke = Media.Brushes.Gray
        };

        Children.Add(_temporaryConnection);
        e.Handled = true;
    }

    private void OnNodeDeleteRequested(object? sender, NodeDeletedEventArgs e)
    {
        // Handle the node deletion request
        DeleteNode(e.Node);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // Check if a node is starting a port connection
        if (e.Source is DragCanvasNode node && node.IsPortDragInProgress)
        {
            // Port connection is being handled by the node
            return;
        }

        if (!AllowDragging) return;

        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed) return;

        origCursorLocation = point.Position;
        hasMovedBeyondThreshold = false;

        // Find canvas child
        if (e.Source is Visual visual)
        {
            elementBeingDragged = FindCanvasChild(visual);
            if (elementBeingDragged == null) return;

            // Check if the element can be dragged
            if (!GetCanBeDragged(elementBeingDragged))
            {
                elementBeingDragged = null;
                return;
            }

            // Don't drag if it's a node with an active port drag
            if (elementBeingDragged is DragCanvasNode dragNode && dragNode.IsPortDragInProgress)
            {
                elementBeingDragged = null;
                return;
            }

            double left = GetLeft(elementBeingDragged);
            double right = GetRight(elementBeingDragged);
            double top = GetTop(elementBeingDragged);
            double bottom = GetBottom(elementBeingDragged);

            origHorizOffset = ResolveOffset(left, right, out modifyLeftOffset);
            origVertOffset = ResolveOffset(top, bottom, out modifyTopOffset);

            IsDragInProgress = true;
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        Point cursorLocation = e.GetPosition(this);

        // Update temporary connection if active
        if (_temporaryConnection != null)
        {
            _temporaryConnection.EndPoint = cursorLocation;

            // Update hover state for connection target
            UpdateConnectionHoverFeedback(cursorLocation);
            return;
        }

        if (elementBeingDragged == null || !IsDragInProgress) return;

        // Check if we've moved beyond the threshold (taxicab distance > 5)
        if (!hasMovedBeyondThreshold)
        {
            double taxicabDistance = Math.Abs(cursorLocation.X - origCursorLocation.X) +
                                    Math.Abs(cursorLocation.Y - origCursorLocation.Y);

            if (taxicabDistance > 5)
            {
                hasMovedBeyondThreshold = true;
            }
            else
            {
                // Not ready to drag yet
                return;
            }
        }

        double newHorizontalOffset = modifyLeftOffset
            ? origHorizOffset + (cursorLocation.X - origCursorLocation.X)
            : origHorizOffset - (cursorLocation.X - origCursorLocation.X);

        double newVerticalOffset = modifyTopOffset
            ? origVertOffset + (cursorLocation.Y - origCursorLocation.Y)
            : origVertOffset - (cursorLocation.Y - origCursorLocation.Y);

        if (modifyLeftOffset)
            SetLeft(elementBeingDragged, newHorizontalOffset);
        else
            SetRight(elementBeingDragged, newHorizontalOffset);

        if (modifyTopOffset)
            SetTop(elementBeingDragged, newVerticalOffset);
        else
            SetBottom(elementBeingDragged, newVerticalOffset);

        // Update all connections involving this node
        if (elementBeingDragged is DragCanvasNode movedNode)
        {
            UpdateConnectionsForNode(movedNode);
        }
    }

    private void UpdateConnectionHoverFeedback(Point canvasPosition)
    {
        var hoveredNode = FindNodeNearPosition(canvasPosition);
        (int index, bool isLeftSide)? hoveredPort = null;

        // Only consider nodes that are NOT the source node
        if (hoveredNode != null && hoveredNode != _connectionSourceNode)
        {
            hoveredPort = FindPortAtPosition(hoveredNode, canvasPosition);

            // Only consider valid ports (opposite side from source)
            if (hoveredPort.HasValue)
            {
                var (_, isLeftSide) = hoveredPort.Value;
                if (!hoveredNode.AllowConnection(hoveredPort.Value.index, isLeftSide, _connectionSourceNode!, _connectionSourcePortIndex, _connectionSourceIsLeftSide) ||
                    !_connectionSourceNode!.AllowConnection(_connectionSourcePortIndex, _connectionSourceIsLeftSide, hoveredNode, hoveredPort.Value.index, isLeftSide))
                {
                    // Same side - not valid
                    hoveredPort = null;
                }
            }
        }

        // Check if hover state changed
        if (hoveredNode != _lastHoveredNodeDuringConnection || hoveredPort != _lastHoveredPortDuringConnection)
        {
            // Clear old hover
            if (_lastHoveredNodeDuringConnection != null)
            {
                _lastHoveredNodeDuringConnection.ClearPortHover();
            }

            // Set new hover
            if (hoveredNode != null && hoveredPort.HasValue)
            {
                var (index, isLeftSide) = hoveredPort.Value;
                hoveredNode.SetPortHover(index, isLeftSide);
            }

            _lastHoveredNodeDuringConnection = hoveredNode;
            _lastHoveredPortDuringConnection = hoveredPort;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        // Handle connection completion
        if (_temporaryConnection != null)
        {
            Point releasePosition = e.GetPosition(this);

            // Find if we're over a node (check ports first, they might be outside node bounds)
            var hoveredNode = FindNodeNearPosition(releasePosition);

            if (hoveredNode != null && hoveredNode != _connectionSourceNode)
            {
                var hoveredPort = FindPortAtPosition(hoveredNode, releasePosition);

                if (hoveredPort.HasValue)
                {
                    var (targetPortIndex, targetIsLeftSide) = hoveredPort.Value;

                    Debug.Assert(_connectionSourceNode != null, "Source node should not be null when making a connection");
                    // Validate: Has to be a valid connection according to the node's rules
                    if (hoveredNode.AllowConnection(targetPortIndex, targetIsLeftSide, _connectionSourceNode, _connectionSourcePortIndex, _connectionSourceIsLeftSide) &&
                        _connectionSourceNode.AllowConnection(_connectionSourcePortIndex, _connectionSourceIsLeftSide, hoveredNode, targetPortIndex, targetIsLeftSide))
                    {
                        // Determine which is source (right) and which is target (left)
                        DragCanvasNode sourceNode, targetNode;
                        int sourcePortIndex, targetPortIndex2;

                        if (_connectionSourceIsLeftSide)
                        {
                            // Started from left port, so the hovered right port is the source
                            sourceNode = hoveredNode;
                            sourcePortIndex = targetPortIndex;
                            targetNode = _connectionSourceNode;
                            targetPortIndex2 = _connectionSourcePortIndex;
                        }
                        else
                        {
                            // Started from right port
                            sourceNode = _connectionSourceNode;
                            sourcePortIndex = _connectionSourcePortIndex;
                            targetNode = hoveredNode;
                            targetPortIndex2 = targetPortIndex;
                        }

                        // Get the actual port positions now
                        var sourcePortPos = sourceNode.GetPortCanvasPosition(sourcePortIndex, false);
                        var targetPortPos = targetNode.GetPortCanvasPosition(targetPortIndex2, true);

                        // Create permanent connection with explicit positions
                        var connection = new DragCanvasConnection
                        {
                            SourceNode = sourceNode,
                            SourcePortIndex = sourcePortIndex,
                            TargetNode = targetNode,
                            TargetPortIndex = targetPortIndex2,
                            Stroke = Media.Brushes.Black,
                            StartPoint = sourcePortPos,
                            EndPoint = targetPortPos
                        };

                        _connections.Add(connection);
                        Children.Insert(0, connection); // Add at beginning so it's behind nodes

                        // Update node connection tracking
                        sourceNode.OnConnectionMade(connection, sourcePortIndex, PortSide.Right);
                        targetNode.OnConnectionMade(connection, targetPortIndex2, PortSide.Left);

                        // Raise connection created event
                        var eventArgs = new ConnectionEventArgs(
                            ConnectionCreatedEvent,
                            sourceNode,
                            targetNode,
                            sourcePortIndex,
                            targetPortIndex2)
                        {
                            SourcePortPosition = sourcePortPos,
                            TargetPortPosition = targetPortPos
                        };

                        RaiseEvent(eventArgs);
                    }
                }
            }

            // Clear hover feedback
            if (_lastHoveredNodeDuringConnection != null)
            {
                _lastHoveredNodeDuringConnection.ClearPortHover();
                _lastHoveredNodeDuringConnection = null;
                _lastHoveredPortDuringConnection = null;
            }

            // Remove temporary connection
            Children.Remove(_temporaryConnection);
            _temporaryConnection = null;
            _connectionSourceNode = null;
        }
        else if (elementBeingDragged is DragCanvasNode nodeClicked && !hasMovedBeyondThreshold)
        {
            // This was a click (not a drag) - select the node
            SelectedNode = nodeClicked;
        }

        elementBeingDragged = null;
        IsDragInProgress = false;
        hasMovedBeyondThreshold = false;
    }

    private DragCanvasNode? FindNodeNearPosition(Point canvasPosition)
    {
        const double portDetectionRadius = 20.0; // Extra margin for ports outside bounds

        // Check all nodes - prioritize port proximity over bounds
        foreach (var child in Children)
        {
            if (child is DragCanvasNode node)
            {
                // First check if any port is close
                var port = FindPortAtPosition(node, canvasPosition);
                if (port.HasValue)
                {
                    return node;
                }

                // Then check expanded bounds (to account for ports at edges)
                var nodePos = node.TranslatePoint(new Point(0, 0), this);
                if (nodePos.HasValue)
                {
                    var bounds = new Rect(nodePos.Value, node.Bounds.Size);
                    // Expand bounds by port detection radius
                    var expandedBounds = bounds.Inflate(portDetectionRadius);
                    if (expandedBounds.Contains(canvasPosition))
                    {
                        return node;
                    }
                }
            }
        }
        return null;
    }

    private (int portIndex, bool isLeftSide)? FindPortAtPosition(DragCanvasNode node, Point canvasPosition)
    {
        const double portDetectionRadius = 15.0;

        // Check left ports
        for (int i = 0; i < node.PortCtLeft; i++)
        {
            var portPos = node.GetPortCanvasPosition(i, true);
            var dist = Distance(canvasPosition, portPos);
            if (dist <= portDetectionRadius)
            {
                return (i, true);
            }
        }

        // Check right ports
        for (int i = 0; i < node.PortCtRight; i++)
        {
            var portPos = node.GetPortCanvasPosition(i, false);
            var dist = Distance(canvasPosition, portPos);
            if (dist <= portDetectionRadius)
            {
                return (i, false);
            }
        }

        return null;
    }

    private static double Distance(Point p1, Point p2)
    {
        var dx = p1.X - p2.X;
        var dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    internal void UpdateConnectionsForNode(DragCanvasNode node)
    {
        foreach (var connection in _connections.Where(c => c.SourceNode == node || c.TargetNode == node))
        {
            connection.UpdateFromNodes();
        }
    }

    private Control? FindCanvasChild(Visual? visual)
    {
        while (visual != null)
        {
            if (visual is Control control && Children.Contains(control))
                return control;

            visual = visual.GetVisualParent();
        }
        return null;
    }

    private static double ResolveOffset(double side1, double side2, out bool useSide1)
    {
        useSide1 = true;
        if (double.IsNaN(side1))
        {
            if (double.IsNaN(side2))
                return 0;
            else
            {
                useSide1 = false;
                return side2;
            }
        }
        return side1;
    }

    /// <summary>
    /// Gets all connections in the canvas
    /// </summary>
    public IReadOnlyList<DragCanvasConnection> Connections => _connections.AsReadOnly();

    /// <summary>
    /// Removes a connection from the canvas
    /// </summary>
    public void RemoveConnection(DragCanvasConnection connection)
    {
        if (_connections.Remove(connection))
        {
            Children.Remove(connection);
        }
    }

    /// <summary>
    /// Clears all connections
    /// </summary>
    public void ClearConnections()
    {
        foreach (var connection in _connections.ToList())
        {
            Children.Remove(connection);
        }
        _connections.Clear();
    }

    /// <summary>
    /// Initiates a drag operation on the specified element programmatically.
    /// </summary>
    /// <param name="element">The element to drag</param>
    /// <param name="startPosition">The starting position relative to the canvas</param>
    public void BeginDrag(Control element, Point startPosition)
    {
        if (!AllowDragging) return;
        if (!Children.Contains(element)) return;

        elementBeingDragged = element;
        origCursorLocation = startPosition;
        hasMovedBeyondThreshold = false;

        double left = GetLeft(elementBeingDragged);
        double right = GetRight(elementBeingDragged);
        double top = GetTop(elementBeingDragged);
        double bottom = GetBottom(elementBeingDragged);

        origHorizOffset = ResolveOffset(left, right, out modifyLeftOffset);
        origVertOffset = ResolveOffset(top, bottom, out modifyTopOffset);

        IsDragInProgress = true;
    }

    /// <summary>
    /// Deletes a connection from the canvas
    /// </summary>
    public void DeleteConnection(DragCanvasConnection connection)
    {
        if (connection.SourceNode == null || connection.TargetNode == null)
            return;

        // Update node connection tracking first
        connection.SourceNode.OnConnectionRemoved(connection, connection.SourcePortIndex, DragCanvasNode.PortSide.Right);
        connection.TargetNode.OnConnectionRemoved(connection, connection.TargetPortIndex, DragCanvasNode.PortSide.Left);

        // Raise deletion event
        var eventArgs = new ConnectionEventArgs(
            ConnectionDeletedEvent,
            connection.SourceNode,
            connection.TargetNode,
            connection.SourcePortIndex,
            connection.TargetPortIndex);

        RaiseEvent(eventArgs);

        // Remove from internal tracking
        RemoveConnection(connection);
    }

    /// <summary>
    /// Deletes a node from the canvas and removes all its connections
    /// </summary>
    public void DeleteNode(DragCanvasNode node)
    {
        // Get all connections using the node's built-in tracking
        var allConnections = node.GetAllConnections().ToList();

        // Delete all connections to/from this node
        foreach (var connection in allConnections)
        {
            DeleteConnection(connection);
        }

        // Remove the node from canvas
        Children.Remove(node);

        // Clear selection if this was the selected node
        if (_selectedNode == node)
        {
            _selectedNode = null;
        }

        // Raise node deleted event for application-level handling
        var eventArgs = new NodeDeletedEventArgs(NodeDeletedEvent, node);
        RaiseEvent(eventArgs);
    }

    /// <summary>
    /// Serializes the current canvas state to a CanvasSerializationData object
    /// </summary>
    public CanvasSerializationData SerializeCanvas()
    {
        var data = new CanvasSerializationData();
        var nodeIdMap = new Dictionary<DragCanvasNode, int>();
        int nextId = 0;

        // Serialize nodes
        foreach (var child in Children)
        {
            if (child is DragCanvasNode node)
            {
                var nodeId = nextId++;
                nodeIdMap[node] = nodeId;

                var nodeData = new NodeSerializationData
                {
                    NodeId = nodeId,
                    X = GetLeft(node),
                    Y = GetTop(node),
                    PortCtLeft = node.PortCtLeft,
                    PortCtRight = node.PortCtRight
                };

                // If node implements ISerializableNode, get application data
                if (node is ISerializableNode serializableNode)
                {
                    var jsonString = serializableNode.SerializeApplicationData();
                    // Parse the JSON string into a JsonElement for proper nesting
                    if (!string.IsNullOrEmpty(jsonString))
                    {
                        nodeData.ApplicationData = JsonSerializer.Deserialize<JsonElement>(jsonString);
                    }
                    nodeData.NodeTypeName = serializableNode.GetNodeTypeName();
                }

                data.Nodes.Add(nodeData);
            }
        }

        // Serialize connections
        foreach (var connection in _connections)
        {
            if (connection.SourceNode != null && connection.TargetNode != null &&
                nodeIdMap.TryGetValue(connection.SourceNode, out int sourceId) &&
                nodeIdMap.TryGetValue(connection.TargetNode, out int targetId))
            {
                data.Connections.Add(new ConnectionSerializationData
                {
                    SourceNodeId = sourceId,
                    TargetNodeId = targetId,
                    SourcePortIndex = connection.SourcePortIndex,
                    TargetPortIndex = connection.TargetPortIndex
                });
            }
        }

        return data;
    }

    /// <summary>
    /// Deserializes canvas state from a CanvasSerializationData object
    /// </summary>
    /// <param name="data">The serialization data</param>
    /// <param name="nodeFactory">Factory function to create nodes from type names</param>
    public void DeserializeCanvas(CanvasSerializationData data, System.Func<string?, DragCanvasNode?> nodeFactory)
    {
        // Clear existing canvas
        ClearConnections();
        Children.Clear();
        _selectedNode = null;

        var nodeMap = new Dictionary<int, DragCanvasNode>();

        // Recreate nodes
        foreach (var nodeData in data.Nodes)
        {
            var node = nodeFactory(nodeData.NodeTypeName);
            if (node == null)
                continue;

            // Set position
            SetLeft(node, nodeData.X);
            SetTop(node, nodeData.Y);

            // Restore application data FIRST (before setting port counts)
            // This allows the application to set up its internal data structures
            if (node is ISerializableNode serializableNode && nodeData.ApplicationData != null)
            {
                // Convert JsonElement back to JSON string for backwards compatibility
                var jsonString = JsonSerializer.Serialize(nodeData.ApplicationData.Value);
                serializableNode.DeserializeApplicationData(jsonString);
            }

            // Set port counts AFTER restoring application data
            // This ensures the node's Parameters collection is properly sized
            node.PortCtLeft = nodeData.PortCtLeft;
            node.PortCtRight = nodeData.PortCtRight;

            // Add to canvas
            Children.Add(node);
            nodeMap[nodeData.NodeId] = node;

            // Force layout update
            node.UpdateLayout();
        }

        // Recreate connections
        foreach (var connData in data.Connections)
        {
            if (nodeMap.TryGetValue(connData.SourceNodeId, out var sourceNode) &&
                nodeMap.TryGetValue(connData.TargetNodeId, out var targetNode))
            {
                // Get port positions
                var sourcePos = sourceNode.GetPortCanvasPosition(connData.SourcePortIndex, false);
                var targetPos = targetNode.GetPortCanvasPosition(connData.TargetPortIndex, true);

                // Create connection
                var connection = new DragCanvasConnection
                {
                    SourceNode = sourceNode,
                    SourcePortIndex = connData.SourcePortIndex,
                    TargetNode = targetNode,
                    TargetPortIndex = connData.TargetPortIndex,
                    Stroke = Media.Brushes.Black,
                    StartPoint = sourcePos,
                    EndPoint = targetPos
                };

                _connections.Add(connection);
                Children.Insert(0, connection);

                // Update node connection tracking
                sourceNode.OnConnectionMade(connection, connData.SourcePortIndex, DragCanvasNode.PortSide.Right);
                targetNode.OnConnectionMade(connection, connData.TargetPortIndex, DragCanvasNode.PortSide.Left);

                // Raise connection created event
                var eventArgs = new ConnectionEventArgs(
                    ConnectionCreatedEvent,
                    sourceNode,
                    targetNode,
                    connData.SourcePortIndex,
                    connData.TargetPortIndex)
                {
                    SourcePortPosition = sourcePos,
                    TargetPortPosition = targetPos
                };

                RaiseEvent(eventArgs);
            }
        }
    }
}