using CampusOps.Core.Common;
using CampusOps.Core.Inventory;
using CampusOps.Infrastructure.Inventory;

namespace CampusOps.Infrastructure.Tests.Inventory;

public sealed class ConcurrentReservationTests
{
    [Fact]
    public async Task CompetingReservations_CommitMatchingStockAndHistory()
    {
        var store = new InMemoryPartStore();
        var part = new Part(Guid.NewGuid(), "FLT-9", "Filter", 5);
        var workOrderId = Guid.NewGuid();
        var at = DateTimeOffset.Parse("2026-01-02T03:04:05Z");
        await store.AddAsync(part, default);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 64).Select(_ => Task.Run(async () =>
        {
            await start.Task;
            return await store.ReserveForWorkOrderAsync(workOrderId, part.Id, 1, at, default);
        })).ToArray();
        start.SetResult();
        var results = await Task.WhenAll(tasks);
        var stored = await store.GetAsync(part.Id, default);
        var history = await store.ListForWorkOrderAsync(workOrderId, default);
        Assert.Equal(5, results.Count(x => x is not null));
        Assert.Equal(5, history.Count);
        Assert.Equal(5, history.Select(x => x.Id).Distinct().Count());
        Assert.Equal(history.Sum(x => x.Quantity), stored!.Reserved);
        Assert.Equal(0, stored.Available);
        Assert.All(history, item => Assert.Equal(at, item.ReservedAt));
        Assert.Equal(5, part.Available); // the caller's object is not the stored aggregate
    }

    [Fact]
    public async Task CancelledReservationChangesNeitherStockNorHistory()
    {
        var store = new InMemoryPartStore();
        var part = new Part(Guid.NewGuid(), "F", "Filter", 5);
        var order = Guid.NewGuid();
        await store.AddAsync(part, default);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.ReserveForWorkOrderAsync(order, part.Id, 2, DateTimeOffset.UtcNow, cancellation.Token));
        Assert.Equal(5, (await store.GetAsync(part.Id, default))!.Available);
        Assert.Empty(await store.ListForWorkOrderAsync(order, default));
    }

    [Fact]
    public async Task InsufficientStockDoesNotAppendHistory()
    {
        var store = new InMemoryPartStore();
        var part = new Part(Guid.NewGuid(), "F", "Filter", 1);
        var order = Guid.NewGuid();
        await store.AddAsync(part, default);
        Assert.Null(await store.ReserveForWorkOrderAsync(order, part.Id, 2, DateTimeOffset.UtcNow, default));
        Assert.Equal(1, (await store.GetAsync(part.Id, default))!.Available);
        Assert.Empty(await store.ListForWorkOrderAsync(order, default));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidQuantityDoesNotMutate(int quantity)
    {
        var store = new InMemoryPartStore();
        var part = new Part(Guid.NewGuid(), "F", "Filter", 5);
        var order = Guid.NewGuid();
        await store.AddAsync(part, default);
        var error = await Assert.ThrowsAsync<DomainException>(() => store.ReserveForWorkOrderAsync(order, part.Id, quantity, DateTimeOffset.UtcNow, default));
        Assert.Equal("part.quantity_invalid", error.Code);
        Assert.Equal(5, (await store.GetAsync(part.Id, default))!.Available);
        Assert.Empty(await store.ListForWorkOrderAsync(order, default));
    }

    [Fact]
    public async Task HistoryIsFilteredToRequestedWorkOrder()
    {
        var store = new InMemoryPartStore();
        var part = new Part(Guid.NewGuid(), "F", "Filter", 5);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        await store.AddAsync(part, default);
        await store.ReserveForWorkOrderAsync(first, part.Id, 1, DateTimeOffset.UtcNow, default);
        await store.ReserveForWorkOrderAsync(second, part.Id, 2, DateTimeOffset.UtcNow, default);
        Assert.Equal(1, Assert.Single(await store.ListForWorkOrderAsync(first, default)).Quantity);
        Assert.Equal(2, Assert.Single(await store.ListForWorkOrderAsync(second, default)).Quantity);
    }
}
