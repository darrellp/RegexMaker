using System.Text.Json.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace RegexMaker.Nodes;
public class LiteralNode : RgxNode
{
    private string _searchString;
    public string SearchString { get => _searchString; set => _searchString=value; }

    private bool _autoEscape = true;
    public bool AutoEscape { get => _autoEscape; set => _autoEscape=value; }

    // Nodes created with the parameterless constructor are only exemplars and will never calculate
    public LiteralNode()
        : base(RgxNodeType.StringSearch)
    {
        SearchString = string.Empty;
        Debug.Assert(_searchString != null);
        AutoEscape = true;
    }

    public LiteralNode(string searchString) : base(RgxNodeType.StringSearch, new IRgxNode[0])
    {
        SearchString = searchString;
    }

    internal override string CalculateResult()
    {
        // For a string search node, the result is just the search string itself.
        return AutoEscape ? AutoEscapeString(SearchString) : SearchString;
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
    public override string DisplayName => $"\"{(AutoEscape ? AutoEscapeString(SearchString) : SearchString)}\"";
    
    public override string Code(CodeCollector cc)
    {
        if (VariableName != null)
        {
            return VariableName;
        }
        var code = $"\"{SearchString}\"";
        return (CheckRename(cc) ? VariableName : code)!;
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
        {
            SearchString = searchStringElement.GetString() ?? string.Empty;
        }
        if (data.TryGetValue("AutoEscape", out var autoEscapeElement))
        {
            AutoEscape = autoEscapeElement.GetBoolean();
        }
    }

    private static string AutoEscapeString(string input)
    {
        // List of regex special characters that need to be escaped
        var specialChars = new HashSet<char> { '.', '^', '$', '*', '+', '?', '(', ')', '[', ']', '{', '}', '\\', '|', '/' };
        var escapedString = new System.Text.StringBuilder();
        foreach (var c in input)
        {
            if (specialChars.Contains(c))
            {
                escapedString.Append('\\'); // Escape the special character
            }
            escapedString.Append(c);
        }
        return escapedString.ToString();
    }
}