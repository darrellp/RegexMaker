using RegexStringLibrary;
using System.Linq;

namespace RegexMaker.Nodes;
internal class ConcatenateNode : RgxNode
{

    // Nodes created with the parameterless constructor are only exemplars and will never calculate
    public ConcatenateNode()
        : base(RgxNodeType.Concatenate)
    {
    }

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

    public override IRgxNode Default()
    {
        return new ConcatenateNode();
    }
}