using CsWebUi.Native;

var libraryPath = Environment.GetEnvironmentVariable("CSWEBUI_NATIVE_LIBRARY");
if (string.IsNullOrWhiteSpace(libraryPath))
{
    Console.Error.WriteLine("CSWEBUI_NATIVE_LIBRARY must point to a WebUI shared library.");
    return 2;
}

WebUiNativeLibrary.SetLibraryPath(libraryPath);
var port = WebUiNative.GetFreePort();

Console.WriteLine($"WebUI allocated port {port}.");
return port == 0 ? 1 : 0;
