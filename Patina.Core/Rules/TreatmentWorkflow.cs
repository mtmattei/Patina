using Patina.Core.Model;

namespace Patina.Core.Rules;

/// <summary>
/// Proposed → Scheduled (needs a date) → In progress → Done (needs a completion note).
/// A step back returns to the previous status; Done is final.
/// </summary>
public static class TreatmentWorkflow
{
    public static TreatmentStatus? Next(TreatmentStatus status) => status switch
    {
        TreatmentStatus.Proposed => TreatmentStatus.Scheduled,
        TreatmentStatus.Scheduled => TreatmentStatus.InProgress,
        TreatmentStatus.InProgress => TreatmentStatus.Done,
        _ => null,
    };

    public static TreatmentStatus? Previous(TreatmentStatus status) => status switch
    {
        TreatmentStatus.Scheduled => TreatmentStatus.Proposed,
        TreatmentStatus.InProgress => TreatmentStatus.Scheduled,
        _ => null,
    };

    /// <summary>Saves the editable fields without changing status. A note, when given, is appended to the log.</summary>
    public static Result<PatinaDocument> Save(
        PatinaDocument doc, string treatmentId, DateOnly? scheduledFor, string assignee, string? note, string author, DateTimeOffset now)
    {
        var treatment = doc.Treatments.FirstOrDefault(t => t.Id == treatmentId);
        if (treatment is null)
        {
            return Result<PatinaDocument>.Fail(Problem.TreatmentNotFound);
        }

        if (treatment.Status == TreatmentStatus.Done)
        {
            return Result<PatinaDocument>.Fail(Problem.TreatmentAlreadyDone);
        }

        var updated = treatment with
        {
            ScheduledFor = scheduledFor,
            Assignee = assignee.Trim(),
            Log = string.IsNullOrWhiteSpace(note) ? treatment.Log : treatment.Log.Add(new(now, author, LogKind.Note, note.Trim())),
        };

        return Result<PatinaDocument>.Ok(doc with { Treatments = doc.Treatments.Replace(treatment, updated) });
    }

    public static Result<PatinaDocument> Advance(
        PatinaDocument doc, string treatmentId, DateOnly? scheduledFor, string assignee, string? note, string author, DateTimeOffset now)
    {
        var treatment = doc.Treatments.FirstOrDefault(t => t.Id == treatmentId);
        if (treatment is null)
        {
            return Result<PatinaDocument>.Fail(Problem.TreatmentNotFound);
        }

        if (Next(treatment.Status) is not { } next)
        {
            return Result<PatinaDocument>.Fail(Problem.TreatmentAlreadyDone);
        }

        if (next == TreatmentStatus.Scheduled && scheduledFor is null)
        {
            return Result<PatinaDocument>.Fail(Problem.ScheduleDateRequired);
        }

        if (next == TreatmentStatus.Done && string.IsNullOrWhiteSpace(note))
        {
            return Result<PatinaDocument>.Fail(Problem.CompletionNoteRequired);
        }

        var entry = string.IsNullOrWhiteSpace(note)
            ? new TreatmentLogEntry(now, author, LogKind.StatusChanged, Status: next)
            : new TreatmentLogEntry(now, author, LogKind.StatusChanged, note.Trim(), next);

        var updated = treatment with
        {
            Status = next,
            ScheduledFor = scheduledFor ?? treatment.ScheduledFor,
            Assignee = assignee.Trim(),
            Log = treatment.Log.Add(entry),
            CompletedOn = next == TreatmentStatus.Done ? DateOnly.FromDateTime(now.LocalDateTime) : null,
        };

        return Result<PatinaDocument>.Ok(doc with { Treatments = doc.Treatments.Replace(treatment, updated) });
    }

    public static Result<PatinaDocument> StepBack(PatinaDocument doc, string treatmentId, string author, DateTimeOffset now)
    {
        var treatment = doc.Treatments.FirstOrDefault(t => t.Id == treatmentId);
        if (treatment is null)
        {
            return Result<PatinaDocument>.Fail(Problem.TreatmentNotFound);
        }

        if (Previous(treatment.Status) is not { } previous)
        {
            return Result<PatinaDocument>.Fail(treatment.Status == TreatmentStatus.Done ? Problem.TreatmentAlreadyDone : Problem.NoEarlierStatus);
        }

        var updated = treatment with
        {
            Status = previous,
            Log = treatment.Log.Add(new(now, author, LogKind.StatusReturned, Status: previous)),
        };

        return Result<PatinaDocument>.Ok(doc with { Treatments = doc.Treatments.Replace(treatment, updated) });
    }
}
