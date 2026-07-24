using CsWebUi;
using CsWebUi.Native;

namespace CsWebUi.Tests;

public sealed class WebUiWindowSmokeTests
{
    [Fact]
    public async Task WaitAsyncHonorsPreCanceledToken()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => WebUiApplication.WaitAsync(cancellation.Token));
    }

    [NativeFact]
    public void HighLevelWindowOwnsAndDestroysANativeWindowWhenConfigured()
    {
        using var window = new WebUiWindow();
        Assert.NotEqual((nuint)0, window.Id);
        Assert.False(window.IsShown);
    }
}
