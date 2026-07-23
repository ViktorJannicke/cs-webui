using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using CsWebUi;

namespace CsWebUi.Tests;

public sealed class WebUiVirtualFileSystemTests
{
    [Fact]
    public void ZipArchiveLoadsViteStylePathsIntoMemory()
    {
        using var archive = CreateZip(
            ("index.html", "<script src=\"/assets/index-A1.js\"></script>"),
            ("assets/index-A1.js", "console.log('embedded');"),
            ("assets/logo.svg", "<svg/>"));

        var fileSystem = WebUiVirtualFileSystem.FromArchive(archive);

        Assert.Equal(
            ["assets/index-A1.js", "assets/logo.svg", "index.html"],
            fileSystem.Files);
        AssertFile(fileSystem, "/", "<script src=\"/assets/index-A1.js\"></script>");
        AssertFile(fileSystem, "/assets/index-A1.js?cache=1", "console.log('embedded');");
        AssertFile(fileSystem, "/assets/logo.svg", "<svg/>");
    }

    [Fact]
    public void BrotliTarArchiveIsDecompressedAndRetained()
    {
        using var archive = CreateBrotliTar(
            ("index.html", "<h1>Brotli</h1>"),
            ("assets/app.css", "body { color: rebeccapurple; }"));

        var fileSystem = WebUiVirtualFileSystem.FromArchive(archive);
        archive.Dispose();

        AssertFile(fileSystem, "index.html", "<h1>Brotli</h1>");
        AssertFile(fileSystem, "assets/app.css", "body { color: rebeccapurple; }");
    }

    [Fact]
    public void OptimizedArchiveLoadsBrotliAndStoredGroups()
    {
        using var archive = CreateOptimizedArchive();

        var fileSystem = WebUiVirtualFileSystem.FromArchive(archive);
        archive.Dispose();

        AssertFile(fileSystem, "index.html", "<h1>Solid Brotli</h1>");
        Assert.True(fileSystem.TryGetFile("assets/logo.png", out var image));
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4e, 0x47 }, image.ToArray());
    }

    [Fact]
    public void OptimizedArchiveRejectsCorruptedFiles()
    {
        using var archive = CreateOptimizedArchive(corruptHash: true);

        var exception = Assert.Throws<InvalidDataException>(
            () => WebUiVirtualFileSystem.FromArchive(archive));

        Assert.Contains("integrity", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DirectoryIndexesAndOptionalSpaFallbackResolve()
    {
        using var archive = CreateZip(
            ("index.html", "root"),
            ("settings/index.html", "settings"));
        var fileSystem = WebUiVirtualFileSystem.FromArchive(
            archive,
            new WebUiVirtualFileSystemOptions
            {
                EnableSinglePageApplicationFallback = true,
            });

        AssertFile(fileSystem, "/settings/", "settings");
        AssertFile(fileSystem, "/client/route", "root");
        Assert.False(fileSystem.TryGetFile("/missing.js", out _));
    }

    [Theory]
    [InlineData("/../secret.txt")]
    [InlineData("/%2e%2e/secret.txt")]
    [InlineData("/assets\\secret.txt")]
    public void TraversalAndBackslashPathsAreRejected(string path)
    {
        using var archive = CreateZip(("index.html", "safe"));
        var fileSystem = WebUiVirtualFileSystem.FromArchive(archive);

        Assert.False(fileSystem.TryGetFile(path, out _));
    }

    [Fact]
    public void HttpResponseContainsMimeLengthAndBody()
    {
        using var archive = CreateZip(("assets/app.js", "export default 42;"));
        var fileSystem = WebUiVirtualFileSystem.FromArchive(archive);

        var response = Encoding.UTF8.GetString(fileSystem.GetHttpResponse("/assets/app.js"));

        Assert.StartsWith("HTTP/1.1 200 OK\r\n", response, StringComparison.Ordinal);
        Assert.Contains("Content-Type: text/javascript; charset=utf-8\r\n", response, StringComparison.Ordinal);
        Assert.Contains("Content-Length: 18\r\n", response, StringComparison.Ordinal);
        Assert.EndsWith("\r\n\r\nexport default 42;", response, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingFileProducesAComplete404Response()
    {
        using var archive = CreateZip(("index.html", "safe"));
        var fileSystem = WebUiVirtualFileSystem.FromArchive(archive);

        var response = Encoding.UTF8.GetString(fileSystem.GetHttpResponse("/missing.js"));

        Assert.StartsWith("HTTP/1.1 404 Not Found\r\n", response, StringComparison.Ordinal);
        Assert.EndsWith("\r\n\r\nNot found", response, StringComparison.Ordinal);
    }

    [Fact]
    public void ArchiveLimitsAreEnforcedBeforeDecompression()
    {
        using var archive = CreateZip(("large.txt", "12345"));

        Assert.Throws<InvalidDataException>(() =>
            WebUiVirtualFileSystem.FromArchive(
                archive,
                new WebUiVirtualFileSystemOptions
                {
                    MaxTotalUncompressedBytes = 4,
                }));
    }

    [NativeFact]
    public async Task NativeServerServesVirtualFilesInDirectAndAsyncResponseModes()
    {
        using var archive = CreateZip(
            ("index.html", "<h1>Native VFS</h1>"),
            ("assets/app.js", "console.log('native');"));
        var fileSystem = WebUiVirtualFileSystem.FromArchive(archive);
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10),
        };
        using var window = new WebUiWindow();
        window.SetVirtualFileSystem(fileSystem);
        var serverUrl = window.StartServer("index.html");
        await AssertServerResponse(client, serverUrl);

        WebUiApplication.SetConfiguration(WebUiConfiguration.AsynchronousResponse, true);
        try
        {
            await AssertServerResponse(client, serverUrl);
        }
        finally
        {
            WebUiApplication.SetConfiguration(WebUiConfiguration.AsynchronousResponse, false);
        }
    }

    private static MemoryStream CreateZip(params (string Path, string Content)[] files)
    {
        var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var entry = zip.CreateEntry(file.Path, CompressionLevel.SmallestSize);
                using var writer = new StreamWriter(
                    entry.Open(),
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    leaveOpen: false);
                writer.Write(file.Content);
            }
        }

        output.Position = 0;
        return output;
    }

    private static MemoryStream CreateBrotliTar(params (string Path, string Content)[] files)
    {
        var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            using var writer = new TarWriter(brotli, leaveOpen: true);
            foreach (var file in files)
            {
                var entry = new PaxTarEntry(TarEntryType.RegularFile, file.Path)
                {
                    DataStream = new MemoryStream(Encoding.UTF8.GetBytes(file.Content)),
                };
                writer.WriteEntry(entry);
            }
        }

        output.Position = 0;
        return output;
    }

    private static MemoryStream CreateOptimizedArchive(bool corruptHash = false)
    {
        var text = Encoding.UTF8.GetBytes("<h1>Solid Brotli</h1>");
        var image = new byte[] { 0x89, 0x50, 0x4e, 0x47 };
        var compressedText = new MemoryStream();
        using (var brotli = new BrotliStream(
            compressedText,
            CompressionLevel.SmallestSize,
            leaveOpen: true))
        {
            brotli.Write(text);
        }

        var textHash = SHA256.HashData(text);
        if (corruptHash)
        {
            textHash[0] ^= 0xff;
        }

        var output = new MemoryStream();
        using (var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("CSWVFS01"u8);
            writer.Write(2);
            writer.Write(2);
            WriteOptimizedEntry(writer, "index.html", group: 0, text.LongLength, textHash);
            WriteOptimizedEntry(writer, "assets/logo.png", group: 1, image.LongLength, SHA256.HashData(image));

            writer.Write((byte)1);
            writer.Write(text.LongLength);
            writer.Write(compressedText.Length);
            writer.Write(compressedText.ToArray());

            writer.Write((byte)0);
            writer.Write(image.LongLength);
            writer.Write(image.LongLength);
            writer.Write(image);
        }

        output.Position = 0;
        return output;
    }

    private static void WriteOptimizedEntry(
        BinaryWriter writer,
        string path,
        int group,
        long length,
        byte[] hash)
    {
        var pathBytes = Encoding.UTF8.GetBytes(path);
        writer.Write(pathBytes.Length);
        writer.Write(pathBytes);
        writer.Write(group);
        writer.Write(0L);
        writer.Write(length);
        writer.Write(hash);
    }

    private static void AssertFile(
        WebUiVirtualFileSystem fileSystem,
        string path,
        string expected)
    {
        Assert.True(fileSystem.TryGetFile(path, out var content));
        Assert.Equal(expected, Encoding.UTF8.GetString(content.Span));
    }

    private static async Task AssertServerResponse(HttpClient client, string serverUrl)
    {
        Assert.Equal("<h1>Native VFS</h1>", await client.GetStringAsync(serverUrl));
        Assert.Equal(
            "console.log('native');",
            await client.GetStringAsync(new Uri(new Uri(serverUrl), "assets/app.js")));
    }
}
