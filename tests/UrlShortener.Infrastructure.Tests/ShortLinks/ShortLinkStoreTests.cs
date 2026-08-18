using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Domain.ShortLinks;
using UrlShortener.Infrastructure.ShortLinks;

namespace UrlShortener.Infrastructure.Tests.ShortLinks;

/// <summary>
/// Review finding C6/TST-005 — AC #18.2's duplicate-key clause had no test that could fail.
/// The endpoint-level concurrency test fires 8 requests at a 62^7 code space, so it passes
/// with the retry loop deleted; the unit test forces a collision against a Dictionary,
/// which is not the component that decides one has happened.
///
/// These run against real SQLite and a real unique constraint, so they are also the
/// regression tests for C3/COR-002 — the narrowed catch that must return false for a
/// uniqueness violation and rethrow everything else.
/// </summary>
public class ShortLinkStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly List<ShortLinkDbContext> _contexts = [];

    public ShortLinkStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        NewContext().Database.EnsureCreated();
    }

    public void Dispose()
    {
        foreach (var context in _contexts) context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private ShortLinkDbContext NewContext()
    {
        var context = new ShortLinkDbContext(
            new DbContextOptionsBuilder<ShortLinkDbContext>().UseSqlite(_connection).Options);
        _contexts.Add(context);
        return context;
    }

    /// <summary>
    /// One repository per context, because that is the production lifetime — Program.cs
    /// registers both as scoped, so two concurrent creates never share a change tracker.
    ///
    /// This distinction is load-bearing. A first draft of these tests reused one context
    /// and failed with InvalidOperationException from the change tracker, never reaching
    /// the database: EF refuses to track two entities with the same key, so the duplicate
    /// was rejected in memory before the constraint could speak. That is not the race the
    /// retry loop exists for, and testing it would have proved nothing about the catch.
    /// </summary>
    private EfShortLinkRepository NewRepository() => new(NewContext());

    private static ShortLink Link(string code, string destination = "https://example.com/x") =>
        new(code, destination, DateTimeOffset.UtcNow);

    [Fact]
    public async Task A_duplicate_code_returns_false_rather_than_throwing()
    {
        Assert.True(await NewRepository().TryAddAsync(Link("TAKEN01"), CancellationToken.None));

        var second = await NewRepository().TryAddAsync(
            Link("TAKEN01", "https://example.com/other"), CancellationToken.None);

        Assert.False(second);
    }

    /// <summary>
    /// The assertion that kills the missing-Detach mutation. If the failed entity is left
    /// tracked, the next SaveChanges re-submits the duplicate and every later attempt
    /// fails too — turning one collision into a permanent failure for that request.
    /// </summary>
    [Fact]
    public async Task A_fresh_code_still_succeeds_after_a_collision()
    {
        await NewRepository().TryAddAsync(Link("TAKEN01"), CancellationToken.None);

        // The collision and the following success share one repository, which is the
        // retry loop's actual shape: one request, one scoped context, a fresh code.
        var retrying = NewRepository();
        Assert.False(await retrying.TryAddAsync(Link("TAKEN01"), CancellationToken.None));

        var third = await retrying.TryAddAsync(Link("FREE001"), CancellationToken.None);

        Assert.True(third);
        Assert.NotNull(await retrying.FindAsync("FREE001", CancellationToken.None));
    }

    [Fact]
    public async Task The_first_writer_of_a_code_keeps_it()
    {
        await NewRepository().TryAddAsync(
            Link("TAKEN01", "https://example.com/first"), CancellationToken.None);
        await NewRepository().TryAddAsync(
            Link("TAKEN01", "https://example.com/second"), CancellationToken.None);

        var stored = await NewRepository().FindAsync("TAKEN01", CancellationToken.None);

        Assert.Equal("https://example.com/first", stored!.Destination);
    }

    /// <summary>
    /// C3/COR-002. A persistence fault that is not a uniqueness violation must propagate,
    /// not be reported as "this code is taken" — which would send the caller into five
    /// retries and then tell the operator a 62^7 code space was exhausted.
    ///
    /// The fault here is a NOT NULL violation, produced by writing through a second context
    /// whose model omits the destination column's requiredness.
    /// </summary>
    [Fact]
    public async Task A_non_uniqueness_persistence_fault_is_not_reported_as_a_collision()
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = "DROP TABLE ShortLinks; CREATE TABLE ShortLinks (Code TEXT NOT NULL CONSTRAINT PK_ShortLinks PRIMARY KEY, Destination TEXT NOT NULL, CreatedAt TEXT NOT NULL, Extra TEXT NOT NULL);";
        await command.ExecuteNonQueryAsync();

        var repository = NewRepository();

        // The insert omits the new non-nullable Extra column: SQLITE_CONSTRAINT_NOTNULL,
        // which is error 19 extended 1299 — a constraint failure, but not a uniqueness one.
        await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.TryAddAsync(Link("ANYCODE"), CancellationToken.None));
    }
}
