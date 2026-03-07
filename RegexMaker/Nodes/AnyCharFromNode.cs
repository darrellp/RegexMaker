using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using RegexStringLibrary;

namespace RegexMaker.Nodes;

public class AnyCharFromNode : RgxNode
{
    // Nodes created with the parameterless constructor are only exemplars and will never calculate
    public AnyCharFromNode()
        : base(RgxNodeType.AnyCharFrom)
    {
        Parameters = [null];
        Chars = "A-Za-z0-9";
        NotIn = false;
    }

    public AnyCharFromNode(IList<IRgxNode> parameters) : base(RgxNodeType.AnyCharFrom, parameters.ToArray())
    {
        Chars = "A-Za-z0-9";
        NotIn = false;
    }

    public string Chars { get; set; }

    public bool NotIn { get; set; } // Added property

    public override string DisplayName => NotIn ? $"[^{Chars}]" : $"[{Chars}]";

    internal override string CalculateResult()
    {
        // Pass NotIn to Stex.AnyCharFrom if supported, otherwise handle here
        var result = string.Empty;

        if (NotIn)
            result = Stex.NotCharIn(Parameters.Select(p => p == null ? Chars : p.ProduceResult()).ToArray());
        else
            result = Stex.AnyCharFrom(Parameters.Select(p => p == null ? Chars : p.ProduceResult()).ToArray());
        return result;
    }

    public override string RandomMatch()
    {
        if (Parameters.Count == 0)
        {
            Debug.Assert(Chars != null);
            return new string([Chars[random.Next(Chars.Length)]]);
        }

        var chars = Parameters.Where(p => p is not null).Select(p => p?.RandomMatch()).ToArray();
        return chars[random.Next(chars.Length)] ?? string.Empty;
    }

    public override IRgxNode Default()
    {
        return new AnyCharFromNode();
    }

    public override string RawCode(CodeCollector cc)
    {
        // We're just going to calculate the result and put it in directly on the assumption that
        // calculating the input for this is way more complicated than the final output.
        return $"\"{CalculateResult()}\"";
    }

    protected override void AddSerializationData(Dictionary<string, object?> data)
    {
        base.AddSerializationData(data);
        data["Chars"] = Chars;
        data["NotIn"] = NotIn; // Serialize NotIn
    }

    protected override void RestoreSerializationData(Dictionary<string, JsonElement> data)
    {
        base.RestoreSerializationData(data);
        if (data.TryGetValue("Chars", out var charsElement)) Chars = charsElement.GetString() ?? "A-Za-z0-9";
        if (data.TryGetValue("NotIn", out var notInElement)) NotIn = notInElement.GetBoolean();
    }
}