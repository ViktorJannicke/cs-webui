using CsWebUi;
using CsWebUi.Native;

namespace CsWebUi.Tests;

public sealed class WebUiWindowSmokeTests
{
    [NativeFact]
    public async Task WaitAsyncCompletesWhenNoWindowIsShown()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await WebUiApplication.WaitAsync(timeout.Token);
    }

    [NativeFact]
    public void HighLevelWindowOwnsAndDestroysANativeWindowWhenConfigured()
    {
        using var window = new WebUiWindow();
        Assert.NotEqual((nuint)0, window.Id);
        Assert.False(window.IsShown);
    }
}
