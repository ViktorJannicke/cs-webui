using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

const string magic = "CSWVFS01";

if (args.Length is not (2 or 4)
    || (args.Length == 4 && !string.Equals(args[2], "--exclude", StringComparison.Ordinal)))
{
    Console.Error.WriteLine(
        "Usage: CsWebUi.Packer <source-directory> <destination-archive> " +
        "[--exclude <semicolon-separated-relative-paths>]");
    return 2;
}

var sourceDirectory = Path.GetFullPath(args[0]);
var destination = Path.GetFullPath(args[1]);
if (!Directory.Exists(sourceDirectory))
{
    Console.Error.WriteLine($"Source directory '{sourceDirectory}' does not exist.");
    return 3;
}

var excludedPaths = args.Length == 4
    ? args[3]
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(static path => path.Replace('\\', '/'))
        .ToHashSet(StringComparer.Ordinal)
    : [];
var files = EnumerateFiles(sourceDirectory, excludedPaths).ToArray();
using var compressible = new GroupBuilder();
using var stored = new GroupBuilder();
var entries = new List<ArchiveEntry>(files.Length);

foreach (var file in files)
{
    var path = Path.GetRelativePath(sourceDirectory, file)
        .Replace(Path.DirectorySeparatorChar, '/');
    var content = File.ReadAllBytes(file);
    var group = IsAlreadyCompressed(path) ? stored : compressible;
    var offset = group.Add(content);
    entries.Add(new ArchiveEntry(
        path,
        group,
        offset,
        content.LongLength,
        SHA256.HashData(content)));
}

var groups = new List<PackedGroup>(2);
AddGroup(compressible, tryBrotli: true, groups);
AddGroup(stored, tryBrotli: false, groups);

var groupIndexes = groups
    .Select((group, index) => (group.Source, Index: index))
    .ToDictionary(static item => item.Source, static item => item.Index);

var destinationDirectory = Path.GetDirectoryName(destination);
if (!string.IsNullOrEmpty(destinationDirectory))
{
    Directory.CreateDirectory(destinationDirectory);
}

var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
try
{
    using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
    using (var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: false))
    {
        writer.Write(Encoding.ASCII.GetBytes(magic));
        writer.Write(entries.Count);
        writer.Write(groups.Count);

        foreach (var entry in entries)
        {
            var path = Encoding.UTF8.GetBytes(entry.Path);
            writer.Write(path.Length);
            writer.Write(path);
            writer.Write(groupIndexes[entry.Group]);
            writer.Write(entry.Offset);
            writer.Write(entry.Length);
            writer.Write(entry.Hash);
        }

        foreach (var group in groups)
        {
            writer.Write((byte)group.Compression);
            writer.Write(group.Source.Length);
            writer.Write(group.Payload.LongLength);
            writer.Write(group.Payload);
        }
    }

    File.Move(temporary, destination, overwrite: true);
}
finally
{
    if (File.Exists(temporary))
    {
        File.Delete(temporary);
    }
}

var originalSize = entries.Sum(static entry => entry.Length);
var archiveSize = new FileInfo(destination).Length;
Console.WriteLine(
    $"Packed {entries.Count} files ({originalSize} bytes) into {archiveSize} bytes " +
    $"using {groups.Count(static group => group.Compression == CompressionKind.Brotli)} Brotli " +
    $"and {groups.Count(static group => group.Compression == CompressionKind.Stored)} stored group(s).");
return 0;

static IEnumerable<string> EnumerateFiles(string root, HashSet<string> excludedPaths)
{
    var options = new EnumerationOptions
    {
        RecurseSubdirectories = true,
        AttributesToSkip = FileAttributes.ReparsePoint,
    };

    return Directory
        .EnumerateFiles(root, "*", options)
        .Where(path => !excludedPaths.Contains(
            Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/')))
        .Order(StringComparer.Ordinal);
}

static bool IsAlreadyCompressed(string path)
    => Path.GetExtension(path).ToLowerInvariant() is
        ".7z" or ".avif" or ".br" or ".gif" or ".gz" or ".ico" or ".jpeg" or ".jpg"
        or ".mp3" or ".mp4" or ".ogg" or ".opus" or ".pdf" or ".png" or ".rar" or ".webm"
        or ".webp" or ".woff" or ".woff2" or ".zip";

static void AddGroup(GroupBuilder source, bool tryBrotli, List<PackedGroup> destination)
{
    if (source.FileCount == 0)
    {
        return;
    }

    var raw = source.ToArray();
    if (tryBrotli)
    {
        using var compressedOutput = new MemoryStream();
        using (var brotli = new BrotliStream(
            compressedOutput,
            CompressionLevel.SmallestSize,
            leaveOpen: true))
        {
            brotli.Write(raw);
        }

        var compressed = compressedOutput.ToArray();
        if (compressed.Length < raw.Length)
        {
            destination.Add(new PackedGroup(source, CompressionKind.Brotli, compressed));
            return;
        }
    }

    destination.Add(new PackedGroup(source, CompressionKind.Stored, raw));
}

internal sealed class GroupBuilder : IDisposable
{
    private readonly MemoryStream _content = new();

    internal int FileCount { get; private set; }

    internal long Length => _content.Length;

    internal long Add(byte[] content)
    {
        var offset = _content.Position;
        _content.Write(content);
        FileCount++;
        return offset;
    }

    internal byte[] ToArray() => _content.ToArray();

    public void Dispose() => _content.Dispose();
}

internal sealed record ArchiveEntry(
    string Path,
    GroupBuilder Group,
    long Offset,
    long Length,
    byte[] Hash);

internal sealed record PackedGroup(
    GroupBuilder Source,
    CompressionKind Compression,
    byte[] Payload);

internal enum CompressionKind : byte
{
    Stored = 0,
    Brotli = 1,
}
