using System.Collections.Immutable;
using Patina.Core.Model;

namespace Patina.Core.Rules;

/// <summary>Draft and submit operations on surveys. Pure: each returns a new document.</summary>
public static class SurveyOps
{
    public static Survey NewDraft(string artworkId, string surveyor, DateOnly today) =>
        new(Ids.New(), artworkId, SurveyStatus.Draft, today, surveyor, Weather.Dry, string.Empty, [], [], ConditionGrade.Good);

    public static Survey? DraftFor(PatinaDocument doc, string artworkId) =>
        doc.Surveys.FirstOrDefault(s => s.ArtworkId == artworkId && s.Status == SurveyStatus.Draft);

    /// <summary>Inserts or replaces the draft. Drafts are not validated: a half-done survey must be saveable in the field.</summary>
    public static Result<PatinaDocument> SaveDraft(PatinaDocument doc, Survey draft)
    {
        if (doc.Artworks.All(a => a.Id != draft.ArtworkId))
        {
            return Result<PatinaDocument>.Fail(Problem.ArtworkNotFound);
        }

        var existing = doc.Surveys.FirstOrDefault(s => s.Id == draft.Id);
        if (existing is { Status: SurveyStatus.Submitted })
        {
            return Result<PatinaDocument>.Fail(Problem.SurveyAlreadySubmitted);
        }

        var stored = draft with { Status = SurveyStatus.Draft, Grade = ConditionRules.Grade(draft.Findings) };
        var surveys = existing is null ? doc.Surveys.Add(stored) : doc.Surveys.Replace(existing, stored);
        return Result<PatinaDocument>.Ok(doc with { Surveys = surveys });
    }

    public static Problem Validate(Survey survey)
    {
        if (string.IsNullOrWhiteSpace(survey.Surveyor))
        {
            return Problem.SurveyorRequired;
        }

        return survey.Findings.Any(f => string.IsNullOrWhiteSpace(f.Zone)) ? Problem.FindingZoneRequired : Problem.None;
    }

    /// <summary>
    /// Submits the survey: grades it, proposes one treatment per serious or urgent finding, and gives the artwork
    /// a cover photo when it has none.
    /// </summary>
    public static Result<PatinaDocument> Submit(PatinaDocument doc, Survey survey, DateTimeOffset now, Func<Finding, string>? titleFor = null)
    {
        var problem = Validate(survey);
        if (problem != Problem.None)
        {
            return Result<PatinaDocument>.Fail(problem);
        }

        var artwork = doc.Artworks.FirstOrDefault(a => a.Id == survey.ArtworkId);
        if (artwork is null)
        {
            return Result<PatinaDocument>.Fail(Problem.ArtworkNotFound);
        }

        var existing = doc.Surveys.FirstOrDefault(s => s.Id == survey.Id);
        if (existing is { Status: SurveyStatus.Submitted })
        {
            return Result<PatinaDocument>.Fail(Problem.SurveyAlreadySubmitted);
        }

        var submitted = survey with
        {
            Status = SurveyStatus.Submitted,
            Grade = ConditionRules.Grade(survey.Findings),
        };

        var surveys = existing is null ? doc.Surveys.Add(submitted) : doc.Surveys.Replace(existing, submitted);
        var proposals = ProposeTreatments(submitted, now, titleFor);

        var cover = artwork.CoverPhotoId
            ?? submitted.PhotoIds.FirstOrDefault()
            ?? submitted.Findings.SelectMany(f => f.PhotoIds).FirstOrDefault();
        var artworks = cover == artwork.CoverPhotoId
            ? doc.Artworks
            : doc.Artworks.Replace(artwork, artwork with { CoverPhotoId = cover });

        return Result<PatinaDocument>.Ok(doc with
        {
            Surveys = surveys,
            Treatments = doc.Treatments.AddRange(proposals),
            Artworks = artworks,
        });
    }

    /// <param name="titleFor">Builds the treatment title in the surveyor's language; defaults to English.</param>
    public static ImmutableList<Treatment> ProposeTreatments(Survey survey, DateTimeOffset now, Func<Finding, string>? titleFor = null) =>
        survey.Findings
            .Where(f => f.Severity >= Severity.Serious)
            .Select(f => new Treatment(
                Ids.New(),
                survey.ArtworkId,
                survey.Id,
                f.Id,
                (titleFor ?? TreatmentTitles.For)(f),
                f.Severity == Severity.Urgent ? TreatmentPriority.Urgent : TreatmentPriority.High,
                TreatmentStatus.Proposed,
                null,
                string.Empty,
                [new TreatmentLogEntry(now, survey.Surveyor, LogKind.Proposed)],
                DateOnly.FromDateTime(now.LocalDateTime),
                null))
            .ToImmutableList();

    public static Result<PatinaDocument> DiscardDraft(PatinaDocument doc, string surveyId)
    {
        var existing = doc.Surveys.FirstOrDefault(s => s.Id == surveyId);
        if (existing is null)
        {
            return Result<PatinaDocument>.Ok(doc);
        }

        return existing.Status == SurveyStatus.Submitted
            ? Result<PatinaDocument>.Fail(Problem.SurveyAlreadySubmitted)
            : Result<PatinaDocument>.Ok(doc with { Surveys = doc.Surveys.Remove(existing) });
    }
}

/// <summary>English default treatment titles. The app passes localized titles; titles are stored as text.</summary>
public static class TreatmentTitles
{
    public static string For(Finding finding)
    {
        var verb = finding.Type switch
        {
            FindingType.Corrosion => "Treat corrosion",
            FindingType.Graffiti => "Remove graffiti",
            FindingType.Cracking => "Repair cracking",
            FindingType.MaterialLoss => "Consolidate losses",
            FindingType.BiologicalGrowth => "Remove biological growth",
            FindingType.Flaking => "Consolidate flaking paint",
            FindingType.Fading => "Assess fading",
            FindingType.Efflorescence => "Reduce efflorescence",
            FindingType.CoatingFailure => "Renew protective coating",
            FindingType.StructuralMovement => "Stabilize structure",
            FindingType.Soiling => "Clean surface",
            _ => "Repair vandalism damage",
        };

        return string.IsNullOrWhiteSpace(finding.Zone) ? verb : $"{verb}: {finding.Zone.Trim()}";
    }
}
