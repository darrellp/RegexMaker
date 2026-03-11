using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using RegexStringLibrary;

namespace RegexMaker.Nodes;

// DERIVE FROM RgxNode
public class NamedNode : RgxNode
{
    // Must have a parameterless constructor for exemplar creation. This constructor should set default values for properties and parameters.
    // Pass the appropriate RgxNodeType to the base constructor.
    public NamedNode() : base(RgxNodeType.Named)
    {
        GroupName = string.Empty;
        Parameters = [null];
    }

    // Declare any properties specific to this node type here. For example:
    public string GroupName { get; set; }

    public override string Name => "Capture";

    // This is the string that will be displayed on the node control in the UI.
    public override string DisplayName => GroupName == string.Empty ? "Capture" : $"Named {GroupName}";

    // This should just calculate.  Caching is done by 
    internal override string CalculateResult()
    {
        if (Parameters == null || Parameters.Count == 0 || Parameters[0] == null)
        {
            Debug.WriteLine("Warning: NamedNode has no parameters. Returning empty string.");
            return string.Empty;
        }

        return (Parameters[0] == null ? "" : Parameters[0]!.ProduceResult()).Named(GroupName);
    }

    // This should generate a random string that will match this pattern.  It should use the static random variable from the base class for any random 
    // number generation to ensure that all nodes use the same source of randomness.
    public override string RandomMatch()
    {
        return Parameters[0]!.RandomMatch();
    }

    public override IRgxNode Default()
    {
        return new NamedNode();
    }

    public override string Code(CodeCollector cc)
    {
        var isNamedCapture = GroupName != string.Empty;
        if (VariableName != null)
            return VariableName;
        else if (!string.IsNullOrEmpty(UserVariableName))
            VariableName = UserVariableName;
        else if (isNamedCapture)
            VariableName = GroupName;
        else
            VariableName = cc.NextVariable("Capture");
        var input = (Parameters[0] as RgxNode)?.Code(cc) ?? "";
        var code = isNamedCapture ? $@"{input}.Named(""{GroupName}"")" : $"Stex.Capture({input})";
        Debug.Assert(VariableName != null, nameof(VariableName) + " != null");
        cc.AddCode(VariableName, code);
        return VariableName;
    }

    protected override void AddSerializationData(Dictionary<string, object?> data)
    {
        base.AddSerializationData(data);
        data["Name"] = GroupName;
    }

    protected override void RestoreSerializationData(Dictionary<string, JsonElement> data)
    {
        base.RestoreSerializationData(data);
        if (data.TryGetValue("Name", out var namedElement)) GroupName = namedElement.GetString() ?? string.Empty;
    }
}