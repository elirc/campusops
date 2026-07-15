using CampusOps.Core.Inventory;
using CampusOps.Infrastructure.Inventory;
namespace CampusOps.Infrastructure.Tests.Inventory;

public sealed class ConcurrentReservationTests
{
    [Fact] public async Task CompetingReservations_NeverExceedStock() { var store = new InMemoryPartStore(); var part = new Part(Guid.NewGuid(), "FLT-9", "Filter", 5); await store.AddAsync(part, default); var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => store.TryReserveAsync(part.Id, 1, default))); Assert.Equal(5, results.Count(x => x is not null)); Assert.Equal(0, part.Available); }
}
