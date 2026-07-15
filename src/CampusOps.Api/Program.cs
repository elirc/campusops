using CampusOps.Api.Assets;
using CampusOps.Api.Errors;
using CampusOps.Api.Locations;
using CampusOps.Api.Requests;
using CampusOps.Core.Assets;
using CampusOps.Core.Locations;
using CampusOps.Core.Requests;
using CampusOps.Infrastructure.Assets;
using CampusOps.Infrastructure.Locations;
using CampusOps.Infrastructure.Requests;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddSingleton<ILocationStore, InMemoryLocationStore>();
builder.Services.AddSingleton<IAssetStore, InMemoryAssetStore>();
builder.Services.AddSingleton<IMaintenanceRequestStore, InMemoryMaintenanceRequestStore>();
builder.Services.AddScoped<LocationService>();
builder.Services.AddScoped<AssetService>();
builder.Services.AddScoped<MaintenanceRequestService>();

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")) app.MapOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapLocationEndpoints();
app.MapAssetEndpoints();
app.MapMaintenanceRequestEndpoints();
app.Run();

public partial class Program;
