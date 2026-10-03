using Microsoft.UI.Xaml.Media.Imaging;
using Patina.Services;

namespace Patina.Controls;

/// <summary>
/// <c>controls:PhotoSource.FileName="{Binding …}"</c> on an <see cref="Image"/> loads a stored survey photo.
/// Reads the file through a stream rather than a URI, which works the same on every head (including WebAssembly,
/// where local files have no URL). A missing file leaves the image empty and raises <see cref="Image.ImageFailed"/>-style
/// state through <c>PhotoSource.IsMissing</c>.
/// </summary>
public static class PhotoSource
{
    public static readonly DependencyProperty FileNameProperty = DependencyProperty.RegisterAttached(
        "FileName", typeof(string), typeof(PhotoSource), new PropertyMetadata(null, OnFileNameChanged));

    public static readonly DependencyProperty IsMissingProperty = DependencyProperty.RegisterAttached(
        "IsMissing", typeof(bool), typeof(PhotoSource), new PropertyMetadata(false));

    public static string? GetFileName(Image image) => (string?)image.GetValue(FileNameProperty);

    public static void SetFileName(Image image, string? value) => image.SetValue(FileNameProperty, value);

    public static bool GetIsMissing(Image image) => (bool)image.GetValue(IsMissingProperty);

    public static void SetIsMissing(Image image, bool value) => image.SetValue(IsMissingProperty, value);

    private static async void OnFileNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Image image)
        {
            return;
        }

        image.Source = null;
        if (e.NewValue is not string fileName || string.IsNullOrEmpty(fileName))
        {
            SetIsMissing(image, false);
            return;
        }

        var path = PhotoService.PathFor(fileName);
        if (!File.Exists(path))
        {
            SetIsMissing(image, true);
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            await using var stream = File.OpenRead(path);
            await bitmap.SetSourceAsync(stream.AsRandomAccessStream());

            // The binding may have moved on while the file was loading.
            if (GetFileName(image) == fileName)
            {
                image.Source = bitmap;
                SetIsMissing(image, false);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            SetIsMissing(image, true);
        }
    }
}
