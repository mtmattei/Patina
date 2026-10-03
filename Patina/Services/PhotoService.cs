using Microsoft.UI.Dispatching;
using Patina.Core;
using Patina.Core.Model;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Patina.Services;

public interface IPhotoService
{
    /// <summary>True where the platform has a camera capture UI (Android, iOS).</summary>
    bool CanCapture { get; }

    /// <summary>Lets the user pick an image file; returns null on cancel.</summary>
    Task<PhotoRef?> PickAsync(CancellationToken ct);

    /// <summary>Opens the camera; returns null on cancel or where capture is not available.</summary>
    Task<PhotoRef?> CaptureAsync(CancellationToken ct);
}

/// <summary>
/// Copies picked or captured images into <c>LocalFolder/photos</c>, so a survey keeps its photos even when the
/// original file is moved or deleted. Pickers and the camera must run on the UI thread; MVUX commands may not,
/// so both are marshalled through the window's dispatcher.
/// </summary>
public sealed class PhotoService(Window window) : IPhotoService
{
    private static readonly string[] ImageTypes = [".jpg", ".jpeg", ".png", ".webp", ".heic"];

    public static string PhotosFolder => Path.Combine(ApplicationData.Current.LocalFolder.Path, "photos");

    public static string PathFor(string fileName) => Path.Combine(PhotosFolder, fileName);

    public bool CanCapture => OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    public Task<PhotoRef?> PickAsync(CancellationToken ct) => OnUiThread(async () =>
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            ViewMode = PickerViewMode.Thumbnail,
        };

        foreach (var type in ImageTypes)
        {
            picker.FileTypeFilter.Add(type);
        }

        var file = await picker.PickSingleFileAsync();
        return file is null ? null : await StoreAsync(file, ct);
    });

    public Task<PhotoRef?> CaptureAsync(CancellationToken ct) => OnUiThread(async () =>
    {
        if (!CanCapture)
        {
            return null;
        }

        var capture = new CameraCaptureUI();
        capture.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
        var file = await capture.CaptureFileAsync(CameraCaptureUIMode.Photo);
        return file is null ? null : await StoreAsync(file, ct);
    });

    private static async Task<PhotoRef> StoreAsync(StorageFile file, CancellationToken ct)
    {
        Directory.CreateDirectory(PhotosFolder);
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        var photo = new PhotoRef(Ids.New(), string.IsNullOrEmpty(extension) ? ".jpg" : extension, file.Name, DateTimeOffset.Now);

        await using var source = await file.OpenStreamForReadAsync();
        await using var target = File.Create(PathFor(photo.FileName));
        await source.CopyToAsync(target, ct);
        return photo;
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
