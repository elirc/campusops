using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
namespace CampusOps.Api.Tests.Operations;

public sealed class HealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client; public HealthTests(WebApplicationFactory<Program> factory) => _client = factory.WithWebHostBuilder(x => x.UseEnvironment("Testing")).CreateClient();
    [Fact] public async Task Readiness_IsHonestAndEchoesCorrelationId() { var request = new HttpRequestMessage(HttpMethod.Get, "/health/ready"); request.Headers.Add("X-Correlation-ID", "ops-test-42"); var response = await _client.SendAsync(request); var body = await response.Content.ReadFromJsonAsync<Readiness>(); Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal("ops-test-42", response.Headers.GetValues("X-Correlation-ID").Single()); Assert.NotNull(body); Assert.False(body.Durable); }
    private sealed record Readiness(string Status, string Persistence, bool Durable, string Worker, string Note);
}
