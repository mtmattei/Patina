using static Patina.Tests.Fixtures;

namespace Patina.Tests;

public class QueriesTests
{
    private static PatinaDocument Sample() => SampleCollection.Create(Today);

    [Test]
    public void Collection_stats_count_the_whole_collection_regardless_of_filter()
    {
        var view = PatinaQueries.Collection(Sample(), Today, "ferry", CollectionFilter.Overdue);

        view.Stats.Total.Should().Be(18);
        view.Stats.Unsurveyed.Should().Be(2);
        view.Stats.Critical.Should().Be(2);
    }

    [Test]
    public void Search_ignores_case_and_accents()
    {
        var view = PatinaQueries.Collection(Sample(), Today, "helene marchand", CollectionFilter.All);
        view.Items.Should().ContainSingle().Which.Title.Should().Be("Tidewater Gate");

        PatinaQueries.Collection(Sample(), Today, "pa-1925", CollectionFilter.All).Items.Should().ContainSingle()
            .Which.Title.Should().Be("The Ferryman");
    }

    [Test]
    public void Filters()
    {
        var doc = Sample();
        PatinaQueries.Collection(doc, Today, null, CollectionFilter.Unsurveyed).Items
            .Should().OnlyContain(s => s.Due == DueState.NeverSurveyed).And.HaveCount(2);
        PatinaQueries.Collection(doc, Today, null, CollectionFilter.Overdue).Items
            .Should().OnlyContain(s => s.Due == DueState.Overdue).And.NotBeEmpty();
        PatinaQueries.Collection(doc, Today, null, CollectionFilter.NeedsAttention).Items
            .Should().OnlyContain(s => s.NeedsAttention);
    }

    [Test]
    public void Most_urgent_first()
    {
        var items = PatinaQueries.Collection(Sample(), Today, null, CollectionFilter.All).Items;
        items.Take(2).Should().OnlyContain(s => s.Grade == ConditionGrade.Critical);
        items.Select(PatinaQueries.Urgency).Should().BeInDescendingOrder();
    }

    [Test]
    public void Dossier_splits_open_and_completed_treatments_and_orders_surveys()
    {
        var doc = Sample();
        var dossier = PatinaQueries.Dossier(doc, "a08", Today)!;

        dossier.CompletedTreatments.Should().ContainSingle();
        dossier.OpenTreatments.Should().BeEmpty();

        var reader = PatinaQueries.Dossier(doc, "a01", Today)!;
        reader.Surveys.Select(s => s.Date).Should().BeInDescendingOrder();
        reader.Summary.LastSurveyed.Should().Be(reader.Surveys[0].Date);
    }

    [Test]
    public void Dossier_reports_a_draft()
    {
        var doc = SurveyOps.SaveDraft(Sample(), SurveyOps.NewDraft("a04", "x", Today)).Value!;
        var dossier = PatinaQueries.Dossier(doc, "a04", Today)!;

        dossier.Draft.Should().NotBeNull();
        dossier.Summary.HasDraft.Should().BeTrue();
        dossier.Summary.Grade.Should().Be(ConditionGrade.Unsurveyed);
    }

    [Test]
    public void Unknown_ids_return_null()
    {
        PatinaQueries.Dossier(Sample(), "nope", Today).Should().BeNull();
        PatinaQueries.Treatment(Sample(), "nope", Today).Should().BeNull();
    }

    [Test]
    public void Queue_puts_late_then_urgent_first_and_filters_by_status()
    {
        var doc = Sample();
        var open = PatinaQueries.Queue(doc, Today, QueueFilter.Open);

        open.Should().OnlyContain(t => t.Status != TreatmentStatus.Done);
        open[0].IsLate.Should().BeTrue();
        open.Skip(1).First().Priority.Should().Be(TreatmentPriority.Urgent);

        PatinaQueries.Queue(doc, Today, QueueFilter.Done).Should().HaveCount(2).And.OnlyContain(t => t.Status == TreatmentStatus.Done);
    }

    [Test]
    public void Queue_statistics()
    {
        var stats = PatinaQueries.QueueStatistics(Sample(), Today);

        stats.Open.Should().Be(5);
        stats.Urgent.Should().Be(2);
        stats.ScheduledThisWeek.Should().Be(1);
        stats.InProgress.Should().Be(1);
    }

    [Test]
    public void Treatment_detail_links_survey_and_finding()
    {
        var doc = Sample();
        var id = doc.Treatments.First(t => t.ArtworkId == "a11").Id;
        var detail = PatinaQueries.Treatment(doc, id, Today)!;

        detail.Artwork.Title.Should().Be("The Ferryman");
        detail.Finding!.Severity.Should().Be(Severity.Urgent);
        detail.Survey!.Findings.Should().Contain(detail.Finding);
    }
}
