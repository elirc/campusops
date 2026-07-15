using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
namespace CampusOps.Api.Tests.Security;

public sealed class AuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public AuthorizationTests(WebApplicationFactory<Program> factory) => _factory = factory.WithWebHostBuilder(x => x.UseEnvironment("Testing"));
    [Fact] public async Task Procurement_WithoutIdentity_IsUnauthorized() => Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().PostAsync("/api/v1/procurement/vendors", null)).StatusCode);
    [Fact] public async Task Procurement_Dispatcher_IsForbidden() { var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/procurement/vendors"); request.Headers.Add("X-Demo-Role", "Dispatcher"); request.Headers.Add("X-Demo-Campus", "North"); Assert.Equal(HttpStatusCode.Forbidden, (await _factory.CreateClient().SendAsync(request)).StatusCode); }
}
