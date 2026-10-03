using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using Patina.Services;

namespace Patina.Controls;

/// <summary>
/// <c>controls:Entrance.IsEnabled="True"</c> on a page's content: on Loaded it rises 6 px and fades in over 280 ms on
/// the house EaseSmooth curve (xaml-design-polish). Composition runs it off the UI thread. Reduced motion: nothing moves.
/// </summary>
public static class Entrance
{
    private const float Rise = 6f;
    private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(280);

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(Entrance), new PropertyMetadata(false, OnChanged));

    public static bool GetIsEnabled(UIElement element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(UIElement element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element && e.NewValue is true)
        {
            element.Loaded += (_, _) => Play(element);
        }
    }

    private static void Play(UIElement element)
    {
        if (!Motion.Enabled)
        {
            return;
        }

        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;
        var ease = compositor.CreateCubicBezierEasingFunction(new Vector2(0.22f, 1f), new Vector2(0.36f, 1f));

        // Translation needs enabling before it can be animated (measured on Uno Skia, xaml-design-polish rule 5).
        ElementCompositionPreview.SetIsTranslationEnabled(element, true);

        var opacity = compositor.CreateScalarKeyFrameAnimation();
        opacity.InsertKeyFrame(0f, 0f);
        opacity.InsertKeyFrame(1f, 1f, ease);
        opacity.Duration = Duration;

        var rise = compositor.CreateVector3KeyFrameAnimation();
        rise.InsertKeyFrame(0f, new Vector3(0, Rise, 0));
        rise.InsertKeyFrame(1f, Vector3.Zero, ease);
        rise.Duration = Duration;

        visual.StartAnimation("Opacity", opacity);
        visual.StartAnimation("Translation", rise);
    }
}
