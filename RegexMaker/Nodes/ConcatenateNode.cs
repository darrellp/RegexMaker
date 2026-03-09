using System.Collections.Generic;
using System.Linq;
using RegexStringLibrary;

namespace RegexMaker.Nodes;

public class ConcatenateNode : RgxNode
{
    // Nodes created with the parameterless constructor are only exemplars and will never calculate
    public ConcatenateNode()
        : base(RgxNodeType.Concatenate)
    {
        Parameters = [null, null];
    }

    public ConcatenateNode(params IRgxNode[] parameters) : base(RgxNodeType.Concatenate, parameters)
    {
    }

    internal override string CalculateResult()
    {
        // Concatenate the results of all parameter nodes.
        return Stex.Cat(Parameters.Select(p => p == null ? string.Empty : p.ProduceResult()).ToArray());
    }

    public override string RandomMatch()
    {
        // Concatenate the random matches of all parameter nodes.
        return string.Concat(Parameters.Select(p => p?.RandomMatch() ?? string.Empty));
    }

    public override IRgxNode Default()
    {
        return new ConcatenateNode();
    }

    public override string RawCode(CodeCollector cc)
    {
        var inputList = new List<string>();
        foreach (var irgx in Parameters)
        {
            if (irgx is null) continue;
            var node = irgx as RgxNode;
            if (node is null) continue;

            var insert = node switch
            {
                LiteralNode literal => $"@\"{literal.CalculateResult()}\"",
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

        return $"Stex.Cat({string.Join(", ", inputList)})";
    }
}