using System.Globalization;
using Windows.ApplicationModel.Resources;
using Patina.Core.Model;
using Patina.Core.Rules;

namespace Patina.Services;

/// <summary>
/// Localized display text for domain values, read from the same Strings/*/Resources.resw as x:Uid.
/// Uses <see cref="ResourceLoader"/> directly so converters and models can run before the host's localizer is
/// resolved (models are constructed during navigation). Keys follow <c>{EnumType}_{Value}</c> for enums.
/// </summary>
public static class Text
{
    private static ResourceLoader? _loader;

    private static ResourceLoader Loader => _loader ??= ResourceLoader.GetForViewIndependentUse();

    public static string Get(string key)
    {
        var value = Loader.GetString(key);
        return string.IsNullOrEmpty(value) ? key : value;
    }

    public static string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), args);

    public static string Enum<T>(T value) where T : struct, System.Enum => Get($"{typeof(T).Name}_{value}");

    public static string Date(DateOnly? date) =>
        date is { } d ? d.ToString("d MMM yyyy", CultureInfo.CurrentCulture) : "—";

    public static string Problem(Problem problem) => Get($"Problem_{problem}");

    /// <summary>The treatment title proposed from a finding, in the surveyor's language.</summary>
    public static string TreatmentTitle(Finding finding)
    {
        var verb = Get($"TreatmentVerb_{finding.Type}");
        return string.IsNullOrWhiteSpace(finding.Zone) ? verb : verb + Get("TitleSeparator") + finding.Zone.Trim();
    }
}
