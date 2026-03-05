using RegexMaker.Nodes;

namespace RegexMaker.Tests.Nodes;

public class CodeCollectorTest
{
    [Fact]
    public void CheckCodeForNumeric()
    {
        var nodeNumeric = new NumericNode();
        var cc = new CodeCollector(nodeNumeric);
        cc.GatherCode();
        Assert.Equal("var result = Stex.Integer();\r\n", cc.Result);
    }

    [Fact]
    public void CheckCodeForLiteral()
    {
        var nodeLiteral = new LiteralNode("Testing");
        var cc = new CodeCollector(nodeLiteral);
        cc.GatherCode();
        Assert.Equal("var result = \"Testing\";\r\n", cc.Result);
    }

    [Fact]
    public void CheckCodeForCharClass()
    {
        var nodeCharClass = new CharClassNode();
        var cc = new CodeCollector(nodeCharClass);
        cc.GatherCode();
        // Defaults to white space
        Assert.Equal("var result = Stex.White;\r\n", cc.Result);
    }

    [Fact]
    public void CheckCodeForAnyCharFrom()
    {
        var nodeAcf = new AnyCharFromNode();
        var cc = new CodeCollector(nodeAcf);
        cc.GatherCode();
        // Defaults to [A-Za-z0-9]
        Assert.Equal("var result = \"[A-Za-z0-9]\";\r\n", cc.Result);
        
        var nodeRange = new RangeNode();
        nodeAcf.Parameters[0] = nodeRange;
        cc = new CodeCollector(nodeAcf);
        cc.GatherCode();
        // Defaults to a-z
        Assert.Equal("var result = \"[a-z]\";\r\n", cc.Result);

    }

    [Fact]
    public void CheckCodeForNamed()
    {
        var nodeNamed = new NamedNode();
        nodeNamed.GroupName = "Testing";
        nodeNamed.Parameters[0] = new NumericNode();
        
        var cc = new CodeCollector(nodeNamed);
        cc.GatherCode();
        var expected = """
                       var Testing = Stex.Integer().Named("Testing");
                       var result = Testing;
                       
                       """;
        Assert.Equal(expected, cc.Result);
    }

    [Fact]
    public void CheckForMultipleParents()
    {
        var nodeAcf = new AnyCharFromNode();
        var nodeAnyOf = new ConcatenateNode(nodeAcf, nodeAcf);
        var cc = new CodeCollector(nodeAnyOf);
        cc.GatherCode();
        var expected = """
                       var __AnyCharFrom__0 = "[A-Za-z0-9]";
                       var result = Stex.Cat(__AnyCharFrom__0, __AnyCharFrom__0);

                       """;
        var value = cc.Result;
        Assert.Equal(expected, value);
    }

}