using System.Collections.Generic;
using System.Text;

namespace RegexMaker.Nodes;

// The strategy for gathering code here is in two parts.  One part is that nodes gather code
// from their parameters and put it together in whatever way makes sense for that node using Stex.
// This produces a string which is either a single variable name if the node produced code that was
// stored in a variable or the stex code to produce the proper result based on what came from
// the parameters.
// The other part is that during this process some nodes need to be made into "variables".  These
// are the nodes that have a named or unnamed capture and nodes which have more than one parent to
// avoid recalculation on those nodes.  The nodes themselves determine this and return either code
// or a variable name.

public class CodeCollector(RgxNode node)
{
    private List<string> _codeLines = [];
    private static int _varCounter;
    
    public string Result { get; private set; } = "";

    public string NextVariable(string baseName)
    {
        return $"__{baseName}__{_varCounter++}";
    }

    public void AddCode(string varName, string code)
    {
        _codeLines.Add($"{varName} = {code};");
    }

    public void GatherCode()
    {
        _varCounter = 0;
        _codeLines = [];
        node.InitializeForCode();
        var result = node.Code(this);

        var sb = new StringBuilder();
        foreach (var line in _codeLines)
        {
            sb.AppendLine(line);
        }
        sb.AppendLine($"var Result = {result};");
        Result = sb.ToString();
    }
}