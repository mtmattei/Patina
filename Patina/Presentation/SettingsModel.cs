using System.Globalization;
using System.Reflection;
using Patina.Core.Rules;
using Patina.Core.Storage;
using Patina.Services;
using Uno.Extensions.Toolkit;

namespace Patina.Presentation;

/// <summary>Surveyor name, appearance, language, and moving the collection between devices.</summary>
public partial record SettingsModel(
    IAppSettings Settings,
    IThemeService Theme,
    ILocalizationService Localization,
    PatinaStore Store,
    IDataTransferService Transfer,
    INavigator Navigator)
{
    private static readonly string[] Cultures = ["en", "fr"];

    public IImmutableList<string> ThemeOptions { get; } =
        ImmutableList.Create(Text.Get("Settings_ThemeSystem"), Text.Get("Settings_ThemeLight"), Text.Get("Settings_ThemeDark"));

    public IImmutableList<string> LanguageOptions { get; } = ImmutableList.Create("English", "Français");

    public string Version { get; } =
        typeof(SettingsModel).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "1.0.0";

    public string DataFolder => LocalFolderDocumentFile.Folder;

    public IState<string> Surveyor => State.Value(this, () => Settings.Surveyor).ForEach(SaveSurveyor);

    public IState<bool> ReduceMotion => State.Value(this, () => Settings.ReduceMotion).ForEach(SaveReduceMotion);

    public IState<int> ThemeIndex => State.Value(this, () => Theme.Theme switch
    {
        AppTheme.Light => 1,
        AppTheme.Dark => 2,
        _ => 0,
    }).ForEach(ApplyTheme);

    public IState<int> LanguageIndex => State.Value(this, () =>
        Localization.CurrentCulture.TwoLetterISOLanguageName == "fr" ? 1 : 0).ForEach(ApplyLanguage);

    public IState<string> Message => State.Value(this, () => string.Empty);

    public async ValueTask Export(CancellationToken ct)
    {
        var outcome = await Transfer.ExportAsync(await Store.GetAsync(ct), ct);
        if (outcome != TransferOutcome.Cancelled)
        {
            await Message.UpdateAsync(_ => Text.Get(outcome == TransferOutcome.Done ? "Settings_Exported" : "Settings_ExportFailed"), ct);
        }
    }

    public async ValueTask Import(CancellationToken ct)
    {
        var picked = await Transfer.PickImportAsync(ct);
        if (picked is not { } result)
        {
            return;
        }

        if (!result.IsOk)
        {
            await Message.UpdateAsync(_ => Text.Problem(result.Problem), ct);
            return;
        }

        var preview = result.Value!;
        var confirmed = await ConfirmAsync(
            Text.Get("Settings_ImportTitle"),
            Text.Format("Settings_ImportBody", preview.FileName, preview.Document.Artworks.Count, preview.Document.Surveys.Count),
            Text.Get("Settings_ImportConfirm"),
            ct);
        if (!confirmed)
        {
            return;
        }

        var problem = await Store.ReplaceAsync(preview.Document, ct);
        if (problem == Problem.None)
        {
            await Transfer.RestorePhotosAsync(preview, ct);
        }

        await Message.UpdateAsync(_ => problem == Problem.None ? Text.Get("Settings_Imported") : Text.Problem(problem), ct);
    }

    public async ValueTask ResetSample(CancellationToken ct)
    {
        if (!await ConfirmAsync(Text.Get("Settings_ResetTitle"), Text.Get("Settings_ResetBody"), Text.Get("Settings_ResetConfirm"), ct))
        {
            return;
        }

        var problem = await Store.ResetToSampleAsync(ct);
        await Message.UpdateAsync(_ => problem == Problem.None ? Text.Get("Settings_ResetDone") : Text.Problem(problem), ct);
    }

    private async ValueTask<bool> ConfirmAsync(string title, string content, string confirm, CancellationToken ct)
    {
        // The choice comes back through DialogAction.Action, not the awaited result (runtime gotchas).
        var confirmed = false;
        await Navigator.ShowMessageDialogAsync(
            this,
            title: title,
            content: content,
            buttons:
            [
                new DialogAction(confirm, () => confirmed = true),
                new DialogAction(Text.Get("Common_Cancel")),
            ],
            cancellation: ct);
        return confirmed;
    }

    private ValueTask SaveSurveyor(string? name, CancellationToken ct)
    {
        Settings.Surveyor = name ?? string.Empty;
        return ValueTask.CompletedTask;
    }

    private ValueTask SaveReduceMotion(bool value, CancellationToken ct)
    {
        Settings.ReduceMotion = value;
        return ValueTask.CompletedTask;
    }

    private async ValueTask ApplyTheme(int index, CancellationToken ct) =>
        await Theme.SetThemeAsync(index switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.System,
        });

    private async ValueTask ApplyLanguage(int index, CancellationToken ct)
    {
        var culture = Cultures[Math.Clamp(index, 0, Cultures.Length - 1)];
        if (Localization.CurrentCulture.TwoLetterISOLanguageName == culture)
        {
            return;
        }

        await Localization.SetCurrentCultureAsync(new CultureInfo(culture));
        await Message.UpdateAsync(_ => Text.Get("Settings_LanguageRestart"), ct);
    }
}
