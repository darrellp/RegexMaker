using System.Linq;
using RegexStringLibrary;

namespace RegexMaker.Nodes;
internal class ConcatenateNode : RgxNode
{

    public ConcatenateNode(params IRgxNode[] parameters) : base(RgxNodeType.Concatenate, parameters) { }
    internal override string CalculateResult()
    {
        // Concatenate the results of all parameter nodes.
        return Stex.Cat(Parameters.Select(p => p.ProduceResult()).ToArray());
    }

    override public string RandomMatch()
    {
        // Concatenate the random matches of all parameter nodes.
        return string.Concat(Parameters.Select(p => p.RandomMatch()));
    }
}