using System.Collections.Generic;
using System.Text.Json;

namespace Avalonia.DragCanvas;

/// <summary>
/// Data transfer object for serializing/deserializing the entire canvas state
/// </summary>
public class CanvasSerializationData
{
    /// <summary>
    /// Serialized node data
    /// </summary>
    public List<NodeSerializationData> Nodes { get; set; } = new();

    /// <summary>
    /// Connection data (indices refer to nodes in the Nodes list)
    /// </summary>
    public List<ConnectionSerializationData> Connections { get; set; } = new();
}

/// <summary>
/// Base class for serializing node data
/// </summary>
public class NodeSerializationData
{
    /// <summary>
    /// Unique identifier for cross-referencing connections
    /// </summary>
    public int NodeId { get; set; }

    /// <summary>
    /// X position on canvas
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y position on canvas
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Number of left ports
    /// </summary>
    public int PortCtLeft { get; set; }

    /// <summary>
    /// Number of right ports
    /// </summary>
    public int PortCtRight { get; set; }

    /// <summary>
    /// Application-specific node data (stored as JsonElement for proper JSON nesting)
    /// </summary>
    public JsonElement? ApplicationData { get; set; }

    /// <summary>
    /// Type identifier for recreating the correct node type
    /// </summary>
    public string? NodeTypeName { get; set; }
}

/// <summary>
/// Data for serializing connections between nodes
/// </summary>
public class ConnectionSerializationData
{
    /// <summary>
    /// ID of the source node
    /// </summary>
    public int SourceNodeId { get; set; }

    /// <summary>
    /// ID of the target node
    /// </summary>
    public int TargetNodeId { get; set; }

    /// <summary>
    /// Index of the source port
    /// </summary>
    public int SourcePortIndex { get; set; }

    /// <summary>
    /// Index of the target port
    /// </summary>
    public int TargetPortIndex { get; set; }
}

