<p align="center">
  <img src="assets/cswebui-logo.svg" width="520" alt="CsWebUi">
</p>

# CsWebUi

Modern .NET 10 bindings for [WebUI](https://github.com/webui-dev/webui): use an installed browser or supported WebView as a lightweight cross-platform desktop UI.

`CsWebUi.Native` is a direct, unsafe C ABI layer for WebUI 2.5.0-beta.4. `CsWebUi` adds deterministic window ownership, UTF-8 conversion, error handling, raw-data helpers, and safe synchronous or `ValueTask`-based callbacks.

> This project follows WebUI's 2.5 beta ABI and is itself released as a prerelease package.

## Use it

```bash
dotnet add package CsWebUi --prerelease
```

```csharp
using CsWebUi;

using var window = new WebUiWindow();

window.Bind("multiply", static e =>
    WebUiResult.FromInt64(e.GetInt64() * e.GetInt64(1)));

window.BindAsync("greet", static (e, cancellationToken) =>
{
    cancellationToken.ThrowIfCancellationRequested();
    return ValueTask.FromResult<WebUiResult>($"Hello, {e.GetString()}!");
});

window.Show("""
    <!doctype html>
    <script src="webui.js"></script>
    <button onclick="multiply(6, 7).then(alert)">Multiply</button>
    <button onclick="greet('WebUI').then(alert)">Greet</button>
    """);

WebUiApplication.Wait();
```

`WebUiWindow.Dispose()` destroys the native window and safely defers final destruction until active managed callbacks finish. Async bindings automatically opt WebUI into its asynchronous-response mode; return a `WebUiResult` to resolve the JavaScript promise.

## Embed a Vite build

The `CsWebUi` package can pack a complete Vite `dist` directory into an
optimized manifest resource during build. The resource becomes part of the
application assembly and therefore also remains inside a single-file
executable:

```xml
<PropertyGroup>
  <WebUiDist>..\ClientInstaller.WebUi\dist</WebUiDist>
</PropertyGroup>
```

Load it once at startup, attach it to a window, and show its entry point:

```csharp
using System.Reflection;
using CsWebUi;

var files = WebUiVirtualFileSystem.FromEmbeddedArchive(
    Assembly.GetExecutingAssembly());

using var window = new WebUiWindow();
window.SetVirtualFileSystem(files);
window.Show("index.html");
WebUiApplication.Wait();
```

The deterministic packer groups compressible content such as HTML, CSS,
JavaScript, JSON, SVG, TTF, and WASM into one solid Brotli stream. Formats that
are already compressed, including WOFF/WOFF2, PNG, JPEG, WebP, AVIF, ZIP, and
video, are stored without another compression pass. Brotli is retained only
when its measured result is smaller than the original group. A compact
manifest records paths, offsets, lengths, and SHA-256 hashes.
Packing is incremental: it reruns when the project, packer, target, or a file
below `WebUiDist` is newer than the generated archive.

Every compressed group is decompressed eagerly and retained in memory. Files
are memory slices over their shared group buffers, avoiding a second copy of
the full web application. Integrity is checked while loading. HTTP responses
are built lazily in pinned managed memory, so no asset is extracted to disk.
Paths are URL-decoded and traversal-safe, matching Vite references such as
`/assets/index-D80-FDF7.js`. Missing files produce an in-memory 404 instead of
falling through to the host file system.

`WebUiDistExclude` accepts semicolon-separated, root-relative files that should
not be packed. It defaults to `webui.tar.br`, preventing a previously generated
archive from being nested into a new one:

```xml
<PropertyGroup>
  <WebUiDistExclude>webui.tar.br;stats.html</WebUiDistExclude>
</PropertyGroup>
```

For an externally produced archive, set `WebUiEmbeddedArchive` instead. ZIP
and Brotli-compressed TAR (`.tar.br`) archives remain supported and are
detected automatically:

```xml
<PropertyGroup>
  <WebUiEmbeddedArchive>..\ClientInstaller.WebUi\dist\webui.tar.br</WebUiEmbeddedArchive>
</PropertyGroup>
```

The default resource name is `CsWebUi.StaticFiles`. Set
`WebUiEmbeddedResourceName` in the project and pass the same name to
`FromEmbeddedArchive` when multiple or custom resources are needed.

Client-side routers can opt into an `index.html` fallback:

```csharp
var files = WebUiVirtualFileSystem.FromEmbeddedArchive(
    Assembly.GetExecutingAssembly(),
    options: new WebUiVirtualFileSystemOptions
    {
        EnableSinglePageApplicationFallback = true,
    });
```

`WebUiVirtualFileSystem.FromDirectory` provides the same serving behavior
without embedding and is useful during development. Archive loading enforces
configurable file-count and uncompressed-size limits to avoid accidental
decompression bombs.

WebUI disables its authentication-cookie check when any custom file handler is
installed. Keep the window private (the default), and do not call
`SetPublic(true)` for untrusted networks without adding an authentication layer.

## Packages

| Package | Purpose |
| --- | --- |
| `CsWebUi.Native` | Full low-level C ABI, `LibraryImport`, pointers, native enums, callbacks, and library override support. |
| `CsWebUi` | Friendly window, event, callback, JavaScript, browser/server, and lifecycle APIs. |

Release packages bundle the standard, non-TLS WebUI shared library for `win-x64`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`. The raw TLS API remains available when an application supplies a secure custom WebUI build.

The bundled Windows shared library statically links the MSVC runtime, matching the official WebUI Windows distribution and avoiding a separate Visual C++ Redistributable prerequisite.

### Optional Windows NativeAOT static linking

Windows `win-x64` NativeAOT applications can opt into linking WebUI and the
WebView2 loader directly into the application executable:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <CsWebUiStaticLink>true</CsWebUiStaticLink>
</PropertyGroup>
```

Publish normally with `dotnet publish`. The resulting publish directory does
not need `webui-2.dll` or `WebView2Loader.dll`. The Microsoft Edge WebView2
Runtime itself remains a system prerequisite when embedded WebView mode is
used.

Static linking is opt-in and currently supports only `win-x64`. Without
`CsWebUiStaticLink`, the package retains its normal dynamic-library behavior.
`WebUiNativeLibrary.SetLibraryPath` and `CSWEBUI_NATIVE_LIBRARY` are bypassed
in static mode because NativeAOT resolves the WebUI entry points at link time.
The WebView2 loader redistribution terms are included in the package under
`licenses/WebView2`.

For a custom or locally built native library, configure it before the first WebUI call:

```csharp
CsWebUi.Native.WebUiNativeLibrary.SetLibraryPath("/path/to/libwebui-2.so");
```

Alternatively set `CSWEBUI_NATIVE_LIBRARY` to a library file or its containing directory.

## NixOS development

The flake pins both Nixpkgs and the upstream WebUI source revision. It builds `webui-2`, exposes it through `CSWEBUI_NATIVE_LIBRARY`, and includes .NET 10, CMake, Chromium, Xvfb, and Linux WebView dependencies.

```bash
nix develop
dotnet test
dotnet run --project samples/CsWebUi.BasicSample
```

Useful flake outputs:

```bash
nix build .#webui-native
nix flake check
```

## Samples

- `CsWebUi.BasicSample` is the smallest possible callback example.
- [`CsWebUi.HighLevelSample`](samples/CsWebUi.HighLevelSample) is a complete local web application showcasing the safe window, event, async callback, binary-message, and JavaScript APIs.

## Design notes

- The raw package uses explicit Cdecl `LibraryImport` declarations, `nuint` for `size_t`, one-byte C booleans, and unmanaged function pointers.
- Both packages are trimming- and NativeAOT-oriented. The high-level callback dispatcher has no reflection-based registration.
- The high-level API parses JavaScript numeric arguments and serializes double results with the invariant culture, avoiding process-locale differences in WebUI's raw float helpers.
- Empty high-level callback results complete correctly when async bindings are present; WebUI's direct empty-string helper otherwise leaves that response pending.
- Strings passed to WebUI reject embedded null characters instead of silently truncating at the native C-string boundary.
- WebUI owns pointers returned from event accessors and other borrowed APIs. The safe `WebUiEvent` wrapper invalidates access after the callback completes.
- Browser mode needs an installed browser. Embedded WebView mode has platform dependencies, including the WebView2 runtime/loader on Windows and GTK/WebKit on Linux.

## License

CsWebUi is MIT licensed. WebUI is also MIT licensed; its attribution is retained in [NOTICE](NOTICE).
