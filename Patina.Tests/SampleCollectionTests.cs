using static Patina.Tests.Fixtures;

namespace Patina.Tests;

public class SampleCollectionTests
{
    private static readonly PatinaDocument Doc = SampleCollection.Create(Today);

    [Test]
    public void Ids_are_unique()
    {
        Doc.Artworks.Select(a => a.Id).Should().OnlyHaveUniqueItems();
        Doc.Artworks.Select(a => a.Accession).Should().OnlyHaveUniqueItems();
        Doc.Surveys.Select(s => s.Id).Should().OnlyHaveUniqueItems();
        Doc.Treatments.Select(t => t.Id).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void Every_grade_and_due_state_is_represented()
    {
        var summaries = Doc.Artworks.Select(a => PatinaQueries.Summarize(Doc, a, Today)).ToList();

        summaries.Select(s => s.Grade).Distinct().Should().BeEquivalentTo(Enum.GetValues<ConditionGrade>());
        summaries.Select(s => s.Due).Distinct().Should().BeEquivalentTo(Enum.GetValues<DueState>());
    }

    [Test]
    public void Every_treatment_points_at_a_real_finding()
    {
        foreach (var t in Doc.Treatments)
        {
            PatinaQueries.Treatment(Doc, t.Id, Today)!.Finding.Should().NotBeNull();
        }
    }

    [Test]
    public void Every_treatment_status_is_represented() =>
        Doc.Treatments.Select(t => t.Status).Distinct().Should().BeEquivalentTo(Enum.GetValues<TreatmentStatus>());

    [Test]
    public void Seed_is_relative_to_the_first_run()
    {
        var later = SampleCollection.Create(Today.AddYears(3));
        later.Surveys.Max(s => s.Date).Should().BeAfter(Today.AddYears(2));
    }
}
