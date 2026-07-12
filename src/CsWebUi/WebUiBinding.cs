namespace CsWebUi;

/// <summary>Represents one managed callback registered with a <see cref="WebUiWindow"/>.</summary>
public sealed class WebUiBinding : IDisposable
{
    private WebUiWindow? _window;

    internal WebUiBinding(WebUiWindow window, nuint id, string element)
    {
        _window = window;
        Id = id;
        Element = element;
    }

    /// <summary>Gets WebUI's binding identifier.</summary>
    public nuint Id { get; }

    /// <summary>Gets the bound HTML element or JavaScript object name.</summary>
    public string Element { get; }

    /// <summary>Stops dispatching this binding to managed code.</summary>
    /// <remarks>WebUI has no native unbind operation; this only removes the managed callback.</remarks>
    public void Dispose()
    {
        Interlocked.Exchange(ref _window, null)?.RemoveBinding(Id);
        GC.SuppressFinalize(this);
    }
}
