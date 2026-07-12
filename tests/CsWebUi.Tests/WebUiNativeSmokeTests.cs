using System.Text;
using CsWebUi.Native;

namespace CsWebUi.Tests;

public sealed unsafe class WebUiNativeSmokeTests
{
    [NativeFact]
    public void NativeLibraryLoadsAndProvidesCoreServicesWhenConfigured()
    {
        Assert.NotEqual((nuint)0, WebUiNative.GetFreePort());

        var input = Encoding.UTF8.GetBytes("CsWebUi\0");
        fixed (byte* inputPointer = input)
        {
            var encoded = WebUiNative.Encode(inputPointer);
            Assert.NotEqual((nint)0, (nint)encoded);

            try
            {
                Assert.Equal("Q3NXZWJVaQ==", Encoding.UTF8.GetString(new ReadOnlySpan<byte>(encoded, 12)));
            }
            finally
            {
                WebUiNative.Free(encoded);
            }
        }

        var window = WebUiNative.NewWindow();
        Assert.NotEqual((nuint)0, window);
        WebUiNative.Destroy(window);
    }
}
