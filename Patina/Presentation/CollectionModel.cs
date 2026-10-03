using Patina.Core.Model;
using Patina.Core.Queries;
using Patina.Core.Storage;

namespace Patina.Presentation;

public sealed record FilterOption(CollectionFilter Value, string Label);

/// <summary>
/// The collection: every artwork, ordered by urgency, filtered by search and a filter chip.
/// Reads only; every write happens on the artwork, survey and treatment pages.
/// </summary>
public partial record CollectionModel(PatinaStore Store)
{
    public IImmutableList<FilterOption> Filters { get; } =
        Enum.GetValues<CollectionFilter>().Select(f => new FilterOption(f, Services.Text.Enum(f))).ToImmutableList();

    public IState<string> Query => State.Value(this, () => string.Empty);

    public IState<FilterOption> Filter => State.Value(this, () => Filters[0]);

    /// <summary>The artwork whose pin is selected on the map.</summary>
    public IState<string> SelectedId => State<string>.Empty(this);

    private IFeed<PatinaDocument> Document => Feed.AsyncEnumerable(Store.Watch);

    public IFeed<CollectionView> View =>
        Feed.Combine(Document, Query, Filter)
            .Select(x => PatinaQueries.Collection(x.Item1, Store.Clock.Today, x.Item2, x.Item3.Value));

    public IListFeed<ArtworkSummary> Artworks => View.Select(v => v.Items).AsListFeed();

    public IFeed<CollectionStats> Stats => View.Select(v => v.Stats);

    /// <summary>The selected pin's artwork, for the map callout. None when nothing is selected.</summary>
    public IFeed<ArtworkSummary> Selected =>
        Feed.Combine(View, SelectedId)
            .Select(x => x.Item1.Items.FirstOrDefault(a => a.Id == x.Item2)!);

    public async ValueTask ClearFilters(CancellationToken ct)
    {
        await Query.UpdateAsync(_ => string.Empty, ct);
        await Filter.UpdateAsync(_ => Filters[0], ct);
    }

    public async ValueTask ClearSelection(CancellationToken ct) => await SelectedId.UpdateAsync(_ => null!, ct);
}
