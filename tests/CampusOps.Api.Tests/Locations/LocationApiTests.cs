using System.Net;
using System.Net.Http.Json;
using CampusOps.Contracts.Locations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CampusOps.Api.Tests.Locations;

public sealed class LocationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public LocationApiTests(WebApplicationFactory<Program> factory) { _client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing")).CreateClient(); _client.DefaultRequestHeaders.Add("X-Demo-Role", "Manager"); _client.DefaultRequestHeaders.Add("X-Demo-Campus", "North"); }

    [Fact]
    public async Task CreateThenGet_ReturnsLocationContract()
    {
        var created = await _client.PostAsJsonAsync("/api/v1/locations", new CreateLocationRequest("North", "Science", "B-12"));
        var body = await created.Content.ReadFromJsonAsync<LocationResponse>();
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/v1/locations/{body.Id}")).StatusCode);
    }
}
