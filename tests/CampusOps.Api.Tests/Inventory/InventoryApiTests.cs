using System.Net;
using System.Net.Http.Json;
using CampusOps.Contracts.Inventory;
using CampusOps.Core.Inventory;
using CampusOps.Core.Requests;
using CampusOps.Core.WorkOrders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CampusOps.Api.Tests.Inventory;

public sealed class InventoryApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public InventoryApiTests(WebApplicationFactory<Program> factory) =>
        _factory = factory.WithWebHostBuilder(x => x.UseEnvironment("Testing"));

    private HttpClient Client(string? role = "Dispatcher")
    {
        var client = _factory.CreateClient();
        if (role is not null)
        {
            client.DefaultRequestHeaders.Add("X-Demo-Role", role);
            client.DefaultRequestHeaders.Add("X-Demo-Campus", "North");
        }
        return client;
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Technician", HttpStatusCode.Forbidden)]
    public async Task InventoryRequiresOperationsRole(string? role, HttpStatusCode expected)
    {
        using var client = Client(role);
        Assert.Equal(expected, (await client.GetAsync("/api/v1/parts")).StatusCode);
        Assert.Equal(expected, (await client.PostAsJsonAsync("/api/v1/parts", new CreatePartRequest("F", "Filter", 5))).StatusCode);
        Assert.Equal(expected, (await client.PostAsJsonAsync($"/api/v1/work-orders/{Guid.NewGuid()}/parts", new ReservePartRequest(Guid.NewGuid(), 1))).StatusCode);
        Assert.Equal(expected, (await client.GetAsync($"/api/v1/work-orders/{Guid.NewGuid()}/parts")).StatusCode);
    }

    [Fact]
    public async Task CreateReadReserveAndHistoryShareOneStore()
    {
        using var client = Client();
        var created = await client.PostAsJsonAsync("/api/v1/parts", new CreatePartRequest(" f-1 ", "Filter", 5));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var part = await client.GetFromJsonAsync<PartResponse>(created.Headers.Location);
        Assert.NotNull(part);
        Assert.Equal("F-1", part.Sku);
        var order = new WorkOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, RequestPriority.Normal, "Replace filter", DateTimeOffset.UtcNow);
        await _factory.Services.GetRequiredService<IWorkOrderStore>().AddAsync(order, default);
        var reserved = await client.PostAsJsonAsync($"/api/v1/work-orders/{order.Id}/parts", new ReservePartRequest(part.Id, 3));
        Assert.Equal(HttpStatusCode.OK, reserved.StatusCode);
        var receipt = await reserved.Content.ReadFromJsonAsync<PartReservation>();
        var history = await client.GetFromJsonAsync<PartReservation[]>($"/api/v1/work-orders/{order.Id}/parts");
        Assert.Equal(receipt, Assert.Single(history!));
        Assert.Equal(2, (await client.GetFromJsonAsync<PartResponse>(created.Headers.Location))!.Available);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/v1/work-orders/{order.Id}/parts", new ReservePartRequest(part.Id, 3))).StatusCode);
        Assert.Single((await client.GetFromJsonAsync<PartReservation[]>($"/api/v1/work-orders/{order.Id}/parts"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/parts/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task MissingWorkOrderCannotReserveStock()
    {
        using var client = Client();
        var created = await client.PostAsJsonAsync("/api/v1/parts", new CreatePartRequest("F-2", "Filter", 5));
        var part = (await created.Content.ReadFromJsonAsync<PartResponse>())!;
        var order = Guid.NewGuid();
        var response = await client.PostAsJsonAsync($"/api/v1/work-orders/{order}/parts", new ReservePartRequest(part.Id, 2));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("work_order.not_found", await response.Content.ReadAsStringAsync());
        Assert.Equal(5, (await client.GetFromJsonAsync<PartResponse>(created.Headers.Location))!.Available);
        Assert.Empty((await client.GetFromJsonAsync<PartReservation[]>($"/api/v1/work-orders/{order}/parts"))!);
    }
}
