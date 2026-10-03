using Patina.Core;
using Patina.Core.Model;
using Patina.Core.Rules;
using Patina.Core.Storage;
using Patina.Services;

namespace Patina.Presentation;

public sealed record WeatherOption(Weather Value, string Label);

public sealed record FindingTypeOption(FindingType Value, string Label);

/// <summary>The survey-level fields, edited two-way through the generated bindable proxy.</summary>
public partial record SurveyForm(string Surveyor, int WeatherIndex, string Notes);

/// <summary>
/// The finding being composed. Type and severity are indexes into their option lists: index bindings survive the
/// generated proxy where a record SelectedItem does not compare equal.
/// </summary>
public partial record FindingForm(int TypeIndex, string Zone, int SeverityIndex, string Note)
{
    public static FindingForm Empty { get; } = new(-1, string.Empty, 0, string.Empty);
}

public sealed record SurveyHeader(Artwork Artwork, bool IsResumed);

/// <summary>
/// One condition survey. Edits live in states; every change that matters (findings, photos) saves the draft at once,
/// because a field survey is interrupted by weather, traffic and battery. Submit grades it and proposes treatments.
/// </summary>
public partial record SurveyModel(
    SurveyStart Start,
    PatinaStore Store,
    IPhotoService Photos,
    IAppSettings Settings,
    INavigator Navigator)
{
    private readonly string _newSurveyId = Ids.New();

    public IImmutableList<WeatherOption> WeatherOptions { get; } =
        Enum.GetValues<Weather>().Select(w => new WeatherOption(w, Text.Enum(w))).ToImmutableList();

    public IImmutableList<string> SeverityOptions { get; } =
        Enum.GetValues<Severity>().Select(s => $"{(int)s}  {Text.Enum(s)}").ToImmutableList();

    public bool CanCapture => Photos.CanCapture;

    public IFeed<SurveyHeader> Header => Feed.Async(async ct =>
    {
        var doc = await Store.GetAsync(ct);
        var artwork = doc.Artworks.First(a => a.Id == Start.ArtworkId);
        return new SurveyHeader(artwork, SurveyOps.DraftFor(doc, Start.ArtworkId) is not null);
    });

    public IListFeed<FindingTypeOption> FindingTypes => Feed.Async(async ct =>
    {
        var doc = await Store.GetAsync(ct);
        var material = doc.Artworks.First(a => a.Id == Start.ArtworkId).Material;
        return ConditionRules.FindingTypesFor(material)
            .Select(t => new FindingTypeOption(t, Text.Enum(t)))
            .ToImmutableList() as IImmutableList<FindingTypeOption>;
    }).AsListFeed();

    public IState<SurveyForm> Form => State.Async(this, async ct =>
    {
        var draft = await DraftAsync(ct);
        var surveyor = draft?.Surveyor is { Length: > 0 } s ? s : Settings.Surveyor;
        var weather = Math.Max(0, WeatherOptions.ToList().FindIndex(o => o.Value == (draft?.Weather ?? Weather.Dry)));
        return new SurveyForm(surveyor, weather, draft?.Notes ?? string.Empty);
    });

    public IListState<Finding> Findings => ListState.Async(this, async ct =>
        (IImmutableList<Finding>)((await DraftAsync(ct))?.Findings ?? []));

    public IListState<PhotoRef> SurveyPhotos => ListState.Async(this, async ct =>
    {
        var doc = await Store.GetAsync(ct);
        var ids = (await DraftAsync(ct))?.PhotoIds ?? [];
        return (IImmutableList<PhotoRef>)doc.Photos.Where(p => ids.Contains(p.Id)).ToImmutableList();
    });

    public IState<FindingForm> NewFinding => State.Value(this, () => FindingForm.Empty);

    public IListState<PhotoRef> FindingPhotos => ListState<PhotoRef>.Empty(this);

    /// <summary>The grade the survey will submit with, recomputed whenever findings change.</summary>
    public IState<ConditionGrade> LiveGrade => State.Async(this, async ct =>
        ConditionRules.Grade((await DraftAsync(ct))?.Findings ?? []));

    /// <summary>The last validation or save message; empty when there is nothing to say.</summary>
    public IState<string> Message => State.Value(this, () => string.Empty);

    public async ValueTask AddFinding(CancellationToken ct)
    {
        var form = await NewFinding ?? FindingForm.Empty;
        var types = await FindingTypes ?? ImmutableList<FindingTypeOption>.Empty;
        if (form.TypeIndex < 0 || form.TypeIndex >= types.Count)
        {
            await Message.UpdateAsync(_ => Text.Get("Survey_FindingTypeRequired"), ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(form.Zone))
        {
            await Message.UpdateAsync(_ => Text.Problem(Problem.FindingZoneRequired), ct);
            return;
        }

        var photos = await FindingPhotos ?? [];
        var finding = new Finding(
            Ids.New(),
            types[form.TypeIndex].Value,
            form.Zone.Trim(),
            (Severity)(Math.Clamp(form.SeverityIndex, 0, 3) + 1),
            form.Note.Trim(),
            photos.Select(p => p.Id).ToImmutableList());

        await Findings.AddAsync(finding, ct);
        await NewFinding.UpdateAsync(_ => FindingForm.Empty, ct);
        await FindingPhotos.UpdateAsync(_ => ImmutableList<PhotoRef>.Empty, ct);
        await AfterFindingsChanged(ct);
    }

    public async ValueTask RemoveFinding(Finding finding, CancellationToken ct)
    {
        await Findings.RemoveAllAsync(f => f.Id == finding.Id, ct);
        await AfterFindingsChanged(ct);
    }

    public async ValueTask AddSurveyPhoto(CancellationToken ct) => await AddPhoto(SurveyPhotos, Photos.PickAsync, saveAfter: true, ct);

    public async ValueTask CaptureSurveyPhoto(CancellationToken ct) => await AddPhoto(SurveyPhotos, Photos.CaptureAsync, saveAfter: true, ct);

    public async ValueTask AddFindingPhoto(CancellationToken ct) => await AddPhoto(FindingPhotos, Photos.PickAsync, saveAfter: false, ct);

    public async ValueTask CaptureFindingPhoto(CancellationToken ct) => await AddPhoto(FindingPhotos, Photos.CaptureAsync, saveAfter: false, ct);

    public async ValueTask RemoveSurveyPhoto(PhotoRef photo, CancellationToken ct)
    {
        await SurveyPhotos.RemoveAllAsync(p => p.Id == photo.Id, ct);
        await SaveAsync(ct);
    }

    public async ValueTask RemoveFindingPhoto(PhotoRef photo, CancellationToken ct) =>
        await FindingPhotos.RemoveAllAsync(p => p.Id == photo.Id, ct);

    public async ValueTask SaveDraft(CancellationToken ct)
    {
        var problem = await SaveAsync(ct);
        await Message.UpdateAsync(_ => problem == Problem.None ? Text.Get("Survey_DraftSaved") : Text.Problem(problem), ct);
    }

    public async ValueTask Submit(CancellationToken ct)
    {
        var survey = await BuildAsync(ct);
        var photos = await AllPhotosAsync(ct);
        var problem = await Store.UpdateAsync(
            doc => SurveyOps.Submit(WithPhotos(doc, photos), survey, Store.Clock.Now, Text.TreatmentTitle), ct);

        if (problem != Problem.None)
        {
            await Message.UpdateAsync(_ => Text.Problem(problem), ct);
            return;
        }

        Settings.Surveyor = survey.Surveyor;
        await Navigator.NavigateBackAsync(this, cancellation: ct);
    }

    public async ValueTask Discard(CancellationToken ct)
    {
        var confirmed = false;
        await Navigator.ShowMessageDialogAsync(
            this,
            title: Text.Get("Survey_DiscardTitle"),
            content: Text.Get("Survey_DiscardBody"),
            buttons:
            [
                new DialogAction(Text.Get("Survey_DiscardConfirm"), () => confirmed = true),
                new DialogAction(Text.Get("Common_Cancel")),
            ],
            cancellation: ct);

        if (!confirmed)
        {
            return;
        }

        await Store.UpdateAsync(doc => SurveyOps.DiscardDraft(doc, SurveyId), ct);
        await Navigator.NavigateBackAsync(this, cancellation: ct);
    }

    /// <summary>Back from the navigation bar: keep whatever was entered as a draft, then leave.</summary>
    public async ValueTask Close(CancellationToken ct)
    {
        if (await HasContentAsync(ct))
        {
            await SaveAsync(ct);
        }

        await Navigator.NavigateBackAsync(this, cancellation: ct);
    }

    private string SurveyId => _draftId ?? _newSurveyId;

    private string? _draftId;

    private DateOnly? _draftDate;

    private async Task<Survey?> DraftAsync(CancellationToken ct)
    {
        var draft = SurveyOps.DraftFor(await Store.GetAsync(ct), Start.ArtworkId);
        _draftId ??= draft?.Id;
        _draftDate ??= draft?.Date;
        return draft;
    }

    private async ValueTask AddPhoto(IListState<PhotoRef> target, Func<CancellationToken, Task<PhotoRef?>> source, bool saveAfter, CancellationToken ct)
    {
        PhotoRef? photo;
        try
        {
            photo = await source(ct);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await Message.UpdateAsync(_ => Text.Get("Survey_PhotoFailed"), ct);
            return;
        }

        if (photo is null)
        {
            return;
        }

        await target.AddAsync(photo, ct);
        if (saveAfter)
        {
            await SaveAsync(ct);
        }
    }

    private async ValueTask AfterFindingsChanged(CancellationToken ct)
    {
        var findings = await Findings ?? [];
        await LiveGrade.UpdateAsync(_ => ConditionRules.Grade(findings), ct);
        await SaveAsync(ct);
        await Message.UpdateAsync(_ => string.Empty, ct);
    }

    private async Task<Survey> BuildAsync(CancellationToken ct)
    {
        await DraftAsync(ct);
        var form = await Form;
        var findings = await Findings ?? [];
        var photos = await SurveyPhotos ?? [];
        return new Survey(
            SurveyId,
            Start.ArtworkId,
            SurveyStatus.Draft,
            _draftDate ?? Store.Clock.Today,
            form?.Surveyor.Trim() ?? string.Empty,
            WeatherOptions[Math.Clamp(form?.WeatherIndex ?? 0, 0, WeatherOptions.Count - 1)].Value,
            form?.Notes.Trim() ?? string.Empty,
            findings.ToImmutableList(),
            photos.Select(p => p.Id).ToImmutableList(),
            ConditionRules.Grade(findings));
    }

    private async Task<bool> HasContentAsync(CancellationToken ct)
    {
        var survey = await BuildAsync(ct);
        return _draftId is not null || survey.Findings.Count > 0 || survey.PhotoIds.Count > 0 || survey.Notes.Length > 0;
    }

    private async Task<ImmutableList<PhotoRef>> AllPhotosAsync(CancellationToken ct) =>
        [.. await SurveyPhotos ?? [], .. await FindingPhotos ?? []];

    private static PatinaDocument WithPhotos(PatinaDocument doc, IEnumerable<PhotoRef> photos)
    {
        var known = doc.Photos.Select(p => p.Id).ToHashSet();
        var added = photos.Where(p => known.Add(p.Id)).ToList();
        return added.Count == 0 ? doc : doc with { Photos = doc.Photos.AddRange(added) };
    }

    private async Task<Problem> SaveAsync(CancellationToken ct)
    {
        var survey = await BuildAsync(ct);
        var photos = await AllPhotosAsync(ct);
        var problem = await Store.UpdateAsync(doc => SurveyOps.SaveDraft(WithPhotos(doc, photos), survey), ct);
        if (problem == Problem.None)
        {
            _draftId = survey.Id;
            _draftDate = survey.Date;
        }

        return problem;
    }
}
