using Avalonia.Interactivity;

namespace Avalonia.DragCanvas;

/// <summary>
///     Event arguments for connection-related events
/// </summary>
public class ConnectionEventArgs(
    RoutedEvent routedEvent,
    DragCanvasNode sourceNode,
    DragCanvasNode targetNode,
    int sourcePortIndex,
    int targetPortIndex)
    : RoutedEventArgs(routedEvent)
{
    /// <summary>
    ///     The node with the right port (source of the connection)
    /// </summary>
    public DragCanvasNode SourceNode { get; } = sourceNode;

    /// <summary>
    ///     The node with the left port (target of the connection)
    /// </summary>
    public DragCanvasNode TargetNode { get; } = targetNode;

    /// <summary>
    ///     Index of the port on the source node
    /// </summary>
    public int SourcePortIndex { get; } = sourcePortIndex;

    /// <summary>
    ///     Index of the port on the target node
    /// </summary>
    public int TargetPortIndex { get; } = targetPortIndex;

    /// <summary>
    ///     Position of the source port in canvas coordinates
    /// </summary>
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public Point SourcePortPosition { get; set; }

    /// <summary>
    ///     Position of the target port in canvas coordinates
    /// </summary>
    public Point TargetPortPosition { get; set; }
    // ReSharper restore UnusedAutoPropertyAccessor.Global
}