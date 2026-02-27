using RegexStringLibrary;
using System.Linq;

namespace RegexMaker.Nodes;

// DERIVE FROM RgxNode
public class AnyOfNode : RgxNode
{
    public AnyOfNode() : base(RgxNodeType.AnyOf)
    {
        Parameters = [null, null];
    }

    // This should just calculate.  Caching is done by 
    internal override string CalculateResult()
    {
        // Concatenate the results of all parameter nodes.
        return Stex.AnyOf(Parameters.Select(p => p == null ? string.Empty : p.ProduceResult()).ToArray());
    }

    // This should generate a random string that will match this pattern.  It should use the static random variable from the base class for any random 
    // number generation to ensure that all nodes use the same source of randomness.
    override public string RandomMatch()
    {
        var iPick = random.Next(Parameters.Count);
        return (Parameters[iPick]?.RandomMatch() ?? string.Empty);
    }

    public override IRgxNode Default()
    {
        return new AnyOfNode();
    }

    // DisplayName will default to "AnyOf" so we don't need to override.
}
