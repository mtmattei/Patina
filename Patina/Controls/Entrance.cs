using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Patina.Services;

namespace Patina.Controls;

/// <summary>
/// <c>controls:Entrance.IsEnabled="True"</c> on a page's content: on Loaded it rises 6 px and fades in over 280 ms on
/// the house EaseSmooth curve (xaml-design-polish). Storyboard-based so it runs on every head:
/// <c>ElementCompositionPreview.SetIsTranslationEnabled</c> is Uno0001 on WebAssembly. Reduced motion: nothing moves.
/// </summary>
public static class Entrance
{
    private const double Rise = 6;
    private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(280);
    private static readonly KeySpline EaseSmooth = new() { ControlPoint1 = new(0.22, 1), ControlPoint2 = new(0.36, 1) };

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

    private static void Play(FrameworkElement element)
    {
        if (!Motion.Enabled)
        {
            return;
        }

        if (element.RenderTransform is not TranslateTransform translate)
        {
            translate = new TranslateTransform();
            element.RenderTransform = translate;
        }

        var storyboard = new Storyboard();
        storyboard.Children.Add(Animate(element, "Opacity", 0, 1));
        storyboard.Children.Add(Animate(translate, "Y", Rise, 0));
        storyboard.Begin();
    }

    private static DoubleAnimationUsingKeyFrames Animate(DependencyObject target, string property, double from, double to)
    {
        var animation = new DoubleAnimationUsingKeyFrames();
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = from });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(Duration), Value = to, KeySpline = EaseSmooth });
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);
        return animation;
    }
}
