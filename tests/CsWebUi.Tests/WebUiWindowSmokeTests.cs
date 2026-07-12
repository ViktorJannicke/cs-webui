using CsWebUi;
using CsWebUi.Native;

namespace CsWebUi.Tests;

public sealed class WebUiWindowSmokeTests
{
    [Fact]
    public void HighLevelWindowOwnsAndDestroysANativeWindowWhenConfigured()
    {
        var libraryPath = Environment.GetEnvironmentVariable("CSWEBUI_NATIVE_LIBRARY");
        if (string.IsNullOrWhiteSpace(libraryPath))
        {
            return;
        }

        WebUiNativeLibrary.SetLibraryPath(libraryPath);

        using var window = new WebUiWindow();
        Assert.NotEqual((nuint)0, window.Id);
        Assert.False(window.IsShown);
    }
}
