using CampusOps.Core.Assets;
using CampusOps.Core.Inventory;
using CampusOps.Core.Reporting;
using CampusOps.Core.Requests;
using CampusOps.Core.WorkOrders;
using CampusOps.Infrastructure.Assets;
using CampusOps.Infrastructure.Inventory;
using CampusOps.Infrastructure.WorkOrders;
namespace CampusOps.Infrastructure.Tests.Reporting;

public sealed class OperationsReportTests
{
    [Fact] public async Task Summary_CombinesModuleReadModels() { var assets = new InMemoryAssetStore(); var work = new InMemoryWorkOrderStore(); var parts = new InMemoryPartStore(); await assets.AddAsync(new Asset(Guid.NewGuid(), "A-1", "Pump", "Plumbing", Guid.NewGuid()), default); var order = new WorkOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, RequestPriority.High, "Repair, urgent", DateTimeOffset.UtcNow); await work.AddAsync(order, default); await parts.AddAsync(new Part(Guid.NewGuid(), "P-1", "Seal", 2), default); var report = new OperationsReportService(assets, work, parts); var summary = await report.SummarizeAsync(default); Assert.Equal(1, summary.Assets); Assert.Equal(1, summary.OpenWorkOrders); Assert.Equal(1, summary.LowStockParts); Assert.Contains("\"Repair, urgent\"", await report.ExportWorkCsvAsync(default)); }
}
