using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Controls;

public class DragCanvas : Canvas
{
    private Control? elementBeingDragged;
    private Point origCursorLocation;
    private double origHorizOffset, origVertOffset;
    private bool modifyLeftOffset, modifyTopOffset;
    
    // Connection management
    private DragCanvasConnection? _temporaryConnection;
    private DragCanvasNode? _connectionSourceNode;
    private int _connectionSourcePortIndex;
    private bool _connectionSourceIsLeftSide;
    private readonly List<DragCanvasConnection> _connections = new();
    private DragCanvasNode? _lastHoveredNodeDuringConnection;
    private (int index, bool isLeftSide)? _lastHoveredPortDuringConnection;

    public bool IsDragInProgress { get; private set; }

    // Routed event for connections created
    public static readonly RoutedEvent<ConnectionEventArgs> ConnectionCreatedEvent =
        RoutedEvent.Register<DragCanvas, ConnectionEventArgs>(
            "ConnectionCreated",
            RoutingStrategies.Bubble);

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

    public DragCanvas()
    {
        // Subscribe to port clicked events
        AddHandler(DragCanvasNode.PortClickedEvent, OnPortClicked);
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
        
        // Find canvas child
        if (e.Source is Visual visual)
        {
            elementBeingDragged = FindCanvasChild(visual);
            if (elementBeingDragged == null) return;

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
        
        if (hoveredNode != null && hoveredNode != _connectionSourceNode)
        {
            hoveredPort = FindPortAtPosition(hoveredNode, canvasPosition);
            
            // Only consider valid ports (opposite side from source)
            if (hoveredPort.HasValue)
            {
                var (_, isLeftSide) = hoveredPort.Value;
                if (isLeftSide == _connectionSourceIsLeftSide)
                {
                    // Same side - not valid
                    hoveredPort = null;
                }
            }
        }
        
        // Check if hover state changed
        bool changed = false;
        
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

                    // Validate: ports must be on opposite sides
                    if (_connectionSourceIsLeftSide != targetIsLeftSide)
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

        elementBeingDragged = null;
        IsDragInProgress = false;
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

    private void UpdateConnectionsForNode(DragCanvasNode node)
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
}