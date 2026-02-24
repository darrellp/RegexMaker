using RegexMaker.Nodes;

namespace RegexMaker.Tests;

public class IRgxNodeTests
{
    [Fact]
    public void IRgxNode_ShouldExposeRequiredProperties()
    {
        // Arrange
        var node = new TestRgxNode();

        // Act & Assert
        Assert.True(node.ID >= 0);
        Assert.Equal(RgxNodeType.StringSearch, node.NodeType);
        Assert.NotNull(node.Parameters);
        Assert.NotNull(node.ProduceResult());
    }

    [Fact]
    public void Parameters_ShouldBeInitializedAsEmptyList()
    {
        // Arrange & Act
        var node = new TestRgxNode();

        // Assert
        Assert.Empty(node.Parameters);
    }

    private class TestRgxNode : RgxNode
    {
        public TestRgxNode() : base(RgxNodeType.StringSearch)
        {
        }

        public override IRgxNode Default()
        {
            throw new NotImplementedException();
        }

        internal override string CalculateResult()
        {
            return ".*";
        }
    }
}