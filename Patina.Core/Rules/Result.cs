namespace Patina.Core.Rules;

/// <summary>Why an operation was refused. The app maps each code to a localized message.</summary>
public enum Problem
{
    None,
    SurveyorRequired,
    FindingZoneRequired,
    SurveyNotFound,
    SurveyAlreadySubmitted,
    ArtworkNotFound,
    TreatmentNotFound,
    ScheduleDateRequired,
    CompletionNoteRequired,
    TreatmentAlreadyDone,
    NoEarlierStatus,
    ImportNotPatinaFile,
    ImportNewerVersion,
}

public readonly record struct Result<T>(T? Value, Problem Problem)
{
    public bool IsOk => Problem == Problem.None;

    public static Result<T> Ok(T value) => new(value, Problem.None);

    public static Result<T> Fail(Problem problem) => new(default, problem);
}
