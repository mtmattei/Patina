using static Patina.Tests.Fixtures;

namespace Patina.Tests;

public class StoreTests
{
    private static (PatinaStore Store, MemoryDocumentFile File) Create(string? content = null)
    {
        var file = new MemoryDocumentFile { Content = content };
        return (new PatinaStore(file, new FixedClock(Now)), file);
    }

    [Test]
    public async Task First_run_seeds_and_saves_the_sample_collection()
    {
        var (store, file) = Create();

        var doc = await store.GetAsync();

        doc.Artworks.Should().HaveCount(18);
        file.Writes.Should().Be(1);
        PatinaJson.Deserialize(file.Content!).Artworks.Should().HaveCount(18);
    }

    [Test]
    public async Task Existing_document_is_loaded_without_writing()
    {
        var (store, file) = Create(PatinaJson.Serialize(Doc(Artwork())));

        (await store.GetAsync()).Artworks.Should().ContainSingle();
        file.Writes.Should().Be(0);
    }

    [Test]
    public async Task Corrupt_file_is_set_aside_and_reported()
    {
        var (store, file) = Create("{ not json");

        var act = () => store.GetAsync();

        await act.Should().ThrowAsync<PatinaStoreException>();
        file.SetAside.Should().ContainSingle().Which.Should().Be("{ not json");

        store.Invalidate();
        (await store.GetAsync()).Artworks.Should().HaveCount(18, "a retry starts over from the sample collection");
    }

    [Test]
    public async Task Read_failure_is_reported_and_recoverable()
    {
        var (store, file) = Create(PatinaJson.Serialize(Doc(Artwork())));
        file.FailReads = true;

        await store.Invoking(s => s.GetAsync()).Should().ThrowAsync<PatinaStoreException>();

        file.FailReads = false;
        (await store.GetAsync()).Artworks.Should().ContainSingle();
    }

    [Test]
    public async Task Newer_version_is_refused_without_touching_the_file()
    {
        var (store, file) = Create(PatinaJson.Serialize(Doc(Artwork()) with { Version = 99 }));

        await store.Invoking(s => s.GetAsync()).Should().ThrowAsync<PatinaStoreException>();
        file.SetAside.Should().BeEmpty();
    }

    [Test]
    public async Task Update_persists_and_broadcasts()
    {
        var (store, file) = Create(PatinaJson.Serialize(Doc(Artwork())));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var seen = new List<int>();

        var watcher = Task.Run(async () =>
        {
            await foreach (var doc in store.Watch(cts.Token))
            {
                seen.Add(doc.Surveys.Count);
                if (doc.Surveys.Count == 1)
                {
                    break;
                }
            }
        });

        await Task.Delay(50);
        var problem = await store.UpdateAsync(d => SurveyOps.SaveDraft(d, SurveyOps.NewDraft("x1", "a", Today)));
        await watcher;

        problem.Should().Be(Problem.None);
        seen.Should().Equal(0, 1);
        PatinaJson.Deserialize(file.Content!).Surveys.Should().ContainSingle();
    }

    [Test]
    public async Task Refused_update_changes_nothing()
    {
        var (store, file) = Create(PatinaJson.Serialize(Doc(Artwork())));

        var problem = await store.UpdateAsync(d => SurveyOps.Submit(d, SurveyOps.NewDraft("x1", "", Today), Now));

        problem.Should().Be(Problem.SurveyorRequired);
        file.Writes.Should().Be(0);
        (await store.GetAsync()).Surveys.Should().BeEmpty();
    }

    [Test]
    public async Task Reset_restores_the_sample_collection()
    {
        var (store, _) = Create(PatinaJson.Serialize(Doc(Artwork())));

        await store.ResetToSampleAsync();

        (await store.GetAsync()).Artworks.Should().HaveCount(18);
    }
}
