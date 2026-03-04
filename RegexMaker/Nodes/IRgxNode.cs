using System.Collections.Generic;

namespace RegexMaker.Nodes;

public enum RgxNodeType
{
    StringSearch,
    Concatenate,
    Repeat,
    PatternStart,
    PatternEnd,
    AnyCharFrom,
    AnyOf,
    Range,
    CharClass,
    Named,
    Date,
    AnyWordFrom,
    BackReference,
    Numeric,
    Options,
};

public interface IRgxNode
{
    public int ID { get; }
    public RgxNodeType NodeType { get; }
    public IList<IRgxNode> Parameters { get; }
    public string Name { get; }

    // This method is responsible for producing the result of the node based on its type and parameters.
    public string ProduceResult();
    public string RandomMatch();
    public IRgxNode Default();
}
