using Patina.Core.Model;
using Patina.Core.Queries;
using Patina.Core.Storage;

namespace Patina.Presentation;

/// <summary>The collection map on narrow windows, where it does not fit beside the list.</summary>
public partial record MapModel(PatinaStore Store)
{
    public IState<string> SelectedId => State<string>.Empty(this);

    private IFeed<PatinaDocument> Document => Feed.AsyncEnumerable(Store.Watch);

    private IFeed<CollectionView> View =>
        Document.Select(doc => PatinaQueries.Collection(doc, Store.Clock.Today, null, CollectionFilter.All));

    public IListFeed<ArtworkSummary> Artworks => View.Select(v => v.Items).AsListFeed();

    public IFeed<ArtworkSummary> Selected =>
        Feed.Combine(View, SelectedId)
            .Select(x => x.Item1.Items.FirstOrDefault(a => a.Id == x.Item2)!);

    public async ValueTask ClearSelection(CancellationToken ct) => await SelectedId.UpdateAsync(_ => null!, ct);
}
