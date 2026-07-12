using CsWebUi.Internal;
using CsWebUi.Native;

namespace CsWebUi;

/// <summary>Represents one native WebUI event while its managed callback is executing.</summary>
/// <remarks>
/// Argument access and client operations are valid only while the callback is active. An event
/// passed to <see cref="WebUiWindow.BindAsync"/> stays active until its returned operation ends.
/// </remarks>
public sealed unsafe class WebUiEvent
{
    private readonly WebUiWindow _window;
    private readonly WebUiEventNative* _nativeEvent;
    private int _active = 1;
    private int _responded;

    internal WebUiEvent(WebUiWindow window, WebUiEventNative* nativeEvent)
    {
        _window = window;
        _nativeEvent = nativeEvent;

        WindowId = nativeEvent->Window;
        EventType = (WebUiEventType)(uint)nativeEvent->EventType;
        Element = Utf8.DecodeRequired(nativeEvent->Element);
        EventNumber = nativeEvent->EventNumber;
        BindingId = nativeEvent->BindId;
        ClientId = nativeEvent->ClientId;
        ConnectionId = nativeEvent->ConnectionId;
        Cookies = Utf8.DecodeRequired(nativeEvent->Cookies);
    }

    /// <summary>Gets the window that received this event.</summary>
    public WebUiWindow Window => _window;

    /// <summary>Gets the native window identifier.</summary>
    public nuint WindowId { get; }

    /// <summary>Gets the event's category.</summary>
    public WebUiEventType EventType { get; }

    /// <summary>Gets the bound element or JavaScript object name.</summary>
    public string Element { get; }

    /// <summary>Gets WebUI's event identifier.</summary>
    public nuint EventNumber { get; }

    /// <summary>Gets WebUI's binding identifier.</summary>
    public nuint BindingId { get; }

    /// <summary>Gets the browser client identifier.</summary>
    public nuint ClientId { get; }

    /// <summary>Gets the client's current connection identifier.</summary>
    public nuint ConnectionId { get; }

    /// <summary>Gets the complete cookie header sent by the client.</summary>
    public string Cookies { get; }

    /// <summary>Gets the number of arguments sent with this event.</summary>
    public nuint ArgumentCount
    {
        get
        {
            ThrowIfInactive();
            return WebUiNative.GetCount(_nativeEvent);
        }
    }

    /// <summary>Gets an argument as a signed 64-bit integer.</summary>
    /// <exception cref="FormatException">The argument is not a JavaScript-compatible 64-bit integer.</exception>
    public long GetInt64(nuint index = 0)
    {
        ThrowIfInactive();
        ValidateArgumentIndex(index);
        return WebUiArgumentParser.ParseInt64(Utf8.DecodeRequired(WebUiNative.GetStringAt(_nativeEvent, index)));
    }

    /// <summary>Gets an argument as a double-precision floating-point number.</summary>
    /// <exception cref="FormatException">The argument is not a JavaScript-compatible floating-point value.</exception>
    public double GetDouble(nuint index = 0)
    {
        ThrowIfInactive();
        ValidateArgumentIndex(index);
        return WebUiArgumentParser.ParseDouble(Utf8.DecodeRequired(WebUiNative.GetStringAt(_nativeEvent, index)));
    }

    /// <summary>Gets an argument as a Boolean value.</summary>
    /// <exception cref="FormatException">The argument is not a JavaScript-compatible Boolean value.</exception>
    public bool GetBoolean(nuint index = 0)
    {
        ThrowIfInactive();
        ValidateArgumentIndex(index);
        return WebUiArgumentParser.ParseBoolean(Utf8.DecodeRequired(WebUiNative.GetStringAt(_nativeEvent, index)));
    }

    /// <summary>Gets an argument as a UTF-8 string.</summary>
    public string GetString(nuint index = 0)
    {
        ThrowIfInactive();
        ValidateArgumentIndex(index);
        return Utf8.DecodeRequired(WebUiNative.GetStringAt(_nativeEvent, index));
    }

    /// <summary>Copies an argument's raw bytes into managed memory.</summary>
    public byte[] GetBytes(nuint index = 0)
    {
        ThrowIfInactive();
        ValidateArgumentIndex(index);

        var size = WebUiNative.GetSizeAt(_nativeEvent, index);
        if (size > int.MaxValue)
        {
            throw new InvalidOperationException("The WebUI argument is too large for a managed array.");
        }

        var source = WebUiNative.GetStringAt(_nativeEvent, index);
        return source is null || size == 0
            ? []
            : new ReadOnlySpan<byte>(source, checked((int)size)).ToArray();
    }

    /// <summary>Sends JavaScript to only the client that raised this event.</summary>
    public void RunJavaScript(string script)
    {
        ThrowIfInactive();
        var bytes = Utf8.Encode(script, nameof(script));
        fixed (byte* value = bytes)
        {
            WebUiNative.RunClient(_nativeEvent, value);
        }
    }

    /// <summary>Navigates only the client that raised this event.</summary>
    public void Navigate(string url)
    {
        ThrowIfInactive();
        var bytes = Utf8.Encode(url, nameof(url));
        fixed (byte* value = bytes)
        {
            WebUiNative.NavigateClient(_nativeEvent, value);
        }
    }

    /// <summary>Shows content to only the client that raised this event.</summary>
    /// <exception cref="WebUiException">The native runtime could not show the content.</exception>
    public void Show(string content)
    {
        ThrowIfInactive();
        var bytes = Utf8.Encode(content, nameof(content));
        fixed (byte* value = bytes)
        {
            if (WebUiNative.ShowClient(_nativeEvent, value) == 0)
            {
                throw WebUiApplication.CreateNativeException();
            }
        }
    }

    /// <summary>Sends raw bytes to only the client that raised this event.</summary>
    public void SendRaw(string function, ReadOnlySpan<byte> data)
    {
        ThrowIfInactive();
        var functionBytes = Utf8.Encode(function, nameof(function));
        fixed (byte* functionValue = functionBytes)
        fixed (byte* dataValue = data)
        {
            WebUiNative.SendRawClient(_nativeEvent, functionValue, dataValue, (nuint)data.Length);
        }
    }

    /// <summary>Closes only the client that raised this event.</summary>
    public void CloseClient()
    {
        ThrowIfInactive();
        WebUiNative.CloseClient(_nativeEvent);
    }

    /// <summary>Sends a response to the JavaScript caller.</summary>
    /// <exception cref="InvalidOperationException">A response was already sent or the callback is no longer active.</exception>
    public void Respond(WebUiResult result)
    {
        ThrowIfInactive();
        if (!TryRespond(result))
        {
            throw new InvalidOperationException("This WebUI event has already received a response.");
        }
    }

    internal bool TryRespond(WebUiResult result)
    {
        if (Interlocked.CompareExchange(ref _responded, 1, 0) != 0)
        {
            return false;
        }

        try
        {
            switch (result.Kind)
            {
                case WebUiResultKind.None:
                    ReturnString(string.Empty);
                    break;
                case WebUiResultKind.Int64:
                    WebUiNative.ReturnInt(_nativeEvent, result.Int64Value);
                    break;
                case WebUiResultKind.Double:
                    // WebUI's native webui_return_float() uses the process locale and
                    // formats a fixed six decimal places. The JavaScript bridge returns
                    // all responses as text, so a culture-invariant round-trip string is
                    // safer and preserves the actual double value for high-level callers.
                    ReturnString(result.DoubleValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case WebUiResultKind.Boolean:
                    WebUiNative.ReturnBool(_nativeEvent, WebUiApplication.NativeBoolean(result.BooleanValue));
                    break;
                case WebUiResultKind.String:
                    ReturnString(result.StringValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }

            return true;
        }
        catch
        {
            // No native response was issued. Keep the event available so the
            // dispatcher can send its fallback reply rather than leave JS waiting.
            Volatile.Write(ref _responded, 0);
            throw;
        }
    }

    internal void Invalidate() => Volatile.Write(ref _active, 0);

    private void ReturnString(string value)
    {
        var bytes = Utf8.Encode(value, nameof(value));
        fixed (byte* response = bytes)
        {
            if (value.Length == 0)
            {
                // webui_return_string() exits early for an empty C string and,
                // with asynchronous_response enabled, never marks the event done.
                // The wrapper-interface setter reaches the same event state and
                // correctly completes an intentionally empty response.
                WebUiNative.InterfaceSetResponse(_nativeEvent->Window, _nativeEvent->EventNumber, response);
            }
            else
            {
                WebUiNative.ReturnString(_nativeEvent, response);
            }
        }
    }

    private void ValidateArgumentIndex(nuint index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, WebUiNative.GetCount(_nativeEvent), nameof(index));
    }

    private void ThrowIfInactive()
    {
        if (Volatile.Read(ref _active) == 0)
        {
            throw new InvalidOperationException("This WebUI event is no longer active. Copy the values needed by background work before the callback returns.");
        }
    }
}
