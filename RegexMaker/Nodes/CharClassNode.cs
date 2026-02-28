using System;
using System.Collections.Generic;

namespace RegexMaker.Nodes;

public enum CharClassType
{
    WhiteSpace,
    Digit
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
            CharClassType.WhiteSpace => @"\s",
            CharClassType.Digit => @"\d",
            _ => string.Empty
        };
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