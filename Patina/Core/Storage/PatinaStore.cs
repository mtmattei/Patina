using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Patina.Core.Model;
using Patina.Core.Rules;

namespace Patina.Core.Storage;

/// <summary>Where the document lives. The app implements it over <c>ApplicationData.LocalFolder</c>; tests use memory.</summary>
public interface IDocumentFile
{
    /// <summary>Returns null when no document has been saved yet.</summary>
    Task<string?> ReadAsync(CancellationToken ct);

    /// <summary>Must replace the document atomically: a crash mid-write leaves the previous version readable.</summary>
    Task WriteAsync(string json, CancellationToken ct);

    /// <summary>Keeps an unreadable file aside so nothing is lost when the app starts over.</summary>
    Task MoveAsideAsync(string reason, CancellationToken ct);
}

public sealed class PatinaStoreException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Single owner of the document. Every write goes through <see cref="UpdateAsync"/>, which is serialised,
/// persisted, and then broadcast to every <see cref="Watch"/> subscriber.
/// </summary>
public sealed class PatinaStore(IDocumentFile file, IClock clock)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<Channel<PatinaDocument>> _subscribers = [];
    private readonly Lock _subscribersLock = new();
    private PatinaDocument? _current;

    public IClock Clock => clock;

    public async Task<PatinaDocument> GetAsync(CancellationToken ct = default)
    {
        if (_current is { } loaded)
        {
            return loaded;
        }

        await _gate.WaitAsync(ct);
        try
        {
            return _current ??= await LoadAsync(ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<PatinaDocument> LoadAsync(CancellationToken ct)
    {
        string? json;
        try
        {
            json = await file.ReadAsync(ct);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new PatinaStoreException("The collection file could not be read.", ex);
        }

        if (json is null)
        {
            var seeded = SampleCollection.Create(clock.Today);
            await file.WriteAsync(PatinaJson.Serialize(seeded), ct);
            return seeded;
        }

        try
        {
            var doc = PatinaJson.Deserialize(json);
            if (doc.Version > PatinaDocument.CurrentVersion)
            {
                throw new PatinaStoreException("The collection was saved by a newer version of Patina.");
            }

            return doc;
        }
        catch (JsonException ex)
        {
            await file.MoveAsideAsync("corrupt", ct);
            throw new PatinaStoreException("The collection file was damaged and has been set aside.", ex);
        }
    }

    /// <summary>The current document, then every committed change. Cold: each enumeration starts with a fresh read.</summary>
    public async IAsyncEnumerable<PatinaDocument> Watch([EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateUnbounded<PatinaDocument>(new UnboundedChannelOptions { SingleReader = true });
        lock (_subscribersLock)
        {
            _subscribers.Add(channel);
        }

        try
        {
            yield return await GetAsync(ct);

            while (await channel.Reader.WaitToReadAsync(ct))
            {
                PatinaDocument? latest = null;
                while (channel.Reader.TryRead(out var doc))
                {
                    latest = doc;
                }

                if (latest is not null)
                {
                    yield return latest;
                }
            }
        }
        finally
        {
            lock (_subscribersLock)
            {
                _subscribers.Remove(channel);
            }
        }
    }

    /// <summary>Applies an operation and persists it. A refused operation changes nothing and returns its problem.</summary>
    public async Task<Problem> UpdateAsync(Func<PatinaDocument, Result<PatinaDocument>> operation, CancellationToken ct = default)
    {
        await GetAsync(ct);
        await _gate.WaitAsync(ct);
        PatinaDocument updated;
        try
        {
            var result = operation(_current!);
            if (!result.IsOk)
            {
                return result.Problem;
            }

            updated = result.Value!;
            await file.WriteAsync(PatinaJson.Serialize(updated), ct);
            _current = updated;
        }
        finally
        {
            _gate.Release();
        }

        Broadcast(updated);
        return Problem.None;
    }

    public Task<Problem> ReplaceAsync(PatinaDocument doc, CancellationToken ct = default) =>
        UpdateAsync(_ => Result<PatinaDocument>.Ok(doc), ct);

    public Task<Problem> ResetToSampleAsync(CancellationToken ct = default) =>
        UpdateAsync(_ => Result<PatinaDocument>.Ok(SampleCollection.Create(clock.Today)), ct);

    /// <summary>Forgets the cached document so the next read goes back to the file (used by Retry after a load error).</summary>
    public void Invalidate() => _current = null;

    private void Broadcast(PatinaDocument doc)
    {
        lock (_subscribersLock)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber.Writer.TryWrite(doc);
            }
        }
    }
}
