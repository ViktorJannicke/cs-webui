using CsWebUi;

namespace CsWebUi.Tests;

public sealed class WebUiExceptionTests
{
    [Fact]
    public void EmptyNativeMessageUsesAUsefulFallback()
    {
        var exception = new WebUiException(17, string.Empty);

        Assert.Equal((nuint)17, exception.ErrorNumber);
        Assert.Equal("WebUI failed with error 17.", exception.Message);
    }
}
