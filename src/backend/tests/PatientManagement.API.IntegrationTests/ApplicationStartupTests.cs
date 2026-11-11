using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using PatientManagement.API.IntegrationTests.Support;

namespace PatientManagement.API.IntegrationTests;

public sealed class ApplicationStartupTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Get_OpenApiDocument_ReturnsOk()
    {
        // Migrations stay off so the test never touches the development database.
        using var client = TestHost.With(factory, "Development", TestHost.ReachableConnectionString).CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
