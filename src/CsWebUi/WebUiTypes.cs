namespace CsWebUi;

/// <summary>Identifies the browser host used to display a WebUI window.</summary>
public enum WebUiBrowser : uint
{
    /// <summary>Do not launch a browser.</summary>
    NoBrowser = 0,

    /// <summary>Alias for <see cref="NoBrowser"/>.</summary>
    None = NoBrowser,

    /// <summary>Use WebUI's recommended installed browser.</summary>
    AnyBrowser = 1,

    /// <summary>Alias for <see cref="AnyBrowser"/>.</summary>
    Any = AnyBrowser,

    Chrome,
    Firefox,
    Edge,
    Safari,
    Chromium,
    Opera,
    Brave,
    Vivaldi,
    Epic,
    Yandex,
    ChromiumBased,

    /// <summary>Use an embedded WebView host.</summary>
    WebView,
}

/// <summary>Identifies the JavaScript or TypeScript runtime used by WebUI.</summary>
public enum WebUiRuntime : uint
{
    /// <summary>Do not select a runtime for JavaScript or TypeScript files.</summary>
    None = 0,

    /// <summary>Use Deno.</summary>
    Deno,

    /// <summary>Use Node.js.</summary>
    NodeJs,

    /// <summary>Use Bun.</summary>
    Bun,
}

/// <summary>Identifies an event reported by WebUI.</summary>
public enum WebUiEventType : uint
{
    /// <summary>A client disconnected from a window.</summary>
    Disconnected = 0,

    /// <summary>A client connected to a window.</summary>
    Connected,

    /// <summary>An HTML element was clicked.</summary>
    MouseClick,

    /// <summary>A client navigated within the window.</summary>
    Navigation,

    /// <summary>A JavaScript binding invoked managed code.</summary>
    Callback,
}

/// <summary>Controls global WebUI behavior.</summary>
public enum WebUiConfiguration : uint
{
    /// <summary>Whether show calls wait for a client connection.</summary>
    ShowWaitConnection = 0,

    /// <summary>Whether UI events are dispatched serially.</summary>
    UiEventBlocking,

    /// <summary>Whether files below a root folder are monitored for changes.</summary>
    FolderMonitor,

    /// <summary>Whether more than one client may connect to a window.</summary>
    MultiClient,

    /// <summary>Whether WebUI authentication cookies are enabled.</summary>
    UseCookies,

    /// <summary>Whether native events may be completed asynchronously.</summary>
    AsynchronousResponse,
}

/// <summary>Identifies the severity of a WebUI log record.</summary>
public enum WebUiLogLevel : uint
{
    /// <summary>Verbose diagnostic information.</summary>
    Debug = 0,

    /// <summary>General informational messages.</summary>
    Information,

    /// <summary>Fatal or error messages.</summary>
    Error,
}

/// <summary>Provides details of an exception raised by a managed WebUI callback.</summary>
public sealed class WebUiUnhandledCallbackExceptionEventArgs : EventArgs
{
    internal WebUiUnhandledCallbackExceptionEventArgs(Exception exception, WebUiWindow window, WebUiEvent? webUiEvent)
    {
        Exception = exception;
        Window = window;
        Event = webUiEvent;
    }

    /// <summary>Gets the exception that was caught at the unmanaged callback boundary.</summary>
    public Exception Exception { get; }

    /// <summary>Gets the window that owns the callback.</summary>
    public WebUiWindow Window { get; }

    /// <summary>Gets the event being handled, when it was safely available.</summary>
    public WebUiEvent? Event { get; }
}
