using static Patina.Tests.Fixtures;

namespace Patina.Tests;

public class ConditionRulesTests
{
    [Test]
    public void No_findings_is_good() => ConditionRules.Grade([]).Should().Be(ConditionGrade.Good);

    [TestCase(Severity.Minor, ConditionGrade.Good)]
    [TestCase(Severity.Moderate, ConditionGrade.Fair)]
    [TestCase(Severity.Serious, ConditionGrade.Poor)]
    [TestCase(Severity.Urgent, ConditionGrade.Critical)]
    public void Worst_finding_sets_the_grade(Severity worst, ConditionGrade expected) =>
        ConditionRules.Grade([Finding(Severity.Minor), Finding(worst)]).Should().Be(expected);

    [Test]
    public void Three_moderate_findings_escalate_to_poor() =>
        ConditionRules.Grade([Finding(Severity.Moderate), Finding(Severity.Moderate), Finding(Severity.Moderate)])
            .Should().Be(ConditionGrade.Poor);

    [Test]
    public void Two_moderate_findings_stay_fair() =>
        ConditionRules.Grade([Finding(Severity.Moderate), Finding(Severity.Moderate)]).Should().Be(ConditionGrade.Fair);

    [TestCase(ArtMaterial.Mural, ConditionGrade.Good, 6)]
    [TestCase(ArtMaterial.Bronze, ConditionGrade.Fair, 12)]
    [TestCase(ArtMaterial.Stone, ConditionGrade.Good, 24)]
    [TestCase(ArtMaterial.Stone, ConditionGrade.Poor, 12)]
    [TestCase(ArtMaterial.Mural, ConditionGrade.Poor, 3)]
    [TestCase(ArtMaterial.Concrete, ConditionGrade.Critical, 3)]
    public void Interval_depends_on_material_and_grade(ArtMaterial material, ConditionGrade grade, int months) =>
        ConditionRules.IntervalMonths(material, grade).Should().Be(months);

    [Test]
    public void Next_due_counts_from_the_last_survey()
    {
        var survey = Survey("x1", new DateOnly(2026, 1, 15), Finding(Severity.Serious));
        ConditionRules.NextDue(ArtMaterial.Bronze, survey).Should().Be(new DateOnly(2026, 7, 15));
    }

    [Test]
    public void Due_states()
    {
        ConditionRules.Due(null, Today).Should().Be(DueState.NeverSurveyed);
        ConditionRules.Due(Today.AddDays(-1), Today).Should().Be(DueState.Overdue);
        ConditionRules.Due(Today, Today).Should().Be(DueState.DueSoon);
        ConditionRules.Due(Today.AddDays(ConditionRules.DueSoonDays), Today).Should().Be(DueState.DueSoon);
        ConditionRules.Due(Today.AddDays(ConditionRules.DueSoonDays + 1), Today).Should().Be(DueState.Current);
    }

    [TestCase(ConditionGrade.Good, DueState.Current, false)]
    [TestCase(ConditionGrade.Fair, DueState.DueSoon, false)]
    [TestCase(ConditionGrade.Poor, DueState.Current, true)]
    [TestCase(ConditionGrade.Good, DueState.Overdue, true)]
    [TestCase(ConditionGrade.Unsurveyed, DueState.NeverSurveyed, true)]
    public void Needs_attention(ConditionGrade grade, DueState due, bool expected) =>
        ConditionRules.NeedsAttention(grade, due).Should().Be(expected);

    [Test]
    public void Every_material_offers_graffiti_and_vandalism()
    {
        foreach (var material in Enum.GetValues<ArtMaterial>())
        {
            ConditionRules.FindingTypesFor(material).Should().Contain([FindingType.Graffiti, FindingType.Vandalism]);
        }
    }
}
