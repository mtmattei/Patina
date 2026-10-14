using System.Collections.Immutable;
using Patina.Core.Model;

namespace Patina.Core.Queries;

/// <summary>One artwork as the collection list and map show it.</summary>
public sealed partial record ArtworkSummary(
    string Id,
    string Accession,
    string Title,
    string Artist,
    int Year,
    ArtMaterial Material,
    string District,
    string Site,
    double Latitude,
    double Longitude,
    ConditionGrade Grade,
    DueState Due,
    DateOnly? LastSurveyed,
    DateOnly? NextDue,
    int OpenTreatments,
    string? CoverPhotoId,
    bool HasDraft)
{
    public bool NeedsAttention => Rules.ConditionRules.NeedsAttention(Grade, Due);
}

public sealed partial record CollectionStats(int Total, int NeedsAttention, int Overdue, int Critical, int Unsurveyed);

public sealed partial record CollectionView(ImmutableList<ArtworkSummary> Items, CollectionStats Stats);

public sealed partial record TreatmentSummary(
    string Id,
    string ArtworkId,
    string ArtworkTitle,
    string Accession,
    string Title,
    TreatmentPriority Priority,
    TreatmentStatus Status,
    DateOnly? ScheduledFor,
    string Assignee,
    DateOnly CreatedOn,
    DateOnly? CompletedOn,
    bool IsLate);

public sealed partial record QueueStats(int Open, int Urgent, int ScheduledThisWeek, int InProgress);

public sealed partial record ArtworkDossier(
    Artwork Artwork,
    ArtworkSummary Summary,
    ImmutableList<Survey> Surveys,
    Survey? Draft,
    ImmutableList<TreatmentSummary> OpenTreatments,
    ImmutableList<TreatmentSummary> CompletedTreatments,
    PhotoRef? Cover);

public sealed partial record TreatmentDetail(
    Treatment Treatment,
    TreatmentSummary Summary,
    Artwork Artwork,
    ArtworkSummary ArtworkSummary,
    Survey? Survey,
    Finding? Finding);
