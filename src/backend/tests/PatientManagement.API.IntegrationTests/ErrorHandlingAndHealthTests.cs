using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using PatientManagement.API.Errors;
using PatientManagement.API.IntegrationTests.Support;

namespace PatientManagement.API.IntegrationTests;

public sealed class ErrorHandlingAndHealthTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ReachableConnectionString =
        "Server=(LocalDb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

    // A named instance that does not exist. The short connect timeout keeps the test fast.
    private const string UnreachableConnectionString =
        "Server=(LocalDb)\\PatientManagementNoSuchInstance;Database=master;Trusted_Connection=True;Connect Timeout=5;";

    private const string CorrelationHeader = "X-Correlation-Id";

    [Fact]
    public async Task Get_Health_ReturnsOkWhenDatabaseIsReachable()
    {
        using var client = CreateClient("Development", ReachableConnectionString);

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Health_ReturnsServiceUnavailableWhenDatabaseIsUnreachable()
    {
        using var client = CreateClient("Development", UnreachableConnectionString);

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Get_SwaggerUi_InDevelopment_ReturnsOk()
    {
        using var client = CreateClient("Development", ReachableConnectionString);

        var response = await client.GetAsync("/swagger/index.html");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_SwaggerUi_InProduction_ReturnsNotFound()
    {
        using var client = CreateClient("Production", ReachableConnectionString);

        var response = await client.GetAsync("/swagger/index.html");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_DomainFailure_Returns422WithStableCode()
    {
        using var client = CreateClient("Development", ReachableConnectionString);

        var response = await client.GetAsync("/api/v1/test-failures/domain");
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        error.Should().NotBeNull();
        error!.Code.Should().Be("appointment_in_past");
        error.Message.Should().Be("The appointment is in the past.");
    }

    [Fact]
    public async Task Get_UnexpectedFailure_Returns500WithoutInternalDetail()
    {
        using var client = CreateClient("Development", ReachableConnectionString);

        var response = await client.GetAsync("/api/v1/test-failures/unexpected");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        body.Should().Contain("internal_error");
        body.Should().NotContain("must not reach the client");
        body.Should().NotContain("StackTrace");
    }

    [Fact]
    public async Task Get_ErrorResponse_ContainsTheRequestCorrelationId()
    {
        using var client = CreateClient("Development", ReachableConnectionString);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/test-failures/domain");
        request.Headers.Add(CorrelationHeader, "trace-abc-123");

        var response = await client.SendAsync(request);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        response.Headers.GetValues(CorrelationHeader).Should().ContainSingle().Which.Should().Be("trace-abc-123");
        error!.CorrelationId.Should().Be("trace-abc-123");
    }

    [Fact]
    public async Task Get_InvalidCorrelationId_IsReplacedWithGeneratedId()
    {
        using var client = CreateClient("Development", ReachableConnectionString);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(CorrelationHeader, "not valid: <script>");

        var response = await client.SendAsync(request);

        var returnedId = response.Headers.GetValues(CorrelationHeader).Single();
        returnedId.Should().MatchRegex("^[0-9a-f]{32}$");
    }

    private HttpClient CreateClient(string environment, string connectionString) =>
        TestHost.With(factory, environment, connectionString).CreateClient();
}
