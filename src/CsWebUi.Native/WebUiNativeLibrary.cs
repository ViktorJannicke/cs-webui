using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace CsWebUi.Native;

/// <summary>
/// Configures resolution of the native <c>webui-2</c> library before the first
/// call into <see cref="WebUiNative"/>.
/// </summary>
/// <remarks>
/// Dynamic library overrides are bypassed when an application is published with
/// <c>CsWebUiStaticLink=true</c>.
/// </remarks>
public static class WebUiNativeLibrary
{
    internal const string LibraryName = "webui-2";

    private const string EnvironmentVariableName = "CSWEBUI_NATIVE_LIBRARY";
    private static readonly object Gate = new();
    private static string? configuredPath;
    private static bool resolverInstalled;
    private static int libraryRequested;

    /// <summary>
    /// Gets the configured custom library path, if any. The path may name either
    /// the native binary itself or the directory that contains it.
    /// </summary>
    public static string? ConfiguredPath
    {
        get
        {
            lock (Gate)
            {
                return configuredPath;
            }
        }
    }

    /// <summary>
    /// Sets an absolute or relative path to a custom WebUI native library, or to
    /// the directory containing it. Call this before any <see cref="WebUiNative"/>
    /// method. The <c>CSWEBUI_NATIVE_LIBRARY</c> environment variable is used when
    /// no explicit path is set.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">A native call has already been attempted.</exception>
    public static void SetLibraryPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        lock (Gate)
        {
            if (Volatile.Read(ref libraryRequested) != 0)
            {
                throw new InvalidOperationException(
                    "The WebUI native library path cannot be changed after the first native call.");
            }

            if (configuredPath is not null && !PathComparer.Equals(configuredPath, fullPath))
            {
                throw new InvalidOperationException(
                    "The WebUI native library path has already been configured for this process.");
            }

            configuredPath = fullPath;
        }
    }

    internal static void EnsureResolverInstalled()
    {
        lock (Gate)
        {
            if (resolverInstalled)
            {
                return;
            }

            if (configuredPath is null)
            {
                var environmentPath = Environment.GetEnvironmentVariable(EnvironmentVariableName);
                if (!string.IsNullOrWhiteSpace(environmentPath))
                {
                    configuredPath = Path.GetFullPath(environmentPath);
                }
            }

            NativeLibrary.SetDllImportResolver(typeof(WebUiNative).Assembly, Resolve);
            resolverInstalled = true;
        }
    }

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
        {
            return IntPtr.Zero;
        }

        Interlocked.Exchange(ref libraryRequested, 1);

        string? path;
        lock (Gate)
        {
            path = configuredPath;
        }

        if (path is null)
        {
            // Returning zero lets .NET locate a bundled runtimes/<rid>/native asset.
            return IntPtr.Zero;
        }

        var candidate = Directory.Exists(path)
            ? Path.Combine(path, GetNativeFileName())
            : path;

        if (NativeLibrary.TryLoad(candidate, out var handle))
        {
            return handle;
        }

        throw new DllNotFoundException(
            $"Unable to load WebUI native library for '{RuntimeInformation.RuntimeIdentifier}'. " +
            $"Configured {EnvironmentVariableName}/SetLibraryPath location: '{path}'. " +
            $"Attempted: '{candidate}'.");
    }

    private static string GetNativeFileName()
    {
        if (OperatingSystem.IsWindows())
        {
            return "webui-2.dll";
        }

        if (OperatingSystem.IsMacOS())
        {
            return "libwebui-2.dylib";
        }

        return "libwebui-2.so";
    }

    private static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
}
