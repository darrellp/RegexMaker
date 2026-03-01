using Avalonia;
using RegexStringLibrary;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RegexMaker.Nodes;

// DERIVE FROM RgxNode
public class DateNode : RgxNode
{
    // Declare any properties specific to this node type here. For example:
    public bool IsAmerican { get; set; }

    // Must have a parameterless constructor for exemplar creation. This constructor should set default values for properties and parameters.
    // Pass the appropriate RgxNodeType to the base constructor.
    public DateNode() : base(RgxNodeType.Date)
    {
        // Set default values for properties and parameters. For example:
        IsAmerican = true;
        Parameters = [];
    }

    // This should just calculate.  Caching is done by 
    internal override string CalculateResult()
    {
        return IsAmerican ? Stex.DateAmerican : Stex.DateEuropean;
    }

    // This should generate a random string that will match this pattern.  It should use the static random variable from the base class for any random 
    // number generation to ensure that all nodes use the same source of randomness.
    override public string RandomMatch()
    {
        return "Date doesn't random match";
    }

    public override IRgxNode Default()
    {
        return new DateNode();
    }

    protected override void AddSerializationData(Dictionary<string, object?> data)
    {
        base.AddSerializationData(data);
        data["IsAmerican"] = IsAmerican;
    }

    protected override void RestoreSerializationData(Dictionary<string, System.Text.Json.JsonElement> data)
    {
        base.RestoreSerializationData(data);
        if (data.TryGetValue("IsAmerican", out var isAmerican))
        {
            IsAmerican = isAmerican.GetBoolean();
        }
    }

}
