using RegexMaker.Nodes;
using RegexStringLibrary;

namespace RegexMaker.Tests;

public class ConcatenateNodeTests
{
    [Fact]
    public void ConcatenateNode_NameShouldBeConcatenate()
    {
        Assert.Equal("Concatenate", new ConcatenateNode().Name);
    }

    [Fact]
    public void ConcatenateNode_ShouldConcatenateTwoStrings()
    {
        // Arrange
        var node1 = new LiteralNode("Hello");
        var node2 = new LiteralNode("World");
        var concatenateNode = new ConcatenateNode(node1, node2);

        // Act
        var result = concatenateNode.ProduceResult();

        // Assert
        Assert.Equal("HelloWorld", result);
    }

    [Fact]
    public void ConcatenateNode_ShouldConcatenateMultipleStrings()
    {
        // Arrange
        var node1 = new LiteralNode("A");
        var node2 = new LiteralNode("B");
        var node3 = new LiteralNode("C");
        var node4 = new LiteralNode("D");
        var concatenateNode = new ConcatenateNode(node1, node2, node3, node4);

        // Act
        var result = concatenateNode.ProduceResult();

        // Assert
        Assert.Equal("ABCD", result);
    }

    [Fact]
    public void ConcatenateNode_ShouldHandleEmptyParameters()
    {
        var pre = Stex.DateAmerican;

        // Arrange
        var concatenateNode = new ConcatenateNode();

        // Act
        var result = concatenateNode.ProduceResult();

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ConcatenateNode_ShouldHandleSingleParameter()
    {
        // Arrange
        var node = new LiteralNode("Single");
        var concatenateNode = new ConcatenateNode(node);

        // Act
        var result = concatenateNode.ProduceResult();

        // Assert
        Assert.Equal("Single", result);
    }

    [Fact]
    public void ConcatenateNode_ShouldCacheResult()
    {
        // Arrange
        var node1 = new LiteralNode("Test");
        var node2 = new LiteralNode("Cache");
        var concatenateNode = new ConcatenateNode(node1, node2);

        // Act
        var result1 = concatenateNode.ProduceResult();
        var result2 = concatenateNode.ProduceResult();

        // Assert
        Assert.Same(result1, result2);
    }

    [Fact]
    public void ConcatenateNode_ShouldInvalidateCacheWhenDirty()
    {
        // Arrange
        var node1 = new LiteralNode("Test");
        var node2 = new LiteralNode("Dirty");
        var concatenateNode = new ConcatenateNode(node1, node2);
        var firstResult = concatenateNode.ProduceResult();

        // Act
        concatenateNode.MakeDirty();
        var secondResult = concatenateNode.ProduceResult();

        // Assert
        Assert.Equal(firstResult, secondResult);
    }

    [Fact]
    public void ConcatenateNode_ShouldMatchConcatenatedPattern()
    {
        // Arrange
        var node1 = new LiteralNode("test");
        var node2 = new LiteralNode("123");
        var concatenateNode = new ConcatenateNode(node1, node2);

        // Act & Assert
        Assert.True(concatenateNode.Matches("test123"));
        Assert.False(concatenateNode.Matches("test456"));
        Assert.False(concatenateNode.Matches("test"));
        Assert.False(concatenateNode.Matches("123"));
    }

    [Fact]
    public void ConcatenateNode_ShouldHaveCorrectNodeType()
    {
        // Arrange
        var node = new ConcatenateNode();

        // Act
        var nodeType = node.NodeType;

        // Assert
        Assert.Equal(RgxNodeType.Concatenate, nodeType);
    }

    [Fact]
    public void ConcatenateNode_ShouldConcatenateWithSpecialRegexCharacters()
    {
        // Arrange
        var node1 = new LiteralNode(@"\d");
        var node2 = new LiteralNode(@"+");
        var concatenateNode = new ConcatenateNode(node1, node2);

        // Act
        var result = concatenateNode.ProduceResult();

        // Assert
        Assert.Equal(@"\d+", result);
    }

    [Theory]
    [InlineData("123", "456", "123456")]
    [InlineData("", "test", "test")]
    [InlineData("test", "", "test")]
    [InlineData("", "", "")]
    public void ConcatenateNode_ShouldHandleVariousInputs(string input1, string input2, string expected)
    {
        // Arrange
        var node1 = new LiteralNode(input1);
        var node2 = new LiteralNode(input2);
        var concatenateNode = new ConcatenateNode(node1, node2);

        // Act
        var result = concatenateNode.ProduceResult();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConcatenateNode_ShouldConcatenateNestedConcatenations()
    {
        // Arrange
        var node1 = new LiteralNode("A");
        var node2 = new LiteralNode("B");
        var innerConcat = new ConcatenateNode(node1, node2);

        var node3 = new LiteralNode("C");
        var outerConcat = new ConcatenateNode(innerConcat, node3);

        // Act
        var result = outerConcat.ProduceResult();

        // Assert
        Assert.Equal("ABC", result);
    }
}