using System;
using System.Collections.Generic;
using System.Text.Json;
using RegexStringLibrary;

namespace RegexMaker.Nodes;

public enum NumericType
{
    Integer,
    Unsigned,
    Float
}

public class NumericNode : RgxNode
{
    public NumericNode() : base(RgxNodeType.Numeric)
    {
        NumericType = NumericType.Integer;
        Parameters = [];
    }

    public NumericNode(NumericType numericType) : base(RgxNodeType.Numeric)
    {
        NumericType = numericType;
        Parameters = [];
    }

    public NumericType NumericType { get; set; }

    public override string DisplayName => NumericType.ToString();

    internal override string CalculateResult()
    {
        return NumericType switch
        {
            NumericType.Integer => Stex.Integer(),
            NumericType.Unsigned => Stex.UnsignedInteger(),
            NumericType.Float => Stex.Float(),
            _ => string.Empty
        };
    }

    public override string RandomMatch()
    {
        return NumericType switch
        {
            NumericType.Integer => (random.Next(2) == 0 ? "-" : "") + random.Next(1, 10000),
            NumericType.Unsigned => random.Next(0, 10000).ToString(),
            NumericType.Float => (random.Next(2) == 0 ? "-" : "") +
                                 (random.Next(0, 1000) + random.NextDouble()).ToString("F2"),
            _ => "0"
        };
    }

    public override IRgxNode Default()
    {
        return new NumericNode();
    }

    public override string RawCode(CodeCollector cc)
    {
        return NumericType switch
        {
            NumericType.Integer => "Stex.Integer()",
            NumericType.Unsigned => "Stex.UnsignedInteger()",
            NumericType.Float => "Stex.Float()",
            _ => string.Empty
        };
    }

    protected override void AddSerializationData(Dictionary<string, object?> data)
    {
        base.AddSerializationData(data);
        data["NumericType"] = Enum.GetName(NumericType);
    }

    protected override void RestoreSerializationData(Dictionary<string, JsonElement> data)
    {
        base.RestoreSerializationData(data);
        if (data.TryGetValue("NumericType", out var numericType))
            NumericType = Enum.Parse<NumericType>(numericType.GetString() ?? string.Empty);
    }
}