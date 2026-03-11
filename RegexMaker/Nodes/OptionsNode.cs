using System;
using System.Collections.Generic;
using System.Text.Json;
using RegexStringLibrary;

namespace RegexMaker.Nodes;

public enum TriState
{
    On,
    Off,
    Dflt
}

public class OptionsNode : RgxNode
{
    public OptionsNode() : base(RgxNodeType.Options)
    {
        CaseSensitiveState = TriState.Dflt;
        MultilineState = TriState.Dflt;
        Parameters.Add(null);
    }

    public OptionsNode(IRgxNode parameter, TriState caseSensitive = TriState.Dflt, TriState multiline = TriState.Dflt)
        : base(RgxNodeType.Options, parameter)
    {
        CaseSensitiveState = caseSensitive;
        MultilineState = multiline;
    }

    public TriState CaseSensitiveState { get; set; }
    public TriState MultilineState { get; set; }

    public override string DisplayName
    {
        get
        {
            var parts = new List<string>();
            if (CaseSensitiveState != TriState.Dflt)
                parts.Add(CaseSensitiveState == TriState.On ? "CS" : "CI");
            if (MultilineState != TriState.Dflt)
                parts.Add(MultilineState == TriState.On ? "ML" : "SL");
            return parts.Count > 0 ? $"Opt({string.Join(",", parts)})" : "Options";
        }
    }

    internal override string CalculateResult()
    {
        var result = Parameters[0] == null ? "options Value" : Parameters[0]!.ProduceResult();

        if (CaseSensitiveState != TriState.Dflt) result = result.CaseSensitive(CaseSensitiveState == TriState.On);

        if (MultilineState != TriState.Dflt) result = result.Multiline(MultilineState == TriState.On);

        return result;
    }

    public override string RandomMatch()
    {
        return Parameters[0] == null ? string.Empty : Parameters[0]!.RandomMatch();
    }

    public override IRgxNode Default()
    {
        return new OptionsNode();
    }

    public override string RawCode(CodeCollector cc)
    {
        var input = (Parameters[0] as RgxNode)?.Code(cc) ?? "\"\"";

        var code = input;
        if (CaseSensitiveState != TriState.Dflt)
            code = $"{code}.CaseSensitive({(CaseSensitiveState == TriState.On ? "true" : "false")})";
        if (MultilineState != TriState.Dflt)
            code = $"{code}.Multiline({(MultilineState == TriState.On ? "true" : "false")})";

        return code;
    }

    protected override void AddSerializationData(Dictionary<string, object?> data)
    {
        base.AddSerializationData(data);
        data["CaseSensitiveState"] = Enum.GetName(CaseSensitiveState);
        data["MultilineState"] = Enum.GetName(MultilineState);
    }

    protected override void RestoreSerializationData(Dictionary<string, JsonElement> data)
    {
        base.RestoreSerializationData(data);
        if (data.TryGetValue("CaseSensitiveState", out var csElement))
            CaseSensitiveState = Enum.Parse<TriState>(csElement.GetString() ?? string.Empty);
        if (data.TryGetValue("MultilineState", out var mlElement))
            MultilineState = Enum.Parse<TriState>(mlElement.GetString() ?? string.Empty);
    }
}