using RegexMaker.Nodes;

namespace RegexMaker.Tests;

public class RandomMatchTests
{
    [Fact]
    public void RandomMatch_PhoneNumbers()
    {
        var DigitsRange = new RangeNode("0", "9");
        var Digit = new AnyCharFromNode([DigitsRange]);
        var prefixOrArea = new RepeatNode([Digit], 3, 3);
        var areaDash = new ConcatenateNode(prefixOrArea, new LiteralNode("-"));
        var area = new RepeatNode([areaDash], 0, 1);
        var phone = new ConcatenateNode(area, prefixOrArea, new LiteralNode("-"), new RepeatNode([Digit], 4, 4));
        for (var i = 0; i < 10; i++)
        {
            var match = phone.RandomMatch();
            Assert.True(phone.Matches(match));
        }
    }
}