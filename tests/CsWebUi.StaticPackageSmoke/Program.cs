using CsWebUi;

var port = WebUiApplication.GetFreePort();
var webViewAvailable = WebUiApplication.BrowserExists(WebUiBrowser.WebView);

Console.WriteLine($"WebUI allocated port {port}. WebView available: {webViewAvailable}.");
return port == 0 ? 1 : 0;
