using CampusOps.Api.Errors;
using CampusOps.Api.Locations;
using CampusOps.Core.Locations;
using CampusOps.Infrastructure.Locations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddSingleton<ILocationStore, InMemoryLocationStore>();
builder.Services.AddScoped<LocationService>();

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")) app.MapOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapLocationEndpoints();
app.Run();

public partial class Program;
