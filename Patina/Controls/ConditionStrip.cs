using Microsoft.UI.Xaml.Automation;
using Patina.Converters;
using Patina.Core.Model;

namespace Patina.Controls;

/// <summary>
/// The signature: four patches in a row, like the colour-calibration strip conservators photograph beside an
/// object. The number of filled patches is the grade, so the strip reads without colour; the template pairs it with
/// the numeral and word. States <c>Grade0</c>…<c>Grade4</c> live in the control's style (Styles/Patina.xaml).
/// </summary>
public sealed partial class ConditionStrip : Control
{
    public static readonly DependencyProperty GradeProperty = DependencyProperty.Register(
        nameof(Grade), typeof(ConditionGrade), typeof(ConditionStrip),
        new PropertyMetadata(ConditionGrade.Unsurveyed, (d, _) => ((ConditionStrip)d).Update(useTransitions: true)));

    public static readonly DependencyProperty ShowLabelProperty = DependencyProperty.Register(
        nameof(ShowLabel), typeof(bool), typeof(ConditionStrip), new PropertyMetadata(true));

    public static readonly DependencyProperty PatchWidthProperty = DependencyProperty.Register(
        nameof(PatchWidth), typeof(double), typeof(ConditionStrip), new PropertyMetadata(10d));

    public static readonly DependencyProperty PatchHeightProperty = DependencyProperty.Register(
        nameof(PatchHeight), typeof(double), typeof(ConditionStrip), new PropertyMetadata(14d));

    public ConditionStrip()
    {
        DefaultStyleKey = typeof(ConditionStrip);
    }

    public ConditionGrade Grade
    {
        get => (ConditionGrade)GetValue(GradeProperty);
        set => SetValue(GradeProperty, value);
    }

    /// <summary>Shows "3 Poor" beside the patches. Turn off only where the grade word is printed next to the strip.</summary>
    public bool ShowLabel
    {
        get => (bool)GetValue(ShowLabelProperty);
        set => SetValue(ShowLabelProperty, value);
    }

    public double PatchWidth
    {
        get => (double)GetValue(PatchWidthProperty);
        set => SetValue(PatchWidthProperty, value);
    }

    public double PatchHeight
    {
        get => (double)GetValue(PatchHeightProperty);
        set => SetValue(PatchHeightProperty, value);
    }

    public string GradeNumber => Grade == ConditionGrade.Unsurveyed ? "–" : ((int)Grade).ToString(System.Globalization.CultureInfo.InvariantCulture);

    public string GradeWord => Services.Text.Enum(Grade);

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        Update(useTransitions: false);
    }

    private void Update(bool useTransitions)
    {
        AutomationProperties.SetName(this, GradeTextConverter.GradeText(Grade));
        if (GetTemplateChild("Number") is TextBlock number)
        {
            number.Text = GradeNumber;
        }

        if (GetTemplateChild("Word") is TextBlock word)
        {
            word.Text = GradeWord;
        }

        // Reduced motion: jump to the final state without the cross-fade.
        VisualStateManager.GoToState(this, $"Grade{(int)Grade}", useTransitions && Services.Motion.Enabled);
    }
}
