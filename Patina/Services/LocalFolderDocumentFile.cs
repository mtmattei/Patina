using Patina.Core.Storage;
using Windows.Storage;

namespace Patina.Services;

/// <summary>
/// Keeps <c>patina.json</c> in <see cref="ApplicationData.LocalFolder"/>. Writes go to a temporary file first and
/// then replace the document, so an interrupted write never leaves a half-written collection behind.
/// </summary>
public sealed class LocalFolderDocumentFile : IDocumentFile
{
    private const string FileName = "patina.json";

    public static string Folder => ApplicationData.Current.LocalFolder.Path;

    private static string DocumentPath => Path.Combine(Folder, FileName);

    public async Task<string?> ReadAsync(CancellationToken ct) =>
        File.Exists(DocumentPath) ? await File.ReadAllTextAsync(DocumentPath, ct) : null;

    public async Task WriteAsync(string json, CancellationToken ct)
    {
        Directory.CreateDirectory(Folder);
        var temp = DocumentPath + ".tmp";
        await File.WriteAllTextAsync(temp, json, ct);
        File.Move(temp, DocumentPath, overwrite: true);
    }

    public Task MoveAsideAsync(string reason, CancellationToken ct)
    {
        if (File.Exists(DocumentPath))
        {
            File.Move(DocumentPath, Path.Combine(Folder, $"patina.{reason}-{DateTime.Now:yyyyMMdd-HHmmss}.json"), overwrite: true);
        }

        return Task.CompletedTask;
    }
}
