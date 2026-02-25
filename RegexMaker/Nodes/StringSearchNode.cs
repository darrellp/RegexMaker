namespace RegexMaker.Nodes;
public class StringSearchNode : RgxNode
{
    private string _searchString;
    public string SearchString { get => _searchString; set => _searchString=value; }

    // Nodes created with the parameterless constructor are only exemplars and will never calculate
    public StringSearchNode()
        : base(RgxNodeType.StringSearch)
    {
        SearchString = string.Empty;
    }

    public StringSearchNode(string searchString) : base(RgxNodeType.StringSearch, new IRgxNode[0])
    {
        SearchString = searchString;
    }

    internal override string CalculateResult()
    {
        // For a string search node, the result is just the search string itself.
        return SearchString;
    }

    override public string RandomMatch()
    {
        // The random match for a string search node is just the search string itself.
        return SearchString;
    }

    public override IRgxNode Default()
    {
        var ret = new StringSearchNode();
        ret.SearchString = "search";
        return ret;
    }

    public override string Name => "Literal";
}