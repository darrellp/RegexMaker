using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace RegexMaker.Nodes;

public class LiteralNode : RgxNode
{
    // Nodes created with the parameterless constructor are only exemplars and will never calculate
    public LiteralNode()
        : base(RgxNodeType.StringSearch)
    {
        SearchString = string.Empty;
        Debug.Assert(SearchString != null);
        AutoEscape = true;
    }

    public LiteralNode(string searchString) : base(RgxNodeType.StringSearch)
    {
        SearchString = searchString;
    }

    public string SearchString { get; set; }

    public bool AutoEscape { get; set; } = true;

    public override string Name => "Literal";
    public override string DisplayName => $"\"{(AutoEscape ? AutoEscapeString(SearchString) : SearchString)}\"";

    internal override string CalculateResult()
    {
        // For a string search node, the result is just the search string itself.
        return AutoEscape ? AutoEscapeString(SearchString) : SearchString;
    }

    public override string RandomMatch()
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

    public override string RawCode(CodeCollector cc)
    {
        return $"\"{SearchString}\"";
    }


    protected override void AddSerializationData(Dictionary<string, object?> data)
    {
        base.AddSerializationData(data);
        data["SearchString"] = SearchString;
        data["AutoEscape"] = AutoEscape;
    }

    protected override void RestoreSerializationData(Dictionary<string, JsonElement> data)
    {
        base.RestoreSerializationData(data);
        if (data.TryGetValue("SearchString", out var searchStringElement))
            SearchString = searchStringElement.GetString() ?? string.Empty;
        if (data.TryGetValue("AutoEscape", out var autoEscapeElement)) AutoEscape = autoEscapeElement.GetBoolean();
    }

    private static string AutoEscapeString(string input)
    {
        // List of regex special characters that need to be escaped
        var specialChars = new HashSet<char>
            { '.', '^', '$', '*', '+', '?', '(', ')', '[', ']', '{', '}', '\\', '|', '/' };
        var escapedString = new StringBuilder();
        foreach (var c in input)
        {
            if (specialChars.Contains(c)) escapedString.Append('\\'); // Escape the special character
            escapedString.Append(c);
        }

        return escapedString.ToString();
    }
}