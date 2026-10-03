using Patina.Core.Model;
using Patina.Core.Queries;
using Patina.Core.Storage;

namespace Patina.Presentation;

/// <summary>Navigation data for the survey page: which artwork, nothing else (the draft is loaded from the store).</summary>
public sealed record SurveyStart(string ArtworkId);

/// <summary>One artwork's record: the label, its condition, open work and survey history.</summary>
public partial record ArtworkModel(ArtworkSummary Summary, PatinaStore Store, INavigator Navigator)
{
    private IFeed<PatinaDocument> Document => Feed.AsyncEnumerable(Store.Watch);

    /// <summary>None when the artwork no longer exists (for example after an import replaced the collection).</summary>
    public IFeed<ArtworkDossier> Dossier => Document.Select(doc => PatinaQueries.Dossier(doc, Summary.Id, Store.Clock.Today)!);

    /// <summary>Header shown before the dossier loads, from the navigation data.</summary>
    public string Title => Summary.Title;

    public async ValueTask StartSurvey(CancellationToken ct) =>
        await Navigator.NavigateDataAsync(this, new SurveyStart(Summary.Id), cancellation: ct);
}
