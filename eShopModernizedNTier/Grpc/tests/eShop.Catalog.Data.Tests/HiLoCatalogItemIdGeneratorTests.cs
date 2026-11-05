using eShop.Catalog.Data.Sequences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eShop.Catalog.Data.Tests;

/// <summary>
/// Id allocation parity with the legacy <c>CatalogItemHiLoGenerator</c>: one sequence round trip
/// per block of ten, ids handed out from memory in between, and blocks never handed out twice.
/// </summary>
public sealed class HiLoCatalogItemIdGeneratorTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public HiLoCatalogItemIdGeneratorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public void AllocatesSequentialIdsStartingAtOne()
    {
        using var context = CreateContext();
        var generator = CreateGenerator();

        var ids = Enumerable.Range(0, 25).Select(_ => generator.GetNextId(context)).ToArray();

        Assert.Equal(Enumerable.Range(1, 25), ids);
    }

    [Fact]
    public void TakesOneSequenceValuePerBlockOfTen()
    {
        using var context = CreateContext();
        var sequences = new CountingSequenceProvider(new SqliteCatalogSequenceProvider());
        var generator = new HiLoCatalogItemIdGenerator(sequences);

        for (var i = 0; i < CatalogSequences.Increment * 3; i++)
        {
            generator.GetNextId(context);
        }

        Assert.Equal(3, sequences.Calls);
    }

    [Fact]
    public void NewGeneratorSkipsTheUnusedTailOfTheAbandonedBlock()
    {
        using var context = CreateContext();
        var sequences = new SqliteCatalogSequenceProvider();

        var first = new HiLoCatalogItemIdGenerator(sequences);
        var beforeRestart = new[] { first.GetNextId(context), first.GetNextId(context) };

        var second = new HiLoCatalogItemIdGenerator(sequences);
        var afterRestart = second.GetNextId(context);

        Assert.Equal([1, 2], beforeRestart);
        Assert.Equal(1 + CatalogSequences.Increment, afterRestart);
    }

    [Fact]
    public void ConcurrentCallersNeverShareAnId()
    {
        using var context = CreateContext();
        var generator = CreateGenerator();
        const int callers = 8;
        const int idsPerCaller = 50;

        var ids = new int[callers][];
        Parallel.For(0, callers, caller =>
            ids[caller] = Enumerable.Range(0, idsPerCaller).Select(_ => generator.GetNextId(context)).ToArray());

        var allocated = ids.SelectMany(x => x).ToArray();

        Assert.Equal(callers * idsPerCaller, allocated.Distinct().Count());
        Assert.Equal(Enumerable.Range(1, callers * idsPerCaller), allocated.Order());
    }

    private CatalogDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);

    private static HiLoCatalogItemIdGenerator CreateGenerator() =>
        new(new SqliteCatalogSequenceProvider());

    private sealed class CountingSequenceProvider : ICatalogSequenceProvider
    {
        private readonly ICatalogSequenceProvider _inner;

        public CountingSequenceProvider(ICatalogSequenceProvider inner) => _inner = inner;

        public int Calls { get; private set; }

        public long GetNextSequenceValue(CatalogDbContext db, string sequenceName)
        {
            Calls++;
            return _inner.GetNextSequenceValue(db, sequenceName);
        }
    }
}
