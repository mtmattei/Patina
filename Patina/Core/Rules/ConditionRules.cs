using Patina.Core.Model;

namespace Patina.Core.Rules;

/// <summary>How condition is graded and when an artwork is due for its next survey.</summary>
public static class ConditionRules
{
    public const int DueSoonDays = 30;

    /// <summary>
    /// The worst finding sets the grade. Three or more moderate findings escalate Fair to Poor:
    /// several moderate problems on one object need a treatment plan as much as one serious one.
    /// </summary>
    public static ConditionGrade Grade(IEnumerable<Finding> findings)
    {
        var list = findings.ToList();
        if (list.Count == 0)
        {
            return ConditionGrade.Good;
        }

        var worst = list.Max(f => f.Severity);
        var grade = worst switch
        {
            Severity.Minor => ConditionGrade.Good,
            Severity.Moderate => ConditionGrade.Fair,
            Severity.Serious => ConditionGrade.Poor,
            _ => ConditionGrade.Critical,
        };

        if (grade == ConditionGrade.Fair && list.Count(f => f.Severity == Severity.Moderate) >= 3)
        {
            grade = ConditionGrade.Poor;
        }

        return grade;
    }

    /// <summary>Base survey interval in months for each material, before the grade adjusts it.</summary>
    public static int BaseIntervalMonths(ArtMaterial material) => material switch
    {
        ArtMaterial.Mural or ArtMaterial.Wood => 6,
        ArtMaterial.Bronze or ArtMaterial.Steel or ArtMaterial.Mosaic => 12,
        _ => 24,
    };

    /// <summary>Poor halves the interval; Critical caps it at three months.</summary>
    public static int IntervalMonths(ArtMaterial material, ConditionGrade grade)
    {
        var months = BaseIntervalMonths(material);
        return grade switch
        {
            ConditionGrade.Critical => Math.Min(months, 3),
            ConditionGrade.Poor => Math.Max(months / 2, 3),
            _ => months,
        };
    }

    public static DateOnly? NextDue(ArtMaterial material, Survey? lastSubmitted) =>
        lastSubmitted is null
            ? null
            : lastSubmitted.Date.AddMonths(IntervalMonths(material, lastSubmitted.Grade));

    public static DueState Due(DateOnly? nextDue, DateOnly today)
    {
        if (nextDue is not { } due)
        {
            return DueState.NeverSurveyed;
        }

        if (due < today)
        {
            return DueState.Overdue;
        }

        return due.DayNumber - today.DayNumber <= DueSoonDays ? DueState.DueSoon : DueState.Current;
    }

    public static bool NeedsAttention(ConditionGrade grade, DueState due) =>
        grade >= ConditionGrade.Poor || due is DueState.Overdue or DueState.NeverSurveyed;

    /// <summary>Finding types a surveyor can record for a material, most likely first.</summary>
    public static IReadOnlyList<FindingType> FindingTypesFor(ArtMaterial material) => material switch
    {
        ArtMaterial.Bronze or ArtMaterial.Steel =>
        [
            FindingType.Corrosion, FindingType.CoatingFailure, FindingType.Graffiti, FindingType.StructuralMovement,
            FindingType.MaterialLoss, FindingType.Soiling, FindingType.Vandalism,
        ],
        ArtMaterial.Stone or ArtMaterial.Concrete =>
        [
            FindingType.Cracking, FindingType.BiologicalGrowth, FindingType.Efflorescence, FindingType.Graffiti,
            FindingType.MaterialLoss, FindingType.Soiling, FindingType.StructuralMovement, FindingType.Vandalism,
        ],
        ArtMaterial.Mosaic =>
        [
            FindingType.MaterialLoss, FindingType.Cracking, FindingType.Graffiti, FindingType.Efflorescence,
            FindingType.Soiling, FindingType.Vandalism,
        ],
        ArtMaterial.Mural =>
        [
            FindingType.Flaking, FindingType.Fading, FindingType.Graffiti, FindingType.Efflorescence,
            FindingType.BiologicalGrowth, FindingType.Soiling, FindingType.Vandalism,
        ],
        _ =>
        [
            FindingType.BiologicalGrowth, FindingType.Cracking, FindingType.CoatingFailure, FindingType.MaterialLoss,
            FindingType.StructuralMovement, FindingType.Graffiti, FindingType.Vandalism,
        ],
    };
}
