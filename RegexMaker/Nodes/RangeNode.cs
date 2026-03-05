using RegexStringLibrary;
using System;
using System.Collections.Generic;

namespace RegexMaker.Nodes;
public class RangeNode : RgxNode
{
    public string CharStart { get; set; }
    public string CharEnd { get; set; }

    // Nodes created with the parameterless constructor are only exemplars and will never calculate
    public RangeNode()
        : base(RgxNodeType.Range)
    {
        CharStart = "a";
        CharEnd = "z";
    }

    public RangeNode(string chStart, string chEnd) : base(RgxNodeType.Range)
    {
        if (chStart.Length != 1 || chEnd.Length != 1)
        {
            throw new ArgumentException("CharStart and CharEnd must be single characters.");
        }
        CharStart = chStart;
        CharEnd = chEnd;
    }

    internal override string CalculateResult()
    {
        return Stex.Range(CharStart, CharEnd);
    }

    override public string RandomMatch()
    {
        char start = CharStart[0];
        char end = CharEnd[0];
        if (start > end)
        {
            throw new ArgumentException("CharStart must be less than or equal to CharEnd.");
        }
        int rangeSize = end - start + 1;
        char randomChar = (char)(start + random.Next(rangeSize));
        return randomChar.ToString();
    }

    public override IRgxNode Default()
    {
        return new RangeNode();
    }

    public override string Code(CodeCollector cc)
    {
        if (VariableName != null)
        {
            return VariableName;
        }

        var code = $@"Stex.Range(""{CharStart}"", ""{CharEnd}"")";

        return (CheckRename(cc) ? VariableName : code)!;
    }

    protected override void AddSerializationData(Dictionary<string, object?> data)
    {
        base.AddSerializationData(data);
        data["CharStart"] = CharStart;
        data["CharEnd"] = CharEnd;
    }

    protected override void RestoreSerializationData(Dictionary<string, System.Text.Json.JsonElement> data)
    {
        base.RestoreSerializationData(data);
        if (data.TryGetValue("CharStart", out var charStartElement))
        {
            CharStart = charStartElement.GetString() ?? "a";
        }
        if (data.TryGetValue("CharEnd", out var charEndElement))
        {
            CharEnd = charEndElement.GetString() ?? "z";
        }
    }
}

