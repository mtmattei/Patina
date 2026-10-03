using static Patina.Tests.Fixtures;

namespace Patina.Tests;

public class JsonTests
{
    [Test]
    public void Document_round_trips()
    {
        var doc = SampleCollection.Create(Today);
        var draft = SurveyOps.NewDraft("a04", "x", Today) with { Findings = [Finding(Severity.Moderate)], PhotoIds = ["p1"] };
        doc = SurveyOps.SaveDraft(doc, draft).Value! with { Photos = [new PhotoRef("p1", ".jpg", "IMG_1.jpg", Now)] };

        var back = PatinaJson.Deserialize(PatinaJson.Serialize(doc));

        back.Artworks.Should().BeEquivalentTo(doc.Artworks);
        back.Surveys.Should().BeEquivalentTo(doc.Surveys);
        back.Treatments.Should().BeEquivalentTo(doc.Treatments);
        back.Photos.Should().BeEquivalentTo(doc.Photos);
    }

    [Test]
    public void Enums_are_written_as_names()
    {
        var json = PatinaJson.Serialize(SampleCollection.Create(Today));
        json.Should().Contain("\"material\": \"Bronze\"").And.Contain("\"status\": \"Submitted\"");
    }

    [Test]
    public void Missing_lists_become_empty()
    {
        var doc = PatinaJson.Deserialize("""{ "version": 1, "artworks": [] }""");

        doc.Surveys.Should().BeEmpty();
        doc.Treatments.Should().BeEmpty();
        doc.Photos.Should().BeEmpty();
    }

    [Test]
    public void Export_then_import()
    {
        var doc = SampleCollection.Create(Today);

        var result = PatinaJson.Import(PatinaJson.Export(doc, Now));

        result.IsOk.Should().BeTrue();
        result.Value!.Artworks.Should().BeEquivalentTo(doc.Artworks);
    }

    [TestCase("not json")]
    [TestCase("""{ "name": "something else" }""")]
    [TestCase("""{ "format": "other", "exportedAt": "2026-01-01T00:00:00Z", "document": { "version": 1 } }""")]
    public void Import_refuses_files_that_are_not_patina_exports(string json) =>
        PatinaJson.Import(json).Problem.Should().Be(Problem.ImportNotPatinaFile);

    [Test]
    public void Import_refuses_a_newer_version()
    {
        var json = PatinaJson.Export(PatinaDocument.Empty with { Version = PatinaDocument.CurrentVersion + 1 }, Now);
        PatinaJson.Import(json).Problem.Should().Be(Problem.ImportNewerVersion);
    }
}
