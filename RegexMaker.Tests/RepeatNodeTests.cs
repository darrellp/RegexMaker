using RegexMaker.Nodes;
using RegexStringLibrary;

namespace RegexMaker.Tests;

public class RepeatNodeTests
{
    [Fact]
    public void AnyCharFromNode_NameShouldBeRepeat()
    {
        var node = new LiteralNode("a");
        var repeatNode = new RepeatNode([node], 3, 3);
        Assert.Equal("Repeat", repeatNode.Name);
    }

    [Fact]
    public void RepeatNode_ShouldRepeatExactNumberOfTimes()
    {
        // Arrange
        var node = new LiteralNode("a");
        var repeatNode = new RepeatNode([node], 3, 3);

        // Assert
        Assert.True(repeatNode.Matches("aaa"));
    }

    [Fact]
    public void RepeatNode_ShouldRepeatWithRange()
    {
        // Arrange
        var node = new LiteralNode("b");
        var repeatNode = new RepeatNode([node], 2, 5);

        // Assert
        Assert.True(repeatNode.Matches("bb"));
        Assert.True(repeatNode.Matches("bbb"));
        Assert.True(repeatNode.Matches("bbbb"));
        Assert.True(repeatNode.Matches("bbbbb"));
        Assert.False(repeatNode.Matches("b"));
        Assert.False(repeatNode.Matches("bbbbbb"));
    }

    [Fact]
    public void RepeatNode_ShouldRepeatAtLeast()
    {
        // Arrange
        var node = new LiteralNode("c");
        var repeatNode = new RepeatNode([node], 3);

        // Assert
        Assert.True(repeatNode.Matches("ccc"));
        Assert.True(repeatNode.Matches("cccccccccccccc"));
        Assert.False(repeatNode.Matches("cc"));
    }

    [Fact]
    public void RepeatNode_ShouldHandleZeroOrMore()
    {
        // Arrange
        var node = new LiteralNode("d");
        var repeatNode = new RepeatNode([node], 0);

        // Assert
        Assert.True(repeatNode.Matches(""));
        Assert.True(repeatNode.Matches("ddddddddddd"));
    }

    [Fact]
    public void RepeatNode_ShouldHandleOneOrMore()
    {
        // Arrange
        var node = new LiteralNode("e");
        var repeatNode = new RepeatNode([node], 1);

        // Assert
        Assert.True(repeatNode.Matches("e"));
        Assert.True(repeatNode.Matches("eeeeee"));
        Assert.False(repeatNode.Matches(""));
    }

    [Fact]
    public void RepeatNode_ShouldHandleOptional()
    {
        // Arrange
        var node = new LiteralNode("f");
        var repeatNode = new RepeatNode([node], 0, 1);

        // Assert
        Assert.True(repeatNode.Matches(""));
        Assert.True(repeatNode.Matches("f"));
        Assert.False(repeatNode.Matches("ff"));
    }

    [Fact]
    public void RepeatNode_ShouldHandleLazyQuantifier()
    {
        // Arrange
        var node = new LiteralNode("g");
        var repeatNode = new RepeatNode([node], 2, 4, isLazy: true);

        // Act
        var result = repeatNode.ProduceResult();

        // Assert
        Assert.Equal("g{2,4}?", result);
    }

    [Fact]
    public void RepeatNode_ShouldHandleLazyOptional()
    {
        // Arrange
        var node = new LiteralNode("h");
        var repeatNode = new RepeatNode([node], 0, 1, isLazy: true);

        // Act
        var result = repeatNode.ProduceResult();

        // Assert
        Assert.Equal("h??", result);
    }

    [Fact]
    public void RepeatNode_ShouldHandleLazyZeroOrMore()
    {
        // Arrange
        var node = new LiteralNode("i");
        var repeatNode = new RepeatNode([node], 0, -1, isLazy: true);

        // Act
        var result = repeatNode.ProduceResult();

        // Assert
        Assert.Equal("i*?", result);
    }

    [Fact]
    public void RepeatNode_ShouldThrowWhenMultipleParameters()
    {
        // Arrange
        var node1 = new LiteralNode("a");
        var node2 = new LiteralNode("b");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RepeatNode([node1, node2], 1, 3));
    }

    [Fact]
    public void RepeatNode_ShouldThrowWhenNoParameters()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RepeatNode([], 1, 3));
    }

    [Fact]
    public void RepeatNode_ShouldHaveCorrectNodeType()
    {
        // Arrange
        var node = new LiteralNode("test");
        var repeatNode = new RepeatNode([node], 1);

        // Act
        var nodeType = repeatNode.NodeType;

        // Assert
        Assert.Equal(RgxNodeType.Repeat, nodeType);
    }

    [Fact]
    public void RepeatNode_ShouldCacheResult()
    {
        // Arrange
        var node = new LiteralNode("test");
        var repeatNode = new RepeatNode([node], 2, 4);

        // Act
        var result1 = repeatNode.ProduceResult();
        var result2 = repeatNode.ProduceResult();

        // Assert
        Assert.Same(result1, result2);
    }

    [Fact]
    public void RepeatNode_ShouldInvalidateCacheWhenDirty()
    {
        // Arrange
        var node = new LiteralNode("test");
        var repeatNode = new RepeatNode([node], 1, 3);
        var firstResult = repeatNode.ProduceResult();

        // Act
        repeatNode.MakeDirty();
        var secondResult = repeatNode.ProduceResult();

        // Assert
        Assert.NotSame(firstResult, secondResult);
        Assert.Equal(firstResult, secondResult);
    }

    [Fact]
    public void RepeatNode_ShouldMatchRepeatedPattern()
    {
        // Arrange
        var node = new LiteralNode("a");
        var repeatNode = new RepeatNode([node], 2, 4);

        // Act & Assert
        Assert.True(repeatNode.Matches("aa"));
        Assert.True(repeatNode.Matches("aaa"));
        Assert.True(repeatNode.Matches("aaaa"));
        Assert.False(repeatNode.Matches("a"));
        Assert.False(repeatNode.Matches("aaaaa"));
    }

    [Fact]
    public void RepeatNode_ShouldHandleComplexPattern()
    {
        // Arrange
        var node = new LiteralNode(@"\d") { AutoEscape = false };
        var repeatNode = new RepeatNode([node], 3, 5);

        // Act
        var result = repeatNode.ProduceResult();

        // Assert
        Assert.True(repeatNode.Matches("123"));
        Assert.True(repeatNode.Matches("12345"));
        Assert.False(repeatNode.Matches("12"));
        Assert.False(repeatNode.Matches("123456"));
    }

    [Fact]
    public void RepeatNode_ShouldStoreProperties()
    {
        // Arrange
        var node = new LiteralNode("x");
        var repeatNode = new RepeatNode([node], 2, 5, isLazy: true);

        // Assert
        Assert.Equal(2, repeatNode.Least);
        Assert.Equal(5, repeatNode.Most);
        Assert.True(repeatNode.IsLazy);
    }

    [Fact]
    public void RepeatNode_ShouldHandleNestedRepeat()
    {
        // Arrange
        var innerNode = new LiteralNode("a");
        var innerRepeat = new RepeatNode([innerNode], 2, 2);
        var outerRepeat = new RepeatNode([innerRepeat], 3, 3);

        // Act
        var result = outerRepeat.ProduceResult();

        // Assert
        Assert.True(outerRepeat.Matches("aaaaaa"));
    }

    [Theory]
    [InlineData(0, -1, "x*")]
    [InlineData(1, -1, "x+")]
    [InlineData(2, -1, "x{2,}")]
    [InlineData(5, 5, "x{5}")]
    [InlineData(0, 1, "x?")]
    [InlineData(3, 7, "x{3,7}")]
    public void RepeatNode_ShouldHandleVariousRepeatPatterns(int least, int most, string expected)
    {
        // Arrange
        var node = new LiteralNode("x");
        var repeatNode = new RepeatNode([node], least, most);

        // Act
        var result = repeatNode.ProduceResult();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RepeatNode_ShouldHandleDigitPatternRepeated()
    {
        // Arrange
        var digitNode = new LiteralNode(Stex.Digit) { AutoEscape = false };
        var repeatNode = new RepeatNode([digitNode], 3, 3);

        // Assert
        Assert.True(repeatNode.Matches("123"));
        Assert.False(repeatNode.Matches("12"));
        Assert.False(repeatNode.Matches("1234"));
    }
}