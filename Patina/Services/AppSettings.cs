using Windows.Storage;
using Windows.UI.ViewManagement;

namespace Patina.Services;

public interface IAppSettings
{
    /// <summary>Name stamped on surveys and treatment log entries.</summary>
    string Surveyor { get; set; }

    /// <summary>
    /// In-app reduced motion. <see cref="UISettings.AnimationsEnabled"/> is hardcoded true on Skia desktop
    /// (runtime gotchas), so the system setting alone cannot honour the preference there.
    /// </summary>
    bool ReduceMotion { get; set; }
}

/// <summary>Device preferences in <see cref="ApplicationData.LocalSettings"/>. Theme and language are owned by Uno's services.</summary>
public sealed class AppSettings : IAppSettings
{
    private const string SurveyorKey = "surveyor";
    private const string ReduceMotionKey = "reduceMotion";

    public string Surveyor
    {
        get => ApplicationData.Current.LocalSettings.Values[SurveyorKey] as string ?? string.Empty;
        set => ApplicationData.Current.LocalSettings.Values[SurveyorKey] = value.Trim();
    }

    public bool ReduceMotion
    {
        get => ApplicationData.Current.LocalSettings.Values[ReduceMotionKey] is true;
        set
        {
            ApplicationData.Current.LocalSettings.Values[ReduceMotionKey] = value;
            Motion.ReduceMotion = value;
        }
    }
}

/// <summary>The one gate every decorative animation checks.</summary>
public static class Motion
{
    private static readonly UISettings System = new();

    public static bool ReduceMotion { get; set; }

    public static bool Enabled => !ReduceMotion && System.AnimationsEnabled;
}
