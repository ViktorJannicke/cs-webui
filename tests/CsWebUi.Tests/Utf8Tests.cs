using CsWebUi.Internal;

namespace CsWebUi.Tests;

public sealed class Utf8Tests
{
    [Fact]
    public void RejectsEmbeddedNullCharacters()
    {
        Assert.Throws<ArgumentException>(() => Utf8.Encode("before\0after", "value"));
    }
}
