using RegexStringLibrary;
using System.Collections.Generic;
using System.Linq;

namespace RegexMaker.Nodes;
public class AnyCharFromNode : RgxNode
{
    public string Chars { get; set; }

    // Nodes created with the parameterless constructor are only exemplars and will never calculate
    public AnyCharFromNode()
        : base(RgxNodeType.AnyCharFrom)
    {
        Parameters = [null];
        Chars = "A-Za-z0-9";
    }

    public AnyCharFromNode(IList<IRgxNode> parameters) : base(RgxNodeType.AnyCharFrom, parameters.ToArray())
    {
        Chars = "A-Za-z0-9";
    }

    internal override string CalculateResult()
    {
        return Stex.AnyCharFrom(Parameters.Select(p => p == null ? Chars : p.ProduceResult()).ToArray());
    }

    override public string RandomMatch()
    {
        // TODO: This will give equal weight to ',' and 'A-Z' for example, which is not ideal. We should ideally give more weight to the ranges.
        var chars = Parameters.Select(p => p.RandomMatch()).ToArray();
        return chars[random.Next(chars.Length)];
    }

    public override string DisplayName => $"[{Chars}]";

    public override IRgxNode Default()
    {
        return new AnyCharFromNode();
    }

    protected override void AddSerializationData(Dictionary<string, object?> data)
    {
        base.AddSerializationData(data);
        data["Chars"] = Chars;
    }

    protected override void RestoreSerializationData(Dictionary<string, System.Text.Json.JsonElement> data)
    {
        base.RestoreSerializationData(data);
        if (data.TryGetValue("Chars", out var charsElement))
        {
            Chars = charsElement.GetString() ?? "A-Za-z0-9";
        }
    }
}
