using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using Mapsui.UI.WinUI;
using Patina.Core.Model;
using Patina.Core.Queries;
using SkiaSharp;

namespace Patina.Controls;

/// <summary>
/// The collection on a map: one pin per artwork, filled with its grade colour, the selected pin larger and ringed.
/// Tapping a pin sets <see cref="SelectedId"/>; tapping open map clears it.
///
/// Imperative by necessity: a map is a viewport, a tile cache and a hit test with no XAML surface. Everything the
/// page needs crosses through two bindable properties, so the page itself keeps no code-behind.
/// </summary>
public sealed partial class ArtworkMap : UserControl
{
    /// <summary>OpenStreetMap's tile policy requires an identifying User-Agent and blocks generic ones (runtime gotchas).</summary>
    private const string TileUserAgent = "Patina/1.0 (public art conservation field app; Uno Platform)";

    private const double TapTolerancePixels = 24;

    /// <summary>About 30 m per pixel: the whole sample collection fits a laptop window.</summary>
    private const double CityResolution = 30;

    private static readonly (double Lon, double Lat) Centre = (-73.585, 45.515);

    /// <summary>
    /// Typed <c>object</c> on purpose: MVUX projects an <c>IListFeed</c> as its own observable list type, so a
    /// strongly typed property would drop the binding. Any enumerable of <see cref="ArtworkSummary"/> works, and
    /// collection changes (search, filters, a submitted survey) redraw the pins.
    /// </summary>
    public static readonly DependencyProperty ArtworksProperty = DependencyProperty.Register(
        nameof(Artworks), typeof(object), typeof(ArtworkMap),
        new PropertyMetadata(null, (d, e) => ((ArtworkMap)d).OnArtworksChanged(e.OldValue, e.NewValue)));

    public static readonly DependencyProperty SelectedIdProperty = DependencyProperty.Register(
        nameof(SelectedId), typeof(string), typeof(ArtworkMap),
        new PropertyMetadata(null, (d, _) => ((ArtworkMap)d).Redraw()));

    private readonly Mapsui.UI.WinUI.MapControl _map = new();
    private MemoryLayer? _pins;
    private bool _initialized;
    private ElementTheme _tileTheme;

    public ArtworkMap()
    {
        Content = _map;
        Loaded += (_, _) => Initialize();
        ActualThemeChanged += (_, _) => ApplyTiles();
    }

    public object? Artworks
    {
        get => GetValue(ArtworksProperty);
        set => SetValue(ArtworksProperty, value);
    }

    private IEnumerable<ArtworkSummary> Items =>
        Artworks is System.Collections.IEnumerable items ? items.OfType<ArtworkSummary>() : [];

    private void OnArtworksChanged(object? oldValue, object? newValue)
    {
        if (oldValue is System.Collections.Specialized.INotifyCollectionChanged oldList)
        {
            oldList.CollectionChanged -= OnItemsChanged;
        }

        if (newValue is System.Collections.Specialized.INotifyCollectionChanged newList)
        {
            newList.CollectionChanged += OnItemsChanged;
        }

        Redraw();
    }

    private void OnItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => Redraw();

    public string? SelectedId
    {
        get => (string?)GetValue(SelectedIdProperty);
        set => SetValue(SelectedIdProperty, value);
    }

    private void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        var map = new Mapsui.Map { CRS = "EPSG:3857" };
        _pins = new MemoryLayer { Name = "artworks", Features = [] };
        map.Layers.Add(_pins);

        // Assign the map first: the navigator only answers once the control has a size.
        _map.Map = map;
        _map.Info += OnInfo;
        ApplyTiles();

        var (x, y) = SphericalMercator.FromLonLat(Centre.Lon, Centre.Lat);
        map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), CityResolution);
        Redraw();
    }

    private void ApplyTiles()
    {
        if (_map.Map is not { } map || (_tileTheme == ActualTheme && map.Layers.Any(l => l is TileLayer)))
        {
            return;
        }

        _tileTheme = ActualTheme;
        foreach (var old in map.Layers.OfType<TileLayer>().ToList())
        {
            map.Layers.Remove(old);
        }

        var osm = OpenStreetMap.CreateTileLayer(TileUserAgent);
        var matrix = ActualTheme == ElementTheme.Dark
            ? MutedTileSource.NightMatrix(new SKColor(0x15, 0x16, 0x14), 0.06f)
            : MutedTileSource.GroundMatrix(new SKColor(0xEE, 0xF0, 0xEC), 0.36f, 0.66f);

        if (osm.TileSource is BruTile.IHttpTileSource http)
        {
            map.Layers.Insert(0, new TileLayer(new MutedTileSource(http, matrix)) { Name = "tiles" });
        }
        else
        {
            map.Layers.Insert(0, osm);
        }

        _map.Refresh();
    }

    private void Redraw()
    {
        if (_pins is null)
        {
            return;
        }

        var selected = SelectedId;
        _pins.Features = Items
            .OrderBy(a => a.Id == selected)
            .Select(a => Pin(a, a.Id == selected))
            .ToList();
        _map.Refresh();
    }

    private static PointFeature Pin(ArtworkSummary artwork, bool selected)
    {
        var (x, y) = SphericalMercator.FromLonLat(artwork.Longitude, artwork.Latitude);
        var feature = new PointFeature(x, y);
        feature["id"] = artwork.Id;
        feature.Styles.Add(new SymbolStyle
        {
            SymbolType = SymbolType.Ellipse,
            SymbolScale = selected ? 1.25 : 0.75,
            Fill = new Mapsui.Styles.Brush(GradeColor(artwork.Grade)),
            Outline = new Pen(selected ? Mapsui.Styles.Color.FromArgb(255, 0x1C, 0x23, 0x21) : Mapsui.Styles.Color.White, selected ? 3 : 2),
        });
        return feature;
    }

    /// <summary>Mirrors Styles/Condition.xaml (Grade*InvariantColor); unsurveyed pins are hollow stone.</summary>
    private static Mapsui.Styles.Color GradeColor(ConditionGrade grade) => grade switch
    {
        ConditionGrade.Good => Mapsui.Styles.Color.FromArgb(255, 0x3E, 0x87, 0x62),
        ConditionGrade.Fair => Mapsui.Styles.Color.FromArgb(255, 0x9C, 0x7C, 0x22),
        ConditionGrade.Poor => Mapsui.Styles.Color.FromArgb(255, 0xC0, 0x63, 0x2A),
        ConditionGrade.Critical => Mapsui.Styles.Color.FromArgb(255, 0xD0, 0x44, 0x36),
        _ => Mapsui.Styles.Color.FromArgb(255, 0x8A, 0x94, 0x90),
    };

    private void OnInfo(object? sender, MapInfoEventArgs e)
    {
        if (_map.Map?.Navigator.Viewport is not { } viewport)
        {
            return;
        }

        // Nearest pin within a fixed pixel tolerance, so the tap target is the same size at every zoom.
        var world = e.WorldPosition;
        ArtworkSummary? hit = null;
        var best = double.MaxValue;
        foreach (var artwork in Items)
        {
            var (x, y) = SphericalMercator.FromLonLat(artwork.Longitude, artwork.Latitude);
            var pixels = Math.Sqrt(((x - world.X) * (x - world.X)) + ((y - world.Y) * (y - world.Y))) / viewport.Resolution;
            if (pixels < best)
            {
                best = pixels;
                hit = artwork;
            }
        }

        SelectedId = best <= TapTolerancePixels ? hit?.Id : null;
        e.Handled = true;
    }
}
