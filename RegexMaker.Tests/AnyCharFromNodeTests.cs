using RegexMaker.Nodes;
using RegexStringLibrary;

namespace RegexMaker.Tests;

public class AnyCharFromNodeTests
{
    [Fact]
    public void AnyCharFromNode_NameShouldBeAnyCharFrom()
    {
        var node1 = new LiteralNode("abc");
        Assert.Equal("AnyCharFrom", new AnyCharFromNode([node1]).Name);
    }

    [Fact]
    public void AnyCharFromNode_ShouldCreateCharacterClass()
    {
        // Arrange
        var node1 = new LiteralNode("abc");
        var anyCharFromNode = new AnyCharFromNode([node1]);

        // Act
        var result = anyCharFromNode.ProduceResult();

        // Assert
        Assert.Equal("[abc]", result);
    }

    [Fact]
    public void AnyCharFromNode_ShouldCombineMultipleParameters()
    {
        // Arrange
        var node1 = new LiteralNode("abc");
        var node2 = new LiteralNode("123");
        var anyCharFromNode = new AnyCharFromNode([node1, node2]);

        // Act
        var result = anyCharFromNode.ProduceResult();

        // Assert
        Assert.Equal("[abc123]", result);
    }

    [Fact]
    public void AnyCharFromNode_ShouldHandleSingleCharacter()
    {
        // Arrange
        var node = new LiteralNode("x");
        var anyCharFromNode = new AnyCharFromNode([node]);

        // Act
        var result = anyCharFromNode.ProduceResult();

        // Assert
        Assert.Equal("[x]", result);
    }

    [Fact]
    public void AnyCharFromNode_ShouldHandleRanges()
    {
        // Arrange
        var node = new LiteralNode(Stex.Range("a", "z"));
        var anyCharFromNode = new AnyCharFromNode([node]);

        // Act
        var result = anyCharFromNode.ProduceResult();

        // Assert
        Assert.Equal("[a-z]", result);
    }

    [Fact]
    public void AnyCharFromNode_ShouldHandleMultipleRanges()
    {
        // Arrange
        var node1 = new LiteralNode(Stex.Range("a", "z"));
        var node2 = new LiteralNode(Stex.Range("A", "Z"));
        var anyCharFromNode = new AnyCharFromNode([node1, node2]);

        // Act
        var result = anyCharFromNode.ProduceResult();

        // Assert
        Assert.Equal("[a-zA-Z]", result);
    }

    [Fact]
    public void AnyCharFromNode_ShouldHandleEmptyParameters()
    {
        // Arrange
        var anyCharFromNode = new AnyCharFromNode([]);

        // Act
        var result = anyCharFromNode.ProduceResult();

        // Assert
        Assert.Equal("[]", result);
    }

    [Fact]
    public void AnyCharFromNode_ShouldHandleMixedRangesAndCharacters()
    {
        // Arrange
        var node1 = new LiteralNode(Stex.Range("0", "9"));
        var node2 = new LiteralNode("._-") { AutoEscape = false };
        var anyCharFromNode = new AnyCharFromNode([node1, node2]);

        // Act
        var result = anyCharFromNode.ProduceResult();

        // Assert
        Assert.Equal("[0-9._-]", result);
    }

    [Fact]
    public void AnyCharFromNode_ShouldHaveCorrectNodeType()
    {
        // Arrange
        var node = new LiteralNode("test");
        var anyCharFromNode = new AnyCharFromNode([node]);

        // Act
        var nodeType = anyCharFromNode.NodeType;

        // Assert
        Assert.Equal(RgxNodeType.AnyCharFrom, nodeType);
    }

    [Fact]
    public void AnyCharFromNode_ShouldCacheResult()
    {
        // Arrange
        var node = new LiteralNode("abc");
        var anyCharFromNode = new AnyCharFromNode([node]);

        // Act
        var result1 = anyCharFromNode.ProduceResult();
        var result2 = anyCharFromNode.ProduceResult();

        // Assert
        Assert.Same(result1, result2);
    }

    [Fact]
    public void AnyCharFromNode_ShouldInvalidateCacheWhenDirty()
    {
        // Arrange
        var node = new LiteralNode("xyz");
        var anyCharFromNode = new AnyCharFromNode([node]);
        var firstResult = anyCharFromNode.ProduceResult();

        // Act
        anyCharFromNode.MakeDirty();
        var secondResult = anyCharFromNode.ProduceResult();

        // Assert
        Assert.Equal(firstResult, secondResult);
    }

    [Fact]
    public void AnyCharFromNode_ShouldMatchAnyCharacterInSet()
    {
        // Arrange
        var node = new LiteralNode("abc");
        var anyCharFromNode = new AnyCharFromNode([node]);

        // Act & Assert
        Assert.True(anyCharFromNode.Matches("a"));
        Assert.True(anyCharFromNode.Matches("b"));
        Assert.True(anyCharFromNode.Matches("c"));
        Assert.False(anyCharFromNode.Matches("d"));
        Assert.False(anyCharFromNode.Matches("x"));
    }

    [Fact]
    public void AnyCharFromNode_ShouldMatchDigitRange()
    {
        // Arrange
        var node = new LiteralNode(Stex.Range("0", "9"));
        var anyCharFromNode = new AnyCharFromNode([node]);

        // Act & Assert
        Assert.True(anyCharFromNode.Matches("0"));
        Assert.True(anyCharFromNode.Matches("5"));
        Assert.True(anyCharFromNode.Matches("9"));
        Assert.False(anyCharFromNode.Matches("a"));
        Assert.False(anyCharFromNode.Matches("A"));
    }

    [Fact]
    public void AnyCharFromNode_ShouldHandleSpecialRegexCharacters()
    {
        // Arrange
        var node = new LiteralNode(@".\+*") { AutoEscape = false };
        var anyCharFromNode = new AnyCharFromNode([node]);

        // Act
        var result = anyCharFromNode.ProduceResult();

        // Assert
        Assert.Equal(@"[.\+*]", result);
    }

    [Fact]
    public void AnyCharFromNode_ShouldHandleBuiltInCharacterClasses()
    {
        // Arrange
        var node1 = new LiteralNode(Stex.Digit) { AutoEscape = false };
        var node2 = new LiteralNode("abc");
        var anyCharFromNode = new AnyCharFromNode([node1, node2]);

        // Act
        var result = anyCharFromNode.ProduceResult();

        // Assert
        Assert.Equal(@"[\dabc]", result);
    }

    [Theory]
    [InlineData("abc", "[abc]")]
    [InlineData("0-9", "[0-9]")]
    [InlineData("", "[]")]
    [InlineData("x", "[x]")]
    public void AnyCharFromNode_ShouldHandleVariousInputs(string input, string expected)
    {
        // Arrange
        var node = new LiteralNode(input);
        var anyCharFromNode = new AnyCharFromNode([node]);

        // Act
        var result = anyCharFromNode.ProduceResult();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void AnyCharFromNode_ShouldHandleNestedNodes()
    {
        // Arrange
        var node1 = new LiteralNode("a");
        var node2 = new LiteralNode("b");
        var concatenateNode = new ConcatenateNode(node1, node2);
        var anyCharFromNode = new AnyCharFromNode([concatenateNode]);

        // Act
        var result = anyCharFromNode.ProduceResult();

        // Assert
        Assert.Equal("[ab]", result);
    }

    [Fact]
    public void AnyCharFromNode_ShouldHandleAlphanumericRange()
    {
        // Arrange
        var node1 = new LiteralNode(Stex.Range("a", "z"));
        var node2 = new LiteralNode(Stex.Range("A", "Z"));
        var node3 = new LiteralNode(Stex.Range("0", "9"));
        var anyCharFromNode = new AnyCharFromNode([node1, node2, node3]);

        // Act
        var result = anyCharFromNode.ProduceResult();

        // Assert
        Assert.Equal("[a-zA-Z0-9]", result);
        Assert.True(anyCharFromNode.Matches("a"));
        Assert.True(anyCharFromNode.Matches("Z"));
        Assert.True(anyCharFromNode.Matches("5"));
        Assert.False(anyCharFromNode.Matches("_"));
        Assert.False(anyCharFromNode.Matches(" "));
    }

    [Fact]
    public void AnyCharFromNode_ShouldHandleWhitespaceCharacters()
    {
        // Arrange
        var node = new LiteralNode(" \t\n");
        var anyCharFromNode = new AnyCharFromNode([node]);
        var repNode = new RepeatNode([anyCharFromNode], 1);

        Assert.True(repNode.Matches(" "));
        Assert.True(repNode.Matches("       \t        "));
        Assert.True(repNode.Matches("\t"));
        Assert.True(repNode.Matches("\n"));
        Assert.False(repNode.Matches("a"));
    }

    [Fact]
    public void AnyCharFromNode_ShouldHandleManyParameters()
    {
        // Arrange
        var node1 = new LiteralNode("a");
        var node2 = new LiteralNode("b");
        var node3 = new LiteralNode("c");
        var node4 = new LiteralNode("d");
        var node5 = new LiteralNode("e");
        var anyCharFromNode = new AnyCharFromNode([node1, node2, node3, node4, node5]);
        var repNode = new RepeatNode([anyCharFromNode], 1);

        Assert.True(repNode.Matches("abcde"));
        Assert.True(repNode.Matches("bbacb"));
        Assert.False(repNode.Matches("abcdxyzabcd"));
    }

    [Fact]
    public void AnyCharFromNode_ShouldHandleHyphenInCharacterClass()
    {
        // Arrange
        var node = new LiteralNode("-_.") { AutoEscape = false };
        var anyCharFromNode = new AnyCharFromNode([node]);

        // Act
        var result = anyCharFromNode.ProduceResult();

        // Assert
        Assert.Equal("[-_.]", result);
    }

    [Fact]
    public void AnyCharFromNode_ShouldHandleVowels()
    {
        // Arrange
        var lowercase = new LiteralNode("aeiou");
        var uppercase = new LiteralNode("AEIOU");
        var anyCharFromNode = new AnyCharFromNode([lowercase, uppercase]);

        // Act
        var result = anyCharFromNode.ProduceResult();

        // Assert
        Assert.Equal("[aeiouAEIOU]", result);
        Assert.True(anyCharFromNode.Matches("a"));
        Assert.True(anyCharFromNode.Matches("E"));
        Assert.False(anyCharFromNode.Matches("b"));
        Assert.False(anyCharFromNode.Matches("Z"));
    }
}