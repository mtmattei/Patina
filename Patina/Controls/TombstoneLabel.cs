namespace Patina.Controls;

/// <summary>
/// The museum object label: accession number, title, maker and date, material and site.
/// Heads the artwork and survey pages. Layout and type live in the control's style (Styles/Patina.xaml).
/// </summary>
public sealed partial class TombstoneLabel : Control
{
    public static readonly DependencyProperty AccessionProperty = Register(nameof(Accession));
    public static readonly DependencyProperty TitleProperty = Register(nameof(Title));
    public static readonly DependencyProperty BylineProperty = Register(nameof(Byline));
    public static readonly DependencyProperty PlaceProperty = Register(nameof(Place));

    public TombstoneLabel()
    {
        DefaultStyleKey = typeof(TombstoneLabel);
    }

    public string Accession { get => (string)GetValue(AccessionProperty); set => SetValue(AccessionProperty, value); }

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    public string Byline { get => (string)GetValue(BylineProperty); set => SetValue(BylineProperty, value); }

    public string Place { get => (string)GetValue(PlaceProperty); set => SetValue(PlaceProperty, value); }

    private static DependencyProperty Register(string name) =>
        DependencyProperty.Register(name, typeof(string), typeof(TombstoneLabel), new PropertyMetadata(string.Empty));
}
