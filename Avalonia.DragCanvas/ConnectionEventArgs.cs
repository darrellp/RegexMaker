using Avalonia.Interactivity;

namespace Avalonia.Controls;

/// <summary>
/// Event arguments for connection-related events
/// </summary>
public class ConnectionEventArgs : RoutedEventArgs
{
    public ConnectionEventArgs(RoutedEvent routedEvent, DragCanvasNode sourceNode, DragCanvasNode targetNode, int sourcePortIndex, int targetPortIndex)
        : base(routedEvent)
    {
        SourceNode = sourceNode;
        TargetNode = targetNode;
        SourcePortIndex = sourcePortIndex;
        TargetPortIndex = targetPortIndex;
    }

    /// <summary>
    /// The node with the right port (source of the connection)
    /// </summary>
    public DragCanvasNode SourceNode { get; }

    /// <summary>
    /// The node with the left port (target of the connection)
    /// </summary>
    public DragCanvasNode TargetNode { get; }

    /// <summary>
    /// Index of the port on the source node
    /// </summary>
    public int SourcePortIndex { get; }

    /// <summary>
    /// Index of the port on the target node
    /// </summary>
    public int TargetPortIndex { get; }

    /// <summary>
    /// Position of the source port in canvas coordinates
    /// </summary>
    public Point SourcePortPosition { get; set; }

    /// <summary>
    /// Position of the target port in canvas coordinates
    /// </summary>
    public Point TargetPortPosition { get; set; }
}