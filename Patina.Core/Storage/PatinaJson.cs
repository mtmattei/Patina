using System.Text.Json;
using System.Text.Json.Serialization;
using Patina.Core.Model;
using Patina.Core.Rules;

namespace Patina.Core.Storage;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(PatinaDocument))]
[JsonSerializable(typeof(ExportEnvelope))]
internal sealed partial class PatinaJsonContext : JsonSerializerContext;

/// <summary>The export file format: the document plus a marker so a random JSON file is never imported by mistake.</summary>
public sealed record ExportEnvelope(string Format, DateTimeOffset ExportedAt, PatinaDocument Document)
{
    public const string FormatName = "patina.collection";
}

public static class PatinaJson
{
    public static string Serialize(PatinaDocument doc) => JsonSerializer.Serialize(doc, PatinaJsonContext.Default.PatinaDocument);

    /// <exception cref="JsonException">The text is not a valid Patina document.</exception>
    public static PatinaDocument Deserialize(string json)
    {
        var doc = JsonSerializer.Deserialize(json, PatinaJsonContext.Default.PatinaDocument)
            ?? throw new JsonException("The file is empty.");
        return Normalize(doc);
    }

    public static string Export(PatinaDocument doc, DateTimeOffset now) =>
        JsonSerializer.Serialize(new ExportEnvelope(ExportEnvelope.FormatName, now, doc), PatinaJsonContext.Default.ExportEnvelope);

    /// <summary>Reads an export file. Refuses anything that is not a Patina export, or one written by a newer version.</summary>
    public static Result<PatinaDocument> Import(string json)
    {
        ExportEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize(json, PatinaJsonContext.Default.ExportEnvelope);
        }
        catch (JsonException)
        {
            return Result<PatinaDocument>.Fail(Problem.ImportNotPatinaFile);
        }

        if (envelope is null || envelope.Format != ExportEnvelope.FormatName || envelope.Document is null)
        {
            return Result<PatinaDocument>.Fail(Problem.ImportNotPatinaFile);
        }

        if (envelope.Document.Version > PatinaDocument.CurrentVersion)
        {
            return Result<PatinaDocument>.Fail(Problem.ImportNewerVersion);
        }

        return Result<PatinaDocument>.Ok(Normalize(envelope.Document));
    }

    /// <summary>Missing collections in older or hand-edited files become empty lists instead of nulls.</summary>
    private static PatinaDocument Normalize(PatinaDocument doc) => doc with
    {
        Artworks = doc.Artworks ?? [],
        Surveys = (doc.Surveys ?? []).Select(s => s with
        {
            Findings = (s.Findings ?? []).Select(f => f with { PhotoIds = f.PhotoIds ?? [] }).ToImmutableListSafe(),
            PhotoIds = s.PhotoIds ?? [],
            Notes = s.Notes ?? string.Empty,
        }).ToImmutableListSafe(),
        Treatments = (doc.Treatments ?? []).Select(t => t with { Log = t.Log ?? [], Assignee = t.Assignee ?? string.Empty }).ToImmutableListSafe(),
        Photos = doc.Photos ?? [],
    };

    private static System.Collections.Immutable.ImmutableList<T> ToImmutableListSafe<T>(this IEnumerable<T> items) =>
        System.Collections.Immutable.ImmutableList.CreateRange(items);
}
