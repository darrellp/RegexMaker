using System.Collections.Generic;
using System.Linq;
using RegexStringLibrary;

namespace RegexMaker.Nodes;
internal class AnyCharFromNode : RgxNode
{
    public AnyCharFromNode(IList<IRgxNode> parameters) : base(RgxNodeType.AnyCharFrom, parameters.ToArray())
    {
    }

    internal override string CalculateResult()
    {
        return Stex.AnyCharFrom(Parameters.Select(p => p.ProduceResult()).ToArray());
    }

    override public string RandomMatch()
    {
        // TODO: This will give equal weight to ',' and 'A-Z' for example, which is not ideal. We should ideally give more weight to the ranges.
        var chars = Parameters.Select(p => p.RandomMatch()).ToArray();
        return chars[random.Next(chars.Length)];
    }
}
