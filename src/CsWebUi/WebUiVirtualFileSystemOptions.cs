namespace CsWebUi;

/// <summary>Controls how a <see cref="WebUiVirtualFileSystem"/> loads and resolves files.</summary>
public sealed class WebUiVirtualFileSystemOptions
{
    /// <summary>The conventional file served for a directory path.</summary>
    public string IndexFile { get; init; } = "index.html";

    /// <summary>
    /// Gets or initializes whether extensionless paths that do not exist are resolved to
    /// <see cref="IndexFile"/>. Enable this for Vite applications that use client-side routing.
    /// </summary>
    public bool EnableSinglePageApplicationFallback { get; init; }

    /// <summary>The maximum number of files accepted from one directory or archive.</summary>
    public int MaxFileCount { get; init; } = 100_000;

    /// <summary>The maximum total uncompressed size accepted from one directory or archive.</summary>
    public long MaxTotalUncompressedBytes { get; init; } = 1024L * 1024L * 1024L;
}
