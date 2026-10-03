using static Patina.Tests.Fixtures;

namespace Patina.Tests;

public class TreatmentWorkflowTests
{
    private static (PatinaDocument Doc, string Id) Proposed()
    {
        var draft = SurveyOps.NewDraft("x1", "C. Lavoie", Today) with { Findings = [Finding(Severity.Urgent, "Arm")] };
        var doc = SurveyOps.Submit(Doc(Artwork()), draft, Now).Value!;
        return (doc, doc.Treatments.Single().Id);
    }

    [Test]
    public void Scheduling_requires_a_date()
    {
        var (doc, id) = Proposed();
        TreatmentWorkflow.Advance(doc, id, null, "M. Haddad", null, "M. Haddad", Now).Problem.Should().Be(Problem.ScheduleDateRequired);
    }

    [Test]
    public void Full_path_to_done()
    {
        var (doc, id) = Proposed();

        doc = TreatmentWorkflow.Advance(doc, id, Today.AddDays(2), "M. Haddad", null, "M. Haddad", Now).Value!;
        doc = TreatmentWorkflow.Advance(doc, id, Today.AddDays(2), "M. Haddad", null, "M. Haddad", Now).Value!;
        TreatmentWorkflow.Advance(doc, id, Today.AddDays(2), "M. Haddad", " ", "M. Haddad", Now).Problem
            .Should().Be(Problem.CompletionNoteRequired);
        doc = TreatmentWorkflow.Advance(doc, id, Today.AddDays(2), "M. Haddad", "Joint re-pinned.", "M. Haddad", Now).Value!;

        var t = doc.Treatments.Single();
        t.Status.Should().Be(TreatmentStatus.Done);
        t.CompletedOn.Should().Be(DateOnly.FromDateTime(Now.LocalDateTime));
        t.Log.Should().HaveCount(4);
        t.Log.Last().Text.Should().Be("Joint re-pinned.");
        t.Log.Last().Status.Should().Be(TreatmentStatus.Done);
    }

    [Test]
    public void Done_is_final()
    {
        var (doc, id) = Proposed();
        doc = TreatmentWorkflow.Advance(doc, id, Today, "a", null, "a", Now).Value!;
        doc = TreatmentWorkflow.Advance(doc, id, Today, "a", null, "a", Now).Value!;
        doc = TreatmentWorkflow.Advance(doc, id, Today, "a", "done", "a", Now).Value!;

        TreatmentWorkflow.Advance(doc, id, Today, "a", "again", "a", Now).Problem.Should().Be(Problem.TreatmentAlreadyDone);
        TreatmentWorkflow.StepBack(doc, id, "a", Now).Problem.Should().Be(Problem.TreatmentAlreadyDone);
        TreatmentWorkflow.Save(doc, id, Today, "b", null, "a", Now).Problem.Should().Be(Problem.TreatmentAlreadyDone);
    }

    [Test]
    public void Step_back_returns_to_the_previous_status()
    {
        var (doc, id) = Proposed();
        TreatmentWorkflow.StepBack(doc, id, "a", Now).Problem.Should().Be(Problem.NoEarlierStatus);

        doc = TreatmentWorkflow.Advance(doc, id, Today, "a", null, "a", Now).Value!;
        doc = TreatmentWorkflow.StepBack(doc, id, "a", Now).Value!;

        doc.Treatments.Single().Status.Should().Be(TreatmentStatus.Proposed);
        doc.Treatments.Single().Log.Last().Kind.Should().Be(LogKind.StatusReturned);
    }

    [Test]
    public void Save_keeps_status_and_logs_only_real_notes()
    {
        var (doc, id) = Proposed();

        doc = TreatmentWorkflow.Save(doc, id, Today.AddDays(5), "  M. Haddad ", "  ", "a", Now).Value!;
        doc.Treatments.Single().Log.Should().HaveCount(1);
        doc.Treatments.Single().Assignee.Should().Be("M. Haddad");

        doc = TreatmentWorkflow.Save(doc, id, Today.AddDays(5), "M. Haddad", "Borough permit requested.", "a", Now).Value!;
        var t = doc.Treatments.Single();
        t.Status.Should().Be(TreatmentStatus.Proposed);
        t.ScheduledFor.Should().Be(Today.AddDays(5));
        t.Log.Last().Kind.Should().Be(LogKind.Note);
    }

    [Test]
    public void Unknown_treatment_is_refused() =>
        TreatmentWorkflow.Advance(Doc(), "nope", Today, "a", null, "a", Now).Problem.Should().Be(Problem.TreatmentNotFound);
}
