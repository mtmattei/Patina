using System.Collections.Immutable;

namespace Patina.Core.Model;

public sealed record Artwork(
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
    string Description,
    string? CoverPhotoId = null);

/// <summary>A photo file stored by the app under <c>photos/{Id}{Extension}</c>.</summary>
public sealed record PhotoRef(string Id, string Extension, string OriginalName, DateTimeOffset AddedAt)
{
    public string FileName => Id + Extension;
}

public sealed record Finding(
    string Id,
    FindingType Type,
    string Zone,
    Severity Severity,
    string Note,
    ImmutableList<string> PhotoIds);

public sealed record Survey(
    string Id,
    string ArtworkId,
    SurveyStatus Status,
    DateOnly Date,
    string Surveyor,
    Weather Weather,
    string Notes,
    ImmutableList<Finding> Findings,
    ImmutableList<string> PhotoIds,
    ConditionGrade Grade);

/// <summary>
/// One line in a treatment's history. <see cref="LogKind.Note"/> carries free text written by the crew;
/// the other kinds are recorded by the app and rendered in the reader's language.
/// </summary>
public sealed record TreatmentLogEntry(DateTimeOffset At, string Author, LogKind Kind, string Text = "", TreatmentStatus? Status = null);

public sealed record Treatment(
    string Id,
    string ArtworkId,
    string? SurveyId,
    string? FindingId,
    string Title,
    TreatmentPriority Priority,
    TreatmentStatus Status,
    DateOnly? ScheduledFor,
    string Assignee,
    ImmutableList<TreatmentLogEntry> Log,
    DateOnly CreatedOn,
    DateOnly? CompletedOn);

/// <summary>The whole record kept on the device. One JSON file; <see cref="Version"/> is the schema version.</summary>
public sealed record PatinaDocument(
    int Version,
    ImmutableList<Artwork> Artworks,
    ImmutableList<Survey> Surveys,
    ImmutableList<Treatment> Treatments,
    ImmutableList<PhotoRef> Photos)
{
    public const int CurrentVersion = 1;

    public static PatinaDocument Empty { get; } = new(CurrentVersion, [], [], [], []);
}
