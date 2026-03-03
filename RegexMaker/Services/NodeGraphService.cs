using RegexMaker.Nodes;
using System.Linq;

namespace RegexMaker.Services;

/// <summary>
/// Manages RgxNode graph connections — pure domain logic, no UI dependencies.
/// </summary>
public static class NodeGraphService
{
    public static void Connect(RgxNode source, RgxNode target, int targetPortIndex)
    {
        target.Parameters[targetPortIndex] = source;
        source.Parents.Add(target);
        target.MakeDirty();
    }

    public static void Disconnect(RgxNode source, RgxNode target, int targetPortIndex)
    {
        if (targetPortIndex >= 0 && targetPortIndex < target.Parameters.Count)
        {
            target.Parameters[targetPortIndex] = null;
        }
        source.Parents.Remove(target);
        target.MakeDirty();
    }

    public static void DeleteNode(RgxNode node)
    {
        // Remove from all parents
        foreach (var parent in node.Parents.ToList())
        {
            if (parent is RgxNode parentNode)
            {
                for (int i = 0; i < parentNode.Parameters.Count; i++)
                {
                    if (parentNode.Parameters[i] == node)
                        parentNode.Parameters[i] = null;
                }
                parentNode.MakeDirty();
            }
        }
        node.Parents.Clear();

        // Disconnect from children
        for (int i = 0; i < node.Parameters.Count; i++)
        {
            if (node.Parameters[i] is RgxNode child)
            {
                child.Parents.Remove(node);
                child.MakeDirty();
            }
            node.Parameters[i] = null;
        }
    }

    public static void SetPortCount(RgxNode node, int newCount)
    {
        int currentCount = node.Parameters.Count;

        if (newCount > currentCount)
        {
            for (int i = 0; i < newCount - currentCount; i++)
                node.Parameters.Add(null);
        }
        else if (newCount < currentCount)
        {
            for (int i = newCount; i < currentCount; i++)
            {
                if (node.Parameters[i] is RgxNode child)
                    child.Parents.Remove(node);
                node.Parameters[i] = null;
            }
            for (int i = 0; i < currentCount - newCount; i++)
                node.Parameters.RemoveAt(node.Parameters.Count - 1);
        }

        node.MakeDirty();
    }
}