using CampusOps.Core.Common;
using CampusOps.Core.Inventory;
using CampusOps.Infrastructure.Inventory;

namespace CampusOps.Infrastructure.Tests.Inventory;

public sealed class PartStoreIsolationTests
{
    [Fact]
    public async Task CallerCannotMutateStoredStockThroughOriginalObject()
    {
        var store = new InMemoryPartStore();
        var part = new Part(Guid.NewGuid(), "F-1", "Filter", 5);
        await store.AddAsync(part, default);
        part.Reserve(3);
        Assert.Equal(5, (await store.GetAsync(part.Id, default))!.Available);
    }

    [Fact]
    public async Task ReadsAreDetachedFromStoredState()
    {
        var store = new InMemoryPartStore();
        var part = new Part(Guid.NewGuid(), "F-1", "Filter", 5);
        await store.AddAsync(part, default);
        (await store.GetAsync(part.Id, default))!.Reserve(2);
        Assert.Single(await store.ListAsync(default)).Reserve(1);
        Assert.Equal(5, (await store.GetAsync(part.Id, default))!.Available);
    }

    [Fact]
    public async Task DuplicateIdCannotReplaceStock()
    {
        var store = new InMemoryPartStore();
        var id = Guid.NewGuid();
        await store.AddAsync(new Part(id, "F-1", "Original", 5), default);
        var error = await Assert.ThrowsAsync<DomainException>(() => store.AddAsync(new Part(id, "F-2", "Replacement", 100), default));
        Assert.Equal("part.already_exists", error.Code);
        Assert.Equal(5, (await store.GetAsync(id, default))!.OnHand);
    }

    [Fact]
    public async Task CancelledAddDoesNotWrite()
    {
        var store = new InMemoryPartStore();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.AddAsync(new Part(Guid.NewGuid(), "F-1", "Filter", 5), cancellation.Token));
        Assert.Empty(await store.ListAsync(default));
    }
}
