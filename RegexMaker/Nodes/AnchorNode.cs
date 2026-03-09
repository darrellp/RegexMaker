using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using RegexStringLibrary;

namespace RegexMaker.Nodes;

public class AnchorNode : RgxNode
{
    public AnchorNode() : base(RgxNodeType.Anchor)
    {
        IsPositive = true;
        IsAhead = true;
        Parameters = [null];
    }

    public bool IsPositive { get; set; }
    public bool IsAhead { get; set; }

    public override string Name => "Anchor";

    public override string DisplayName =>
        (IsPositive ? "Pos" : "Neg") + (IsAhead ? "Ahead" : "Behind");

    internal override string CalculateResult()
    {
        string input = (Parameters[0] == null ? "repeat Value" : Parameters[0]?.ProduceResult() ?? string.Empty);
        return (IsPositive, IsAhead) switch
        {
            (true, true) => input.PosLookAhead(),
            (false, true) => input.NegLookAhead(),
            (true, false) => input.PosLookBehind(),
            (false, false) => input.NegLookBehind(),
        };
    }

    public override string RandomMatch()
    {
        return string.Empty;
    }

    public override IRgxNode Default()
    {
        return new AnchorNode();
    }

    public override string RawCode(CodeCollector cc)
    {
        var input = (Parameters[0] as RgxNode)?.Code(cc) ?? "\"\"";
        return (IsPositive, IsAhead) switch
        {
            (true, true) => $"{input}.PosLookAhead()",
            (false, true) => $"{input}.NegLookAhead()",
            (true, false) => $"{input}.PosLookBehind()",
            (false, false) => $"{input}.NegLookBehind()",
        };
    }

    protected override void AddSerializationData(Dictionary<string, object?> data)
    {
        base.AddSerializationData(data);
        data["IsPositive"] = IsPositive;
        data["IsAhead"] = IsAhead;
    }

    protected override void RestoreSerializationData(Dictionary<string, JsonElement> data)
    {
        base.RestoreSerializationData(data);
        if (data.TryGetValue("IsPositive", out var posElement))
            IsPositive = posElement.GetBoolean();
        if (data.TryGetValue("IsAhead", out var aheadElement))
            IsAhead = aheadElement.GetBoolean();
    }
}