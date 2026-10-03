using System.Collections.Immutable;
using System.Globalization;
using Patina.Core.Model;
using Patina.Core.Rules;

namespace Patina.Core.Queries;

/// <summary>Read-side projections of the document. Pure functions of (document, today).</summary>
public static class PatinaQueries
{
    private static readonly CompareInfo Compare = CultureInfo.InvariantCulture.CompareInfo;
    private const CompareOptions SearchOptions = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    public static Survey? LatestSubmitted(PatinaDocument doc, string artworkId) =>
        doc.Surveys
            .Where(s => s.ArtworkId == artworkId && s.Status == SurveyStatus.Submitted)
            .OrderByDescending(s => s.Date)
            .ThenByDescending(s => s.Id, StringComparer.Ordinal)
            .FirstOrDefault();

    public static ArtworkSummary Summarize(PatinaDocument doc, Artwork artwork, DateOnly today)
    {
        var last = LatestSubmitted(doc, artwork.Id);
        var next = ConditionRules.NextDue(artwork.Material, last);
        return new ArtworkSummary(
            artwork.Id,
            artwork.Accession,
            artwork.Title,
            artwork.Artist,
            artwork.Year,
            artwork.Material,
            artwork.District,
            artwork.Site,
            artwork.Latitude,
            artwork.Longitude,
            last?.Grade ?? ConditionGrade.Unsurveyed,
            ConditionRules.Due(next, today),
            last?.Date,
            next,
            doc.Treatments.Count(t => t.ArtworkId == artwork.Id && t.Status != TreatmentStatus.Done),
            artwork.CoverPhotoId,
            SurveyOps.DraftFor(doc, artwork.Id) is not null);
    }

    public static CollectionView Collection(PatinaDocument doc, DateOnly today, string? query, CollectionFilter filter)
    {
        var all = doc.Artworks.Select(a => Summarize(doc, a, today)).ToList();

        var stats = new CollectionStats(
            all.Count,
            all.Count(s => s.NeedsAttention),
            all.Count(s => s.Due == DueState.Overdue),
            all.Count(s => s.Grade == ConditionGrade.Critical),
            all.Count(s => s.Due == DueState.NeverSurveyed));

        var items = all
            .Where(s => Matches(s, query))
            .Where(s => filter switch
            {
                CollectionFilter.NeedsAttention => s.NeedsAttention,
                CollectionFilter.Overdue => s.Due == DueState.Overdue,
                CollectionFilter.Unsurveyed => s.Due == DueState.NeverSurveyed,
                _ => true,
            })
            .OrderByDescending(s => Urgency(s))
            .ThenBy(s => s.NextDue ?? DateOnly.MinValue)
            .ThenBy(s => s.Title, StringComparer.CurrentCulture)
            .ToImmutableList();

        return new CollectionView(items, stats);
    }

    /// <summary>Critical first, then poor, then overdue or never surveyed, then everything else.</summary>
    public static int Urgency(ArtworkSummary s) =>
        (s.Grade == ConditionGrade.Critical ? 8 : 0)
        + (s.Grade == ConditionGrade.Poor ? 4 : 0)
        + (s.Due is DueState.Overdue or DueState.NeverSurveyed ? 2 : 0)
        + (s.Due == DueState.DueSoon ? 1 : 0);

    public static bool Matches(ArtworkSummary s, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var q = query.Trim();
        return Contains(s.Title, q) || Contains(s.Artist, q) || Contains(s.Accession, q)
            || Contains(s.District, q) || Contains(s.Site, q);
    }

    private static bool Contains(string source, string value) => Compare.IndexOf(source, value, SearchOptions) >= 0;

    public static ArtworkDossier? Dossier(PatinaDocument doc, string artworkId, DateOnly today)
    {
        var artwork = doc.Artworks.FirstOrDefault(a => a.Id == artworkId);
        if (artwork is null)
        {
            return null;
        }

        var treatments = doc.Treatments
            .Where(t => t.ArtworkId == artworkId)
            .Select(t => Summarize(doc, t, artwork, today))
            .ToList();

        return new ArtworkDossier(
            artwork,
            Summarize(doc, artwork, today),
            doc.Surveys
                .Where(s => s.ArtworkId == artworkId && s.Status == SurveyStatus.Submitted)
                .OrderByDescending(s => s.Date)
                .ToImmutableList(),
            SurveyOps.DraftFor(doc, artworkId),
            treatments.Where(t => t.Status != TreatmentStatus.Done).OrderByDescending(t => t.Priority).ThenBy(t => t.CreatedOn).ToImmutableList(),
            treatments.Where(t => t.Status == TreatmentStatus.Done).OrderByDescending(t => t.CompletedOn).ToImmutableList(),
            artwork.CoverPhotoId is { } id ? doc.Photos.FirstOrDefault(p => p.Id == id) : null);
    }

    public static TreatmentSummary Summarize(PatinaDocument doc, Treatment t, Artwork artwork, DateOnly today) =>
        new(
            t.Id,
            t.ArtworkId,
            artwork.Title,
            artwork.Accession,
            t.Title,
            t.Priority,
            t.Status,
            t.ScheduledFor,
            t.Assignee,
            t.CreatedOn,
            t.CompletedOn,
            t.Status is TreatmentStatus.Scheduled or TreatmentStatus.InProgress && t.ScheduledFor is { } d && d < today);

    public static ImmutableList<TreatmentSummary> Queue(PatinaDocument doc, DateOnly today, QueueFilter filter)
    {
        var artworks = doc.Artworks.ToDictionary(a => a.Id);
        return doc.Treatments
            .Where(t => artworks.ContainsKey(t.ArtworkId))
            .Where(t => filter switch
            {
                QueueFilter.Open => t.Status != TreatmentStatus.Done,
                QueueFilter.Proposed => t.Status == TreatmentStatus.Proposed,
                QueueFilter.Scheduled => t.Status == TreatmentStatus.Scheduled,
                QueueFilter.InProgress => t.Status == TreatmentStatus.InProgress,
                _ => t.Status == TreatmentStatus.Done,
            })
            .Select(t => Summarize(doc, t, artworks[t.ArtworkId], today))
            .OrderByDescending(t => t.IsLate)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.ScheduledFor ?? DateOnly.MaxValue)
            .ThenBy(t => t.CreatedOn)
            .ToImmutableList();
    }

    public static QueueStats QueueStatistics(PatinaDocument doc, DateOnly today)
    {
        var open = doc.Treatments.Where(t => t.Status != TreatmentStatus.Done).ToList();
        var weekEnd = today.AddDays(7);
        return new QueueStats(
            open.Count,
            open.Count(t => t.Priority == TreatmentPriority.Urgent),
            open.Count(t => t.Status == TreatmentStatus.Scheduled && t.ScheduledFor is { } d && d >= today && d <= weekEnd),
            open.Count(t => t.Status == TreatmentStatus.InProgress));
    }

    public static TreatmentDetail? Treatment(PatinaDocument doc, string treatmentId, DateOnly today)
    {
        var t = doc.Treatments.FirstOrDefault(x => x.Id == treatmentId);
        var artwork = t is null ? null : doc.Artworks.FirstOrDefault(a => a.Id == t.ArtworkId);
        if (t is null || artwork is null)
        {
            return null;
        }

        var survey = t.SurveyId is { } sid ? doc.Surveys.FirstOrDefault(s => s.Id == sid) : null;
        var finding = survey?.Findings.FirstOrDefault(f => f.Id == t.FindingId);
        return new TreatmentDetail(t, Summarize(doc, t, artwork, today), artwork, survey, finding);
    }
}
