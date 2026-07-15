using System.Text;
using CampusOps.Core.Assets;
using CampusOps.Core.Inventory;
using CampusOps.Core.WorkOrders;
namespace CampusOps.Core.Reporting;

public sealed record OperationsSummary(int Assets, int OpenWorkOrders, int BlockedWorkOrders, int LowStockParts, IReadOnlyDictionary<string, int> WorkByPriority);
public sealed class OperationsReportService(IAssetStore assets, IWorkOrderStore workOrders, IPartStore parts)
{
    public async Task<OperationsSummary> SummarizeAsync(CancellationToken token) { var assetItems = await assets.ListAsync(token); var work = await workOrders.ListAsync(token); var partItems = await parts.ListAsync(token); return new(assetItems.Count, work.Count(x => x.Status is not (WorkOrderStatus.Completed or WorkOrderStatus.Cancelled)), work.Count(x => x.Status == WorkOrderStatus.Blocked), partItems.Count(x => x.Available <= 2), work.GroupBy(x => x.Priority.ToString()).ToDictionary(x => x.Key, x => x.Count())); }
    public async Task<string> ExportWorkCsvAsync(CancellationToken token) { var csv = new StringBuilder("Id,Title,Priority,Status,Version\n"); foreach (var item in await workOrders.ListAsync(token)) csv.AppendJoin(',', item.Id, Escape(item.Title), item.Priority, item.Status, item.Version).Append('\n'); return csv.ToString(); }
    private static string Escape(string value) => value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
