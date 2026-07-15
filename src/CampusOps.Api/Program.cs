using CampusOps.Api.Assets;
using CampusOps.Api.Errors;
using CampusOps.Api.Locations;
using CampusOps.Core.Assets;
using CampusOps.Core.Locations;
using CampusOps.Infrastructure.Assets;
using CampusOps.Infrastructure.Locations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddSingleton<ILocationStore, InMemoryLocationStore>();
builder.Services.AddSingleton<IAssetStore, InMemoryAssetStore>();
builder.Services.AddScoped<LocationService>();
builder.Services.AddScoped<AssetService>();

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")) app.MapOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapLocationEndpoints();
app.MapAssetEndpoints();
app.Run();

public partial class Program;
