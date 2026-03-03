using RegexStringLibrary;
using System;
using System.Collections.Generic;

namespace RegexMaker.Nodes;

// DERIVE FROM RgxNode
public class NamedNode : RgxNode
{
    // Declare any properties specific to this node type here. For example:
    public string GroupName { get; set; }

    // Must have a parameterless constructor for exemplar creation. This constructor should set default values for properties and parameters.
    // Pass the appropriate RgxNodeType to the base constructor.
    public NamedNode() : base(RgxNodeType.Named)
    {
        GroupName = String.Empty;
        Parameters = [null];
    }

    // This should just calculate.  Caching is done by 
    internal override string CalculateResult()
    {
        return (Parameters[0] == null ? "" : Parameters[0]!.ProduceResult()).Named(GroupName);
    }

    // This should generate a random string that will match this pattern.  It should use the static random variable from the base class for any random 
    // number generation to ensure that all nodes use the same source of randomness.
    override public string RandomMatch()
    {
        return Parameters[0]!.RandomMatch();
    }

    public override string Name => "Capture";

    public override IRgxNode Default()
    {
        return new NamedNode();
    }

    // This is the string that will be displayed on the node control in the UI.
    public override string DisplayName
    {
        get
        {
            return GroupName == String.Empty ? "Capture" : $"Named {GroupName}";
        }
    }

    protected override void AddSerializationData(Dictionary<string, object?> data)
    {
        base.AddSerializationData(data);
        data["Name"] = GroupName;
    }

    protected override void RestoreSerializationData(Dictionary<string, System.Text.Json.JsonElement> data)
    {
        base.RestoreSerializationData(data);
        if (data.TryGetValue("Name", out var namedElement))
        {
            GroupName = namedElement.GetString()??String.Empty;
        }
    }

}
