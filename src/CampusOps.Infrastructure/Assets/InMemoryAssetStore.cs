using System.Collections.Concurrent;
using CampusOps.Core.Assets;

namespace CampusOps.Infrastructure.Assets;

public sealed class InMemoryAssetStore : IAssetStore
{
    private readonly ConcurrentDictionary<Guid, Asset> _items = new();
    public Task AddAsync(Asset asset, CancellationToken token) { token.ThrowIfCancellationRequested(); _items[asset.Id] = asset; return Task.CompletedTask; }
    public Task<Asset?> GetAsync(Guid id, CancellationToken token) { token.ThrowIfCancellationRequested(); _items.TryGetValue(id, out var item); return Task.FromResult(item); }
    public Task<IReadOnlyCollection<Asset>> ListAsync(CancellationToken token) => Task.FromResult<IReadOnlyCollection<Asset>>(_items.Values.OrderBy(x => x.AssetTag).ToArray());
}
