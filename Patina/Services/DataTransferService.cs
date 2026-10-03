using System.IO.Compression;
using Microsoft.UI.Dispatching;
using Patina.Core;
using Patina.Core.Model;
using Patina.Core.Rules;
using Patina.Core.Storage;
using Windows.Storage.Pickers;

namespace Patina.Services;

public enum TransferOutcome
{
    Done,
    Cancelled,
    Failed,
}

public sealed record ImportPreview(PatinaDocument Document, string FileName);

public interface IDataTransferService
{
    /// <summary>Writes a <c>.patina</c> archive (collection JSON + photo files) to a location the user picks.</summary>
    Task<TransferOutcome> ExportAsync(PatinaDocument doc, CancellationToken ct);

    /// <summary>Reads and validates an archive without applying it. Null when the user cancels.</summary>
    Task<Result<ImportPreview>?> PickImportAsync(CancellationToken ct);

    /// <summary>Copies the archive's photos into the photo folder; call after the document has been replaced.</summary>
    Task RestorePhotosAsync(ImportPreview preview, CancellationToken ct);
}

/// <summary>
/// Export format: a zip with <c>collection.json</c> (an <see cref="ExportEnvelope"/>) and <c>photos/</c>.
/// Photos travel with the record, so a collection moved to another device keeps its documentation.
/// </summary>
public sealed class DataTransferService(Window window, IClock clock) : IDataTransferService
{
    private const string Extension = ".patina";
    private const string DocumentEntry = "collection.json";
    private const string PhotosPrefix = "photos/";

    private string? _pendingArchive;

    public Task<TransferOutcome> ExportAsync(PatinaDocument doc, CancellationToken ct) => OnUiThread(async () =>
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"patina-{clock.Today:yyyy-MM-dd}",
        };
        picker.FileTypeChoices.Add("Patina collection", [Extension]);

        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return TransferOutcome.Cancelled;
        }

        try
        {
            await using var stream = await file.OpenStreamForWriteAsync();
            stream.SetLength(0);
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = zip.CreateEntry(DocumentEntry, CompressionLevel.Optimal);
                await using (var writer = new StreamWriter(entry.Open()))
                {
                    await writer.WriteAsync(PatinaJson.Export(doc, clock.Now));
                }

                foreach (var photo in doc.Photos)
                {
                    var path = PhotoService.PathFor(photo.FileName);
                    if (File.Exists(path))
                    {
                        zip.CreateEntryFromFile(path, PhotosPrefix + photo.FileName, CompressionLevel.NoCompression);
                    }
                }
            }

            return TransferOutcome.Done;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return TransferOutcome.Failed;
        }
    });

    public Task<Result<ImportPreview>?> PickImportAsync(CancellationToken ct) => OnUiThread<Result<ImportPreview>?>(async () =>
    {
        var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
        picker.FileTypeFilter.Add(Extension);
        picker.FileTypeFilter.Add(".zip");

        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return null;
        }

        try
        {
            // Copy the archive locally first: picked files on WebAssembly and Android are streams, not paths.
            var local = Path.Combine(Path.GetTempPath(), $"patina-import-{Ids.New()}.zip");
            await using (var source = await file.OpenStreamForReadAsync())
            await using (var target = File.Create(local))
            {
                await source.CopyToAsync(target, ct);
            }

            using var zip = ZipFile.OpenRead(local);
            var entry = zip.GetEntry(DocumentEntry);
            if (entry is null)
            {
                return Result<ImportPreview>.Fail(Problem.ImportNotPatinaFile);
            }

            using var reader = new StreamReader(entry.Open());
            var parsed = PatinaJson.Import(await reader.ReadToEndAsync(ct));
            if (!parsed.IsOk)
            {
                return Result<ImportPreview>.Fail(parsed.Problem);
            }

            _pendingArchive = local;
            return Result<ImportPreview>.Ok(new ImportPreview(parsed.Value!, file.Name));
        }
        catch (InvalidDataException)
        {
            return Result<ImportPreview>.Fail(Problem.ImportNotPatinaFile);
        }
    });

    public Task RestorePhotosAsync(ImportPreview preview, CancellationToken ct)
    {
        if (_pendingArchive is not { } archive || !File.Exists(archive))
        {
            return Task.CompletedTask;
        }

        Directory.CreateDirectory(PhotoService.PhotosFolder);
        using (var zip = ZipFile.OpenRead(archive))
        {
            var wanted = preview.Document.Photos.Select(p => p.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in zip.Entries.Where(e => e.FullName.StartsWith(PhotosPrefix, StringComparison.Ordinal)))
            {
                var name = Path.GetFileName(entry.FullName);
                if (wanted.Contains(name))
                {
                    entry.ExtractToFile(PhotoService.PathFor(name), overwrite: true);
                }
            }
        }

        File.Delete(archive);
        _pendingArchive = null;
        return Task.CompletedTask;
    }

    private Task<T> OnUiThread<T>(Func<Task<T>> action)
    {
        var dispatcher = window.DispatcherQueue;
        if (dispatcher.HasThreadAccess)
        {
            return action();
        }

        var tcs = new TaskCompletionSource<T>();
        dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
        {
            try
            {
                tcs.SetResult(await action());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }
}
