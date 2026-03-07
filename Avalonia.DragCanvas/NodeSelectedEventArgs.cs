using Avalonia.Interactivity;

namespace Avalonia.DragCanvas;

/// <summary>
///     Event arguments for node selection events
/// </summary>
public class NodeSelectedEventArgs : RoutedEventArgs
{
    public NodeSelectedEventArgs(RoutedEvent routedEvent, DragCanvasNode selectedNode)
        : base(routedEvent)
    {
        SelectedNode = selectedNode;
    }

    /// <summary>
    ///     The node that was selected
    /// </summary>
    public DragCanvasNode SelectedNode { get; }
}