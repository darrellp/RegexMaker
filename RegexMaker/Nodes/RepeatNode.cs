using Avalonia;
using RegexStringLibrary;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RegexMaker.Nodes;
public class RepeatNode : RgxNode
{
    public int Least { get; set; }
    public int Most { get; set; }
    public bool IsLazy { get; set; }

    // Nodes created with the parameterless constructor are only exemplars and will never calculate
    public RepeatNode()
        : base(RgxNodeType.Repeat)
    {
        Least = 0;
        Most = 1;
        IsLazy = false;
        Parameters.Add(null);
    }

    public RepeatNode(IList<IRgxNode> parameters, int least, int most = -1, bool isLazy = false) : base(RgxNodeType.Repeat, parameters.ToArray())
    {
        if (parameters.Count != 1)
        {
            throw new ArgumentException("RepeatNode must have exactly one parameter node.");
        }
        Least = least;
        Most = most;
        IsLazy = isLazy;
    }

    internal override string CalculateResult()
    {
        // Concatenate the results of all parameter nodes.
        // NOTE: We'd normally have to use Stex.Cat here but in Polyglot all the static methods are just available as top-level functions so we can call Cat directly.
        return (Parameters[0] == null ? "repeat Value" : Parameters[0]?.ProduceResult()??string.Empty).Rep(Least, Most, IsLazy);
    }

    override public string RandomMatch()
    {
        int repeatCount;
        if (Most == -1)
        {
            // If Most is -1, we can repeat any number of times greater than or equal to Least. For random generation, we can choose a reasonable upper limit.
            // TODO: We might want to make this upper limit configurable or based on the length of the pattern being repeated.
            repeatCount = Least + random.Next(10); // Arbitrary upper limit of 10 for random generation.
        }
        else
        {
            repeatCount = random.Next(Least, Most + 1);
        }
        // return string.Concat(Enumerable.Repeat(Parameters[0].RandomMatch(), repeatCount));
        return string.Concat(Enumerable.Repeat(0, repeatCount).Select(i => Parameters[0]?.RandomMatch()??string.Empty));
    }

    public override IRgxNode Default()
    {
        return new RepeatNode();
    }

    public override string RawCode(CodeCollector cc)
    {
        var input = (Parameters[0] as RgxNode)?.Code(cc) ?? "\"\"";

        var lazyArg = IsLazy ? ", true" : "";
        var repCode = (Least, Most) switch
        {
            (0, 1) => $"{input}.Rep(0, 1{lazyArg})",
            (0, -1) => $"{input}.Rep(0, -1{lazyArg})",
            (1, -1) => $"{input}.Rep(1, -1{lazyArg})",
            (var least, -1) => $"{input}.Rep({least}, -1{lazyArg})",
            (var least, var most) when least == most => $"{input}.Rep({least}, {most}{lazyArg})",
            (var least, var most) => $"{input}.Rep({least}, {most}{lazyArg})"
        };

        return repCode!;
    }

    public override string DisplayName
    {
        get
        {
            var rep = (Least, Most) switch
            {
                (0, 1) => "Opt",
                (0, -1) => "*",
                (1, -1) => "+",
                (var least, -1) => $"Rep({least},)",
                (var least, var most) when least == most => $"Rep({least})",
                (var least, var most) => $"Rep({least}, {most})"
            };
            return rep;
        }
    }

    protected override void AddSerializationData(Dictionary<string, object?> data)
    {
        base.AddSerializationData(data);
        data["Least"] = Least;
        data["Most"] = Most;
        data["IsLazy"] = IsLazy;
    }

    protected override void RestoreSerializationData(Dictionary<string, System.Text.Json.JsonElement> data)
    {
        base.RestoreSerializationData(data);
        if (data.TryGetValue("Least", out var leastElement))
        {
            Least = leastElement.GetInt32();
        }
        if (data.TryGetValue("Most", out var mostElement))
        {
            Most = mostElement.GetInt32();
        }
        if (data.TryGetValue("IsLazy", out var isLazyElement))
        {
            IsLazy = isLazyElement.GetBoolean();
        }
    }
}
