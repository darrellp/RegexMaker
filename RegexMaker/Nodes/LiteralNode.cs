using System.Text.Json.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;

namespace RegexMaker.Nodes;
public class LiteralNode : RgxNode
{
    private string _searchString;
    public string SearchString { get => _searchString; set => _searchString=value; }

    // Nodes created with the parameterless constructor are only exemplars and will never calculate
    public LiteralNode()
        : base(RgxNodeType.StringSearch)
    {
        SearchString = string.Empty;
    }

    public LiteralNode(string searchString) : base(RgxNodeType.StringSearch, new IRgxNode[0])
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
        var ret = new LiteralNode();
        ret.SearchString = "search";
        return ret;
    }

    public override string Name => "Literal";
    public override string DisplayName => $"\"{_searchString}\"";

    protected override void AddSerializationData(Dictionary<string, object?> data)
    {
        base.AddSerializationData(data);
        data["SearchString"] = SearchString;
    }

    protected override void RestoreSerializationData(Dictionary<string, JsonElement> data)
    {
        base.RestoreSerializationData(data);
        if (data.TryGetValue("SearchString", out var searchStringElement))
        {
            SearchString = searchStringElement.GetString() ?? string.Empty;
        }
    }
}