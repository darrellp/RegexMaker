namespace RegexMaker.Nodes;
internal class StringSearchNode : RgxNode
{
    private string _searchString;

    // Nodes created with the parameterless constructor are only exemplars and will never calculate
    public StringSearchNode()
        : base(RgxNodeType.StringSearch)
    {
        _searchString = string.Empty;
    }

    public StringSearchNode(string searchString) : base(RgxNodeType.StringSearch, new IRgxNode[0])
    {
        _searchString = searchString;
    }

    internal override string CalculateResult()
    {
        // For a string search node, the result is just the search string itself.
        return _searchString;
    }

    override public string RandomMatch()
    {
        // The random match for a string search node is just the search string itself.
        return _searchString;
    }

    public override IRgxNode Default()
    {
        return new StringSearchNode();
    }

    public override string Name => "Literal";
}