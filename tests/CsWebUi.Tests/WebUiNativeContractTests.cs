namespace CsWebUi.Tests;

public sealed class WebUiNativeContractTests
{
    [Fact]
    public void HeaderConstantsMatchThePinnedUpstreamVersion()
    {
        Assert.Equal("2.5.0-beta.4", CsWebUi.Native.WebUiConstants.Version);
        Assert.Equal(ushort.MaxValue, CsWebUi.Native.WebUiConstants.MaxIds);
        Assert.Equal(16, CsWebUi.Native.WebUiConstants.MaxArgumentIndex);
    }

    [Fact]
    public void NativeEnumValuesMatchTheWebUiHeader()
    {
        Assert.Equal(0u, (uint)CsWebUi.Native.WebUiBrowser.NoBrowser);
        Assert.Equal(1u, (uint)CsWebUi.Native.WebUiBrowser.AnyBrowser);
        Assert.Equal(13u, (uint)CsWebUi.Native.WebUiBrowser.WebView);
        Assert.Equal(3u, (uint)CsWebUi.Native.WebUiRuntime.Bun);
        Assert.Equal(4u, (uint)CsWebUi.Native.WebUiEventType.Callback);
        Assert.Equal(5, (int)CsWebUi.Native.WebUiConfig.AsynchronousResponse);
    }

    [Fact]
    public void NativeEventLayoutMatchesThe64BitWebUiAbi()
    {
        Assert.Equal(64, System.Runtime.CompilerServices.Unsafe.SizeOf<CsWebUi.Native.WebUiEventNative>());
        Assert.Equal((nint)0, System.Runtime.InteropServices.Marshal.OffsetOf<CsWebUi.Native.WebUiEventNative>(nameof(CsWebUi.Native.WebUiEventNative.Window)));
        Assert.Equal((nint)16, System.Runtime.InteropServices.Marshal.OffsetOf<CsWebUi.Native.WebUiEventNative>(nameof(CsWebUi.Native.WebUiEventNative.Element)));
        Assert.Equal((nint)56, System.Runtime.InteropServices.Marshal.OffsetOf<CsWebUi.Native.WebUiEventNative>(nameof(CsWebUi.Native.WebUiEventNative.Cookies)));
    }
}
