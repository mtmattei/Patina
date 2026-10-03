using Patina.Core.Model;
using Patina.Core.Queries;
using Patina.Core.Rules;
using Patina.Core.Storage;
using Patina.Services;

namespace Patina.Presentation;

/// <summary>One step of the Proposed → Scheduled → In progress → Done stepper.</summary>
public sealed record StepView(int Number, string Label, bool IsDone, bool IsCurrent)
{
    public bool IsUpcoming => !IsDone && !IsCurrent;
}

/// <summary>One treatment: where it came from, its schedule and assignee, its log, and moving it forward.</summary>
public partial record TreatmentModel(TreatmentSummary Summary, PatinaStore Store, IAppSettings Settings, INavigator Navigator)
{
    private IFeed<PatinaDocument> Document => Feed.AsyncEnumerable(Store.Watch);

    public IFeed<TreatmentDetail> Detail => Document.Select(doc => PatinaQueries.Treatment(doc, Summary.Id, Store.Clock.Today)!);

    public IListFeed<StepView> Steps => Detail.Select(d => Stepper(d.Treatment.Status)).AsListFeed();

    public IFeed<TreatmentStatus> Status => Detail.Select(d => d.Treatment.Status);

    public IFeed<bool> IsEditable => Detail.Select(d => d.Treatment.Status != TreatmentStatus.Done);

    public IFeed<bool> CanStepBack => Detail.Select(d => TreatmentWorkflow.Previous(d.Treatment.Status) is not null);

    public IState<DateTimeOffset?> ScheduledFor => State<DateTimeOffset?>.Async(this, async ct =>
    {
        var t = await TreatmentAsync(ct);
        return t?.ScheduledFor is { } d ? new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue)) : null;
    });

    public IState<string> Assignee => State.Async(this, async ct => (await TreatmentAsync(ct))?.Assignee ?? string.Empty);

    public IState<string> Note => State.Value(this, () => string.Empty);

    public IState<string> Message => State.Value(this, () => string.Empty);

    public async ValueTask Save(CancellationToken ct)
    {
        var (date, assignee, note) = await ReadFieldsAsync();
        var problem = await Store.UpdateAsync(doc => TreatmentWorkflow.Save(doc, Summary.Id, date, assignee, note, Author, Store.Clock.Now), ct);
        await AfterWrite(problem, Text.Get("Treatment_Saved"), ct);
    }

    public async ValueTask Advance(CancellationToken ct)
    {
        var (date, assignee, note) = await ReadFieldsAsync();
        var problem = await Store.UpdateAsync(doc => TreatmentWorkflow.Advance(doc, Summary.Id, date, assignee, note, Author, Store.Clock.Now), ct);
        await AfterWrite(problem, string.Empty, ct);
    }

    public async ValueTask StepBack(CancellationToken ct)
    {
        var problem = await Store.UpdateAsync(doc => TreatmentWorkflow.StepBack(doc, Summary.Id, Author, Store.Clock.Now), ct);
        await AfterWrite(problem, string.Empty, ct);
    }

    public async ValueTask OpenArtwork(TreatmentDetail detail, CancellationToken ct) =>
        await Navigator.NavigateDataAsync(this, detail.ArtworkSummary, cancellation: ct);

    private string Author => Settings.Surveyor is { Length: > 0 } name ? name : Text.Get("Common_UnnamedSurveyor");

    private async Task<Treatment?> TreatmentAsync(CancellationToken ct) =>
        (await Store.GetAsync(ct)).Treatments.FirstOrDefault(t => t.Id == Summary.Id);

    private async Task<(DateOnly? Date, string Assignee, string Note)> ReadFieldsAsync()
    {
        var date = await ScheduledFor;
        return (date is { } d ? DateOnly.FromDateTime(d.Date) : null, await Assignee ?? string.Empty, await Note ?? string.Empty);
    }

    private async ValueTask AfterWrite(Problem problem, string success, CancellationToken ct)
    {
        if (problem == Problem.None)
        {
            await Note.UpdateAsync(_ => string.Empty, ct);
        }

        await Message.UpdateAsync(_ => problem == Problem.None ? success : Text.Problem(problem), ct);
    }

    private static IImmutableList<StepView> Stepper(TreatmentStatus status) =>
        Enum.GetValues<TreatmentStatus>()
            .Select(s => new StepView((int)s + 1, Text.Enum(s), s < status || status == TreatmentStatus.Done, s == status && s != TreatmentStatus.Done))
            .ToImmutableList();
}
