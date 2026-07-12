using System.Runtime.InteropServices;

namespace CsWebUi.Native;

/// <summary>Constants from WebUI 2.5.0-beta.4.</summary>
public static class WebUiConstants
{
    /// <summary>The version of the C header represented by this assembly.</summary>
    public const string Version = "2.5.0-beta.4";

    /// <summary>The largest valid WebUI window/server identifier.</summary>
    public const ushort MaxIds = ushort.MaxValue;

    /// <summary>The maximum supported event argument index.</summary>
    public const int MaxArgumentIndex = 16;
}

/// <summary>Known browser identifiers from <c>webui_browser</c>.</summary>
public enum WebUiBrowser : uint
{
    NoBrowser = 0,
    AnyBrowser = 1,
    Chrome = 2,
    Firefox = 3,
    Edge = 4,
    Safari = 5,
    Chromium = 6,
    Opera = 7,
    Brave = 8,
    Vivaldi = 9,
    Epic = 10,
    Yandex = 11,
    ChromiumBased = 12,
    WebView = 13,
}

/// <summary>Known JavaScript/TypeScript runtime identifiers from <c>webui_runtime</c>.</summary>
public enum WebUiRuntime : uint
{
    None = 0,
    Deno = 1,
    NodeJs = 2,
    Bun = 3,
}

/// <summary>Known event identifiers from <c>webui_event</c>.</summary>
public enum WebUiEventType : uint
{
    Disconnected = 0,
    Connected = 1,
    MouseClick = 2,
    Navigation = 3,
    Callback = 4,
}

/// <summary>Global WebUI configuration options.</summary>
public enum WebUiConfig
{
    ShowWaitConnection = 0,
    UiEventBlocking = 1,
    FolderMonitor = 2,
    MultiClient = 3,
    UseCookies = 4,
    AsynchronousResponse = 5,
}

/// <summary>WebUI logger levels.</summary>
public enum WebUiLoggerLevel : uint
{
    Debug = 0,
    Info = 1,
    Error = 2,
}

/// <summary>
/// Native <c>webui_event_t</c>. Every pointer is borrowed from WebUI and is only
/// valid for the duration documented by the native callback.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WebUiEventNative
{
    /// <summary>The native window identifier.</summary>
    public nuint Window;

    /// <summary>The native event type. Cast to <see cref="WebUiEventType"/> when needed.</summary>
    public nuint EventType;

    /// <summary>A borrowed UTF-8, NUL-terminated element name.</summary>
    public byte* Element;

    /// <summary>The internal native event identifier.</summary>
    public nuint EventNumber;

    /// <summary>The native binding identifier.</summary>
    public nuint BindId;

    /// <summary>The unique client identifier.</summary>
    public nuint ClientId;

    /// <summary>The connection identifier.</summary>
    public nuint ConnectionId;

    /// <summary>A borrowed UTF-8, NUL-terminated cookie string.</summary>
    public byte* Cookies;
}
