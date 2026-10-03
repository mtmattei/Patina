using BruTile;
using SkiaSharp;

namespace Patina.Controls;

/// <summary>
/// Wraps an HTTP tile source and re-renders every tile once through a colour matrix, so OpenStreetMap tiles arrive
/// muted toward the app's limestone ground and the condition pins carry the colour on the map.
/// Mapsui caches the tinted bytes like any other tile, so nothing runs per frame.
/// Adapted from MorningCard.Uno (Controls/MutedTileSource.cs), validated on Uno.Sdk 6.7.30 desktop and Android.
/// </summary>
internal sealed class MutedTileSource(IHttpTileSource inner, float[] colorMatrix) : IHttpTileSource
{
    private readonly SKColorFilter _filter = SKColorFilter.CreateColorMatrix(colorMatrix);

    /// <summary>Desaturates to luma, then scales the ground colour by it: land lands on the ground, ink sinks to grey.</summary>
    public static float[] GroundMatrix(SKColor ground, float floor, float gain)
    {
        float[] Row(float channel)
        {
            var c = channel / 255f;
            return [c * gain * 0.2126f, c * gain * 0.7152f, c * gain * 0.0722f, 0, c * floor];
        }

        return [.. Row(ground.Red), .. Row(ground.Green), .. Row(ground.Blue), 0, 0, 0, 1, 0];
    }

    /// <summary>Night variant: inverts luma first so streets read light on a dark ground.</summary>
    public static float[] NightMatrix(SKColor ground, float lift)
    {
        float[] Row(float channel)
        {
            var c = channel / 255f;
            return [-c * 0.2126f * 0.55f, -c * 0.7152f * 0.55f, -c * 0.0722f * 0.55f, 0, c * 0.55f + lift];
        }

        return [.. Row(ground.Red + 40), .. Row(ground.Green + 40), .. Row(ground.Blue + 40), 0, 0, 0, 1, 0];
    }

    public ITileSchema Schema => inner.Schema;

    public string Name => inner.Name;

    public Attribution Attribution => inner.Attribution;

    public async Task<byte[]?> GetTileAsync(HttpClient httpClient, TileInfo tileInfo, CancellationToken? cancellationToken = null)
    {
        var bytes = await inner.GetTileAsync(httpClient, tileInfo, cancellationToken);
        return bytes is null ? null : Tint(bytes);
    }

    private byte[] Tint(byte[] png)
    {
        using var source = SKBitmap.Decode(png);
        if (source is null)
        {
            return png;
        }

        using var surface = SKSurface.Create(new SKImageInfo(source.Width, source.Height));
        using var paint = new SKPaint { ColorFilter = _filter };
        surface.Canvas.DrawBitmap(source, 0, 0, paint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
