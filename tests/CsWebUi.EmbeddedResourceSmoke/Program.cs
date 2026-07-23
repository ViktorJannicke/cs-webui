using System.Reflection;
using System.Text;
using CsWebUi;

using var suppliedArchive = args.Length == 0 ? null : File.OpenRead(args[0]);
var fileSystem = suppliedArchive is null
    ? WebUiVirtualFileSystem.FromEmbeddedArchive(Assembly.GetExecutingAssembly())
    : WebUiVirtualFileSystem.FromArchive(suppliedArchive);

if (!fileSystem.TryGetFile("/index.html", out var index))
{
    throw new InvalidOperationException("The embedded index was not addressable.");
}

if (suppliedArchive is null
    && Encoding.UTF8.GetString(index.Span) != "<h1>Embedded</h1>\n")
{
    throw new InvalidOperationException("The embedded index had unexpected contents.");
}

if (suppliedArchive is null
    && (!fileSystem.TryGetFile("/assets/app.js", out var script)
        || Encoding.UTF8.GetString(script.Span) != "console.log('embedded');\n"))
{
    throw new InvalidOperationException("The embedded Vite-style asset was not addressable.");
}

Console.WriteLine($"Loaded {fileSystem.Files.Count} embedded files.");
