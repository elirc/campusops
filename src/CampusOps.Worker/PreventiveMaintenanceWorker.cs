using CampusOps.Core.PreventiveMaintenance;
namespace CampusOps.Worker;

public sealed class PreventiveMaintenanceWorker(PreventiveMaintenanceService service, ILogger<PreventiveMaintenanceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) { using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1)); while (await timer.WaitForNextTickAsync(stoppingToken)) { var count = await service.GenerateDueAsync(stoppingToken); if (count > 0) logger.LogInformation("Generated {Count} preventive maintenance requests", count); } }
}
