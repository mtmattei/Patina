using static Patina.Tests.Fixtures;

namespace Patina.Tests;

public class SurveyOpsTests
{
    [Test]
    public void Draft_saves_without_validation_and_is_graded()
    {
        var doc = Doc(Artwork());
        var draft = SurveyOps.NewDraft("x1", string.Empty, Today) with { Findings = [Finding(Severity.Serious, zone: "")] };

        var result = SurveyOps.SaveDraft(doc, draft);

        result.IsOk.Should().BeTrue();
        var saved = result.Value!.Surveys.Single();
        saved.Status.Should().Be(SurveyStatus.Draft);
        saved.Grade.Should().Be(ConditionGrade.Poor);
        SurveyOps.DraftFor(result.Value, "x1").Should().NotBeNull();
    }

    [Test]
    public void Saving_a_draft_twice_replaces_it()
    {
        var doc = Doc(Artwork());
        var draft = SurveyOps.NewDraft("x1", "C. Lavoie", Today);
        doc = SurveyOps.SaveDraft(doc, draft).Value!;
        doc = SurveyOps.SaveDraft(doc, draft with { Notes = "second" }).Value!;

        doc.Surveys.Should().ContainSingle().Which.Notes.Should().Be("second");
    }

    [Test]
    public void Draft_for_unknown_artwork_is_refused() =>
        SurveyOps.SaveDraft(Doc(), SurveyOps.NewDraft("nope", "x", Today)).Problem.Should().Be(Problem.ArtworkNotFound);

    [Test]
    public void Submit_requires_a_surveyor()
    {
        var draft = SurveyOps.NewDraft("x1", "  ", Today);
        SurveyOps.Submit(Doc(Artwork()), draft, Now).Problem.Should().Be(Problem.SurveyorRequired);
    }

    [Test]
    public void Submit_requires_a_zone_on_every_finding()
    {
        var draft = SurveyOps.NewDraft("x1", "C. Lavoie", Today) with { Findings = [Finding(Severity.Minor, zone: " ")] };
        SurveyOps.Submit(Doc(Artwork()), draft, Now).Problem.Should().Be(Problem.FindingZoneRequired);
    }

    [Test]
    public void Submit_proposes_treatments_for_serious_and_urgent_findings_only()
    {
        var draft = SurveyOps.NewDraft("x1", "C. Lavoie", Today) with
        {
            Findings = [Finding(Severity.Minor), Finding(Severity.Moderate), Finding(Severity.Serious, "Base"), Finding(Severity.Urgent, "Arm")],
        };

        var doc = SurveyOps.Submit(Doc(Artwork()), draft, Now).Value!;

        doc.Surveys.Single().Status.Should().Be(SurveyStatus.Submitted);
        doc.Surveys.Single().Grade.Should().Be(ConditionGrade.Critical);
        doc.Treatments.Should().HaveCount(2);
        doc.Treatments.Should().OnlyContain(t => t.Status == TreatmentStatus.Proposed && t.SurveyId == draft.Id);
        doc.Treatments.Select(t => t.Priority).Should().BeEquivalentTo([TreatmentPriority.High, TreatmentPriority.Urgent]);
        doc.Treatments.Should().OnlyContain(t => t.Log.Single().Kind == LogKind.Proposed);
        doc.Treatments.Select(t => t.Title).Should().Contain("Treat corrosion: Arm");
    }

    [Test]
    public void Submit_uses_the_supplied_title_builder()
    {
        var draft = SurveyOps.NewDraft("x1", "C. Lavoie", Today) with { Findings = [Finding(Severity.Urgent, "Bras")] };

        var doc = SurveyOps.Submit(Doc(Artwork()), draft, Now, f => $"Traiter : {f.Zone}").Value!;

        doc.Treatments.Single().Title.Should().Be("Traiter : Bras");
    }

    [Test]
    public void Submit_sets_a_cover_photo_when_the_artwork_has_none()
    {
        var draft = SurveyOps.NewDraft("x1", "C. Lavoie", Today) with { PhotoIds = ["p1", "p2"] };

        var doc = SurveyOps.Submit(Doc(Artwork()), draft, Now).Value!;

        doc.Artworks.Single().CoverPhotoId.Should().Be("p1");
    }

    [Test]
    public void Submit_keeps_an_existing_cover_photo()
    {
        var draft = SurveyOps.NewDraft("x1", "C. Lavoie", Today) with { PhotoIds = ["p2"] };

        var doc = SurveyOps.Submit(Doc(Artwork() with { CoverPhotoId = "p1" }), draft, Now).Value!;

        doc.Artworks.Single().CoverPhotoId.Should().Be("p1");
    }

    [Test]
    public void A_submitted_survey_cannot_be_submitted_or_discarded_again()
    {
        var draft = SurveyOps.NewDraft("x1", "C. Lavoie", Today);
        var doc = SurveyOps.Submit(Doc(Artwork()), draft, Now).Value!;

        SurveyOps.Submit(doc, draft, Now).Problem.Should().Be(Problem.SurveyAlreadySubmitted);
        SurveyOps.SaveDraft(doc, draft).Problem.Should().Be(Problem.SurveyAlreadySubmitted);
        SurveyOps.DiscardDraft(doc, draft.Id).Problem.Should().Be(Problem.SurveyAlreadySubmitted);
    }

    [Test]
    public void Discard_removes_the_draft()
    {
        var draft = SurveyOps.NewDraft("x1", "C. Lavoie", Today);
        var doc = SurveyOps.SaveDraft(Doc(Artwork()), draft).Value!;

        SurveyOps.DiscardDraft(doc, draft.Id).Value!.Surveys.Should().BeEmpty();
    }
}
