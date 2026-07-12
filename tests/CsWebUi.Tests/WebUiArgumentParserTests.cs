using CsWebUi.Internal;

namespace CsWebUi.Tests;

public sealed class WebUiArgumentParserTests
{
    [Theory]
    [InlineData("42", 42L)]
    [InlineData("-9007199254740991", -9007199254740991L)]
    public void ParsesJavaScriptIntegerValues(string text, long expected)
    {
        Assert.Equal(expected, WebUiArgumentParser.ParseInt64(text));
    }

    [Theory]
    [InlineData("19.75", 19.75)]
    [InlineData("22.5", 22.5)]
    [InlineData("1.25e2", 125.0)]
    public void ParsesJavaScriptDotDecimalValues(string text, double expected)
    {
        Assert.Equal(expected, WebUiArgumentParser.ParseDouble(text));
    }

    [Fact]
    public void RejectsLocaleSpecificDecimalSeparators()
    {
        Assert.Throws<FormatException>(() => WebUiArgumentParser.ParseDouble("19,75"));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("FALSE", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void ParsesJavaScriptBooleanValues(string text, bool expected)
    {
        Assert.Equal(expected, WebUiArgumentParser.ParseBoolean(text));
    }

    [Fact]
    public void RejectsPartialNumericAndBooleanValues()
    {
        Assert.Throws<FormatException>(() => WebUiArgumentParser.ParseInt64("42.5"));
        Assert.Throws<FormatException>(() => WebUiArgumentParser.ParseBoolean("falsehood"));
    }
}
