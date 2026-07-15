using CampusOps.Contracts.Locations;
using CampusOps.Core.Locations;

namespace CampusOps.Api.Locations;

public static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/locations").WithTags("Locations");
        group.MapPost("/", async (CreateLocationRequest request, LocationService service, CancellationToken token) =>
        {
            var location = await service.CreateAsync(request.Campus, request.Building, request.Room, token);
            return Results.Created($"/api/v1/locations/{location.Id}", Map(location));
        });
        group.MapGet("/", async (LocationService service, CancellationToken token) =>
            Results.Ok((await service.ListAsync(token)).Select(Map)));
        group.MapGet("/{id:guid}", async (Guid id, LocationService service, CancellationToken token) =>
        {
            var location = await service.GetAsync(id, token);
            return location is null ? Results.NotFound() : Results.Ok(Map(location));
        });
        return endpoints;
    }

    private static LocationResponse Map(Location location) =>
        new(location.Id, location.Campus, location.Building, location.Room, location.DisplayName);
}
