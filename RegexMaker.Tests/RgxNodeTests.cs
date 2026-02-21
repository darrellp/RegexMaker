using RegexMaker.Nodes;

namespace RegexMaker.Tests;

public class RgxNodeTests
{
    [Fact]
    public void RgxNode_ShouldHaveUniqueID()
    {
        // Arrange & Act
        var node1 = CreateTestNode("Test");
        var node2 = CreateTestNode("Test");

        // Assert
        Assert.NotEqual(node1.ID, node2.ID);
    }

    [Fact]
    public void ProduceResult_ShouldCacheResult()
    {
        // Arrange
        var node = CreateTestNode("Test");

        // Act
        var result1 = node.ProduceResult();
        var result2 = node.ProduceResult();

        // Assert
        Assert.Same(result1, result2); // Same reference means cached
    }

    [Fact]
    public void MakeDirty_ShouldInvalidateCache()
    {
        // Arrange
        var node = CreateTestNode("Test");
        var firstResult = node.ProduceResult();

        // Act
        node.MakeDirty();
        var secondResult = node.ProduceResult();

        // Assert
        Assert.Same(firstResult, secondResult); // Different references after cache invalidation
    }

    [Theory]
    [InlineData("test", true)]
    [InlineData("TEST", false)]
    [InlineData("", false)]
    public void Matches_ShouldValidateInputAgainstPattern(string input, bool expectedMatch)
    {
        // Arrange
        var node = CreateTestNode("test");

        // Act
        var matches = node.Matches(input);

        // Assert
        Assert.Equal(expectedMatch, matches);
    }

    // Helper method to create a concrete test implementation
    private StringSearchNode CreateTestNode(string val)
    {
        return new StringSearchNode(val);
    }
}