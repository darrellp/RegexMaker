namespace Avalonia.DragCanvas;

/// <summary>
/// Interface for nodes that support serialization
/// </summary>
public interface ISerializableNode
{
    /// <summary>
    /// Gets application-specific data to serialize
    /// </summary>
    string SerializeApplicationData();

    /// <summary>
    /// Restores application-specific data from serialized form
    /// </summary>
    void DeserializeApplicationData(string data);

    /// <summary>
    /// Gets a type identifier for recreating this node
    /// </summary>
    string GetNodeTypeName();
}