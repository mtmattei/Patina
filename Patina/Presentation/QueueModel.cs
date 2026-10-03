using Patina.Core.Model;
using Patina.Core.Queries;
using Patina.Core.Storage;

namespace Patina.Presentation;

public sealed record QueueFilterOption(QueueFilter Value, string Label);

/// <summary>The treatment queue: late work first, then urgent, then by date.</summary>
public partial record QueueModel(PatinaStore Store)
{
    public IImmutableList<QueueFilterOption> Filters { get; } =
        Enum.GetValues<QueueFilter>().Select(f => new QueueFilterOption(f, Services.Text.Enum(f))).ToImmutableList();

    public IState<QueueFilterOption> Filter => State.Value(this, () => Filters[0]);

    private IFeed<PatinaDocument> Document => Feed.AsyncEnumerable(Store.Watch);

    public IListFeed<TreatmentSummary> Treatments =>
        Feed.Combine(Document, Filter)
            .Select(x => PatinaQueries.Queue(x.Item1, Store.Clock.Today, x.Item2.Value))
            .AsListFeed();

    public IFeed<QueueStats> Stats => Document.Select(doc => PatinaQueries.QueueStatistics(doc, Store.Clock.Today));

    public async ValueTask ShowOpen(CancellationToken ct) => await Filter.UpdateAsync(_ => Filters[0], ct);
}
