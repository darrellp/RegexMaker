using Avalonia.Interactivity;

namespace Avalonia.DragCanvas;

/// <summary>
///     Event arguments for node selection events
/// </summary>
public class NodeSelectedEventArgs(RoutedEvent routedEvent, DragCanvasNode selectedNode) : RoutedEventArgs(routedEvent)
{
    /// <summary>
    ///     The node that was selected
    /// </summary>
    public DragCanvasNode SelectedNode { get; } = selectedNode;
}