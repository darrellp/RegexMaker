using RegexStringLibrary;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RegexMaker.Nodes;
internal class RepeatNode : RgxNode
{
    public int Least { get; private set; }
    public int Most { get; private set; }
    public bool IsLazy { get; private set; }

    // Nodes created with the parameterless constructor are only exemplars and will never calculate
    public RepeatNode()
        : base(RgxNodeType.Repeat)
    {
        Least = Most = 0;
        IsLazy = false;
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
        return Parameters[0].ProduceResult().Rep(Least, Most, IsLazy);
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
        return string.Concat(Enumerable.Repeat(0, repeatCount).Select(i => Parameters[0].RandomMatch()));
    }

    public override IRgxNode Default()
    {
        return new RepeatNode();
    }
}
