namespace Patina.Tests;

internal sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset Now { get; set; } = now;
}

internal sealed class MemoryDocumentFile : IDocumentFile
{
    public string? Content { get; set; }

    public int Writes { get; private set; }

    public List<string> SetAside { get; } = [];

    public bool FailReads { get; set; }

    public Task<string?> ReadAsync(CancellationToken ct) =>
        FailReads ? throw new IOException("disk unavailable") : Task.FromResult(Content);

    public Task WriteAsync(string json, CancellationToken ct)
    {
        Content = json;
        Writes++;
        return Task.CompletedTask;
    }

    public Task MoveAsideAsync(string reason, CancellationToken ct)
    {
        if (Content is not null)
        {
            SetAside.Add(Content);
        }

        Content = null;
        return Task.CompletedTask;
    }
}

internal static class Fixtures
{
    public static readonly DateOnly Today = new(2026, 10, 3);

    public static readonly DateTimeOffset Now = new(2026, 10, 3, 14, 0, 0, TimeSpan.Zero);

    public static Artwork Artwork(string id = "x1", ArtMaterial material = ArtMaterial.Bronze) =>
        new(id, $"PA-2000-{id}", $"Work {id}", "Artist", 2000, material, "Ville-Marie", "Site", 45.5, -73.6, "Description");

    public static Finding Finding(Severity severity, string zone = "North face", FindingType type = FindingType.Corrosion) =>
        new(Ids.New(), type, zone, severity, string.Empty, []);

    public static Survey Survey(string artworkId, DateOnly date, params Finding[] findings) =>
        new(Ids.New(), artworkId, SurveyStatus.Submitted, date, "C. Lavoie", Weather.Dry, string.Empty, [.. findings], [],
            ConditionRules.Grade(findings));

    public static PatinaDocument Doc(params Artwork[] artworks) => PatinaDocument.Empty with { Artworks = [.. artworks] };
}
