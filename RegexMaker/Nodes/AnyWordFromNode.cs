using RegexStringLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RegexMaker.Nodes;

/// <summary>
/// A node that matches any one of multiple specified words.
/// </summary>
public class AnyWordFromNode : RgxNode
{
    public List<string> Words { get; set; } = [];

    public AnyWordFromNode() : base(RgxNodeType.AnyWordFrom)
    {
        Words = [];
    }

    public AnyWordFromNode(IList<string> words) : base(RgxNodeType.AnyWordFrom)
    {
        Words = words.ToList();
    }

    internal override string CalculateResult()
    {
        if (Words.Count == 0)
            return string.Empty;

        // Use Stex.AnyOf to create alternation pattern for the words
        // Each word is escaped to handle special regex characters
        return Stex.AnyOf(Words.Select(w => Stex.AnyOf(w)).ToArray());
    }

    public override string RandomMatch()
    {
        if (Words.Count == 0)
            return string.Empty;

        return Words[random.Next(Words.Count)];
    }

    public override string DisplayName
    {
        get
        {
            if (Words.Count == 0)
                return "AnyWord()";
            if (Words.Count == 1)
                return $"AnyWord({Words[0]})";
            return $"AnyWord({Words[0]}, ...)";
        }
    }

    public override IRgxNode Default()
    {
        return new AnyWordFromNode();
    }

    public override string Code(CodeCollector cc)
    {
        if (VariableName != null)
        {
            return VariableName;
        }

        var quotedWords = Words.Select(w => $@"""{w}""");
        var code = $"Stex.AnyOf({string.Join(", ", quotedWords)})";

        return (CheckRename(cc) ? VariableName : code)!;
    }

    protected override void AddSerializationData(Dictionary<string, object?> data)
    {
        base.AddSerializationData(data);
        data["Words"] = Words;
    }

    protected override void RestoreSerializationData(Dictionary<string, JsonElement> data)
    {
        base.RestoreSerializationData(data);
        if (data.TryGetValue("Words", out var wordsElement))
        {
            Words = wordsElement.EnumerateArray()
                .Select(e => e.GetString() ?? string.Empty)
                .ToList();
        }
    }
}