namespace Patina.Core;

public interface IClock
{
    DateTimeOffset Now { get; }

    DateOnly Today => DateOnly.FromDateTime(Now.LocalDateTime);
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

public static class Ids
{
    public static string New() => Guid.NewGuid().ToString("N")[..12];
}
