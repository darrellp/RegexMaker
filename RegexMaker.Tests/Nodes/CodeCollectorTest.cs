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
        Assert.Equal("var Result = Stex.Integer();\r\n", cc.Result);
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
                       Testing = Stex.Integer().Named("Testing");
                       var Result = Testing;
                       
                       """;
        Assert.Equal(expected, cc.Result);
    }
}