using CsWebUi;

namespace CsWebUi.Tests;

public sealed class WebUiResultTests
{
    [Fact]
    public void NoneHasNoValueKind()
    {
        Assert.Equal(WebUiResultKind.None, WebUiResult.None.Kind);
    }

    [Fact]
    public void ImplicitValuesSelectTheExpectedResponseKind()
    {
        WebUiResult text = "hello";
        WebUiResult integer = 42L;
        WebUiResult floatingPoint = 3.5d;
        WebUiResult boolean = true;

        Assert.Equal(WebUiResultKind.String, text.Kind);
        Assert.Equal(WebUiResultKind.Int64, integer.Kind);
        Assert.Equal(WebUiResultKind.Double, floatingPoint.Kind);
        Assert.Equal(WebUiResultKind.Boolean, boolean.Kind);
    }

    [Fact]
    public void NullStringIsNormalizedToAnEmptyResponse()
    {
        Assert.Equal(WebUiResultKind.String, WebUiResult.FromString(null).Kind);
    }
}
