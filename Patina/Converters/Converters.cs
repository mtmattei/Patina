using Microsoft.UI.Xaml.Data;
using Patina.Core.Model;
using Patina.Core.Queries;
using Patina.Services;

namespace Patina.Converters;

/// <summary>Any enum value → its localized label (<c>{EnumType}_{Value}</c>).</summary>
public sealed class EnumTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is Enum e ? Text.Get($"{e.GetType().Name}_{e}") : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>DateOnly / DateTimeOffset → "3 Oct 2026" in the current culture; null → an em dash.</summary>
public sealed class DateTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => value switch
    {
        DateOnly d => Text.Date(d),
        DateTimeOffset dto => Text.Date(DateOnly.FromDateTime(dto.LocalDateTime)),
        _ => "—",
    };

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>
/// Visible when the value is "present": true, non-null, non-empty text, a non-zero number, a non-empty list.
/// ConverterParameter "invert" flips it.
/// </summary>
public sealed class VisibleWhenConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var present = value switch
        {
            null => false,
            bool b => b,
            string s => !string.IsNullOrWhiteSpace(s),
            int i => i != 0,
            System.Collections.ICollection c => c.Count > 0,
            _ => true,
        };

        if (parameter as string == "invert")
        {
            present = !present;
        }

        return present ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>Artwork summary → its due line: "Overdue since …", "Due …", "Next survey …", "Never surveyed".</summary>
public sealed class DueTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is ArtworkSummary s ? DueText(s.Due, s.NextDue) : string.Empty;

    public static string DueText(DueState due, DateOnly? next) => due switch
    {
        DueState.NeverSurveyed => Text.Get("Due_Never"),
        DueState.Overdue => Text.Format("Due_Overdue", Text.Date(next)),
        DueState.DueSoon => Text.Format("Due_Soon", Text.Date(next)),
        _ => Text.Format("Due_Current", Text.Date(next)),
    };

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>Grade → "3 · Poor" for screen readers and compact labels.</summary>
public sealed class GradeTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is ConditionGrade g ? GradeText(g) : string.Empty;

    public static string GradeText(ConditionGrade g) =>
        g == ConditionGrade.Unsurveyed ? Text.Enum(g) : Text.Format("Grade_Accessible", (int)g, Text.Enum(g));

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>Treatment status → the label of the action that moves it forward ("Schedule", "Start work", "Mark done").</summary>
public sealed class NextActionTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is TreatmentStatus s ? Text.Get($"NextAction_{s}") : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>Treatment log entry → its line, localized for entries the app recorded.</summary>
public sealed class LogTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not TreatmentLogEntry e)
        {
            return string.Empty;
        }

        var headline = e.Kind switch
        {
            LogKind.Proposed => Text.Get("Log_Proposed"),
            LogKind.StatusChanged when e.Status is { } s => Text.Format("Log_StatusChanged", Text.Enum(s)),
            LogKind.StatusReturned when e.Status is { } s => Text.Format("Log_StatusReturned", Text.Enum(s)),
            _ => string.Empty,
        };

        return string.IsNullOrWhiteSpace(e.Text) ? headline
            : string.IsNullOrEmpty(headline) ? e.Text
            : $"{headline} {e.Text}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>Numeric value → formatted with a resource string ("{0} findings"); parameter is the resource key.</summary>
public sealed class CountTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var count = value switch
        {
            int i => i,
            System.Collections.ICollection c => c.Count,
            _ => 0,
        };
        var key = parameter as string ?? "Count";
        return Text.Format(count == 1 ? key + "_One" : key + "_Many", count);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>DateOnly? ↔ DateTimeOffset? for CalendarDatePicker.Date.</summary>
public sealed class DateOnlyOffsetConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language) =>
        value is DateOnly d ? new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue)) : null;

    public object? ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is DateTimeOffset dto ? DateOnly.FromDateTime(dto.Date) : null;
}

/// <summary>Artwork (or summary) → "Joseph Taillefer, 1931".</summary>
public sealed class BylineTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => value switch
    {
        Artwork a => $"{a.Artist}, {a.Year}",
        ArtworkSummary s => $"{s.Artist}, {s.Year}",
        _ => string.Empty,
    };

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>Artwork → "Bronze · Square Phillips · Ville-Marie".</summary>
public sealed class PlaceTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is Artwork a ? $"{Text.Enum(a.Material)} · {a.Site} · {a.District}" : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>Dossier → why the next survey falls when it does ("Bronze works are surveyed every 12 months…").</summary>
public sealed class IntervalTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not ArtworkDossier d)
        {
            return string.Empty;
        }

        var material = d.Artwork.Material;
        var baseMonths = Core.Rules.ConditionRules.BaseIntervalMonths(material);
        var months = Core.Rules.ConditionRules.IntervalMonths(material, d.Summary.Grade);
        return months == baseMonths
            ? Text.Format("Artwork_Interval", Text.Enum(material), baseMonths)
            : Text.Format("Artwork_IntervalAdjusted", Text.Enum(material), baseMonths, Text.Enum(d.Summary.Grade), months);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>Dossier → "Start survey" or "Resume draft survey".</summary>
public sealed class SurveyActionTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is ArtworkDossier { Draft: not null } ? Text.Get("Artwork_ResumeSurvey") : Text.Get("Artwork_StartSurvey");

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>Finding → "Corrosion · North face · Serious".</summary>
public sealed class FindingTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is Finding f ? $"{Text.Enum(f.Type)} · {f.Zone} · {Text.Enum(f.Severity)}" : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

/// <summary>Survey → "C. Lavoie · Overcast".</summary>
public sealed class SurveyMetaTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is Survey s ? $"{s.Surveyor} · {Text.Enum(s.Weather)}" : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}
