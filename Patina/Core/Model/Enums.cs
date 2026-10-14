namespace Patina.Core.Model;

public enum ArtMaterial
{
    Bronze,
    Steel,
    Stone,
    Concrete,
    Mosaic,
    Mural,
    Wood,
}

/// <summary>Overall condition of an artwork. The numeric value is the number of filled patches on the condition strip.</summary>
public enum ConditionGrade
{
    Unsurveyed = 0,
    Good = 1,
    Fair = 2,
    Poor = 3,
    Critical = 4,
}

public enum FindingType
{
    Corrosion,
    Graffiti,
    Cracking,
    MaterialLoss,
    BiologicalGrowth,
    Flaking,
    Fading,
    Efflorescence,
    CoatingFailure,
    StructuralMovement,
    Soiling,
    Vandalism,
}

public enum Severity
{
    Minor = 1,
    Moderate = 2,
    Serious = 3,
    Urgent = 4,
}

public enum SurveyStatus
{
    Draft,
    Submitted,
}

public enum Weather
{
    Dry,
    Overcast,
    Wet,
    Freezing,
    Hot,
}

public enum TreatmentStatus
{
    Proposed,
    Scheduled,
    InProgress,
    Done,
}

public enum TreatmentPriority
{
    Routine,
    High,
    Urgent,
}

public enum DueState
{
    NeverSurveyed,
    Overdue,
    DueSoon,
    Current,
}

public enum CollectionFilter
{
    All,
    NeedsAttention,
    Overdue,
    Unsurveyed,
}

public enum QueueFilter
{
    Open,
    Proposed,
    Scheduled,
    InProgress,
    Done,
}

public enum LogKind
{
    Note,
    Proposed,
    StatusChanged,
    StatusReturned,
}
