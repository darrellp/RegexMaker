using System.Collections.Generic;
using System.Linq;
using RegexStringLibrary;

namespace RegexMaker.Nodes;

// DERIVE FROM RgxNode
public class AnyOfNode : RgxNode
{
    public AnyOfNode() : base(RgxNodeType.AnyOf)
    {
        Parameters = [null, null];
    }

    public AnyOfNode(params IRgxNode[] parameters) : base(RgxNodeType.Concatenate, parameters)
    {
    }

    // This should just calculate.  Caching is done by RgxNode
    internal override string CalculateResult()
    {
        // Concatenate the results of all parameter nodes.
        return Stex.AnyOf(Parameters.Select(p => p == null ? string.Empty : p.ProduceResult()).ToArray());
    }

    // This should generate a random string that will match this pattern.  It should use the static random variable from the base class for any random 
    // number generation to ensure that all nodes use the same source of randomness.
    public override string RandomMatch()
    {
        var iPick = random.Next(Parameters.Count);
        return Parameters[iPick]?.RandomMatch() ?? string.Empty;
    }

    public override IRgxNode Default()
    {
        return new AnyOfNode();
    }

    public override string RawCode(CodeCollector cc)
    {
        if (VariableName != null) return VariableName;
        var inputList = new List<string>();
        foreach (var irgx in Parameters)
        {
            if (irgx is null) continue;
            var node = irgx as RgxNode;
            if (node is null) continue;


            var insert = node switch
            {
                LiteralNode literal => $"\"{literal.SearchString}\"",
                NamedNode named => named.Code(cc),
                _ => node.VariableName
            };
            if (insert is null)
            {
                node.CheckRename(cc, true);
                insert = node.VariableName;
            }

            inputList.Add(insert);
        }

        var code = $"Stex.AnyOf({string.Join(", ", inputList)})";
        return code;
    }
    // DisplayName will default to "AnyOf" so we don't need to override.
}