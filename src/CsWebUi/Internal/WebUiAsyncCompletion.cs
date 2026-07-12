namespace CsWebUi.Internal;

internal static class WebUiAsyncCompletion
{
    internal static async Task CompleteAsync(
        ValueTask<WebUiResult> operation,
        WebUiEvent webUiEvent,
        Action<Exception, WebUiEvent> reportFailure,
        Action<WebUiEvent> complete)
    {
        try
        {
            webUiEvent.TryRespond(await operation.ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            reportFailure(exception, webUiEvent);
        }
        finally
        {
            complete(webUiEvent);
        }
    }
}
