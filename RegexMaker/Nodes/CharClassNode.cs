using RegexStringLibrary;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RegexMaker.Nodes;

public enum CharClassType
{
    WildCard,
    Start,
    End,
    WhiteSpace,
    Digit,
    NonDigit,
    WordChar,
    NonWordChar,
    WordBoundary,
    NonWordBoundary,
    Letter,
    CapLetter,
    LowerLetter,
    AlphaNumeric,

    // Add more character classes as needed
}

public class CharClassNode : RgxNode
{
    public CharClassType CharClass { get; set; }

    public CharClassNode() : base(RgxNodeType.CharClass)
    {
        CharClass = CharClassType.WhiteSpace;
    }

    public CharClassNode(CharClassType charClass) : base(RgxNodeType.CharClass)
    {
        CharClass = charClass;
    }

    internal override string CalculateResult()
    {
        return CharClass switch {
            CharClassType.WildCard => Stex.Any,
            CharClassType.WhiteSpace => Stex.White,
            CharClassType.Digit => Stex.Digit,
            CharClassType.NonDigit => Stex.NonDigit,
            CharClassType.WordChar => Stex.WordChar,
            CharClassType.NonWordChar => Stex.NonWordChar,
            CharClassType.WordBoundary => Stex.WordBoundary,
            CharClassType.NonWordBoundary => Stex.NonWordBoundary,
            CharClassType.Letter => Stex.Letter,
            CharClassType.CapLetter => Stex.CapLetter,
            CharClassType.LowerLetter => Stex.LowerLetter,
            CharClassType.AlphaNumeric => Stex.Alphanum,
            CharClassType.Start => Stex.Begin,
            CharClassType.End => Stex.End,
            _ => string.Empty
        };
    }

    protected override void AddSerializationData(Dictionary<string, object?> data)
    {
        base.AddSerializationData(data);
        data["CharClassType"] = Enum.GetName(CharClass);
    }

    protected override void RestoreSerializationData(Dictionary<string, System.Text.Json.JsonElement> data)
    {
        base.RestoreSerializationData(data);
        if (data.TryGetValue("CharClassType", out var classType))
        {
            CharClass = Enum.Parse<CharClassType>(classType.GetString() ?? string.Empty);
        }
    }

    public override string RandomMatch()
    {
        return string.Empty;
    }

    public override IRgxNode Default()
    {
        return new CharClassNode();
    }

    public override string DisplayName => CharClass.ToString();
}