using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using PatientManagement.API.Auth;
using PatientManagement.API.Controllers;
using PatientManagement.API.Errors;
using PatientManagement.API.IntegrationTests.Support;

namespace PatientManagement.API.IntegrationTests;

public sealed class PatientListEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Passw0rd!";
    private const string PatientsPath = "/api/v1/patients";

    [Fact]
    public async Task List_SignedIn_ReturnsOnePageWithTheTotalAndPagingFields()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        await SendAsync(client, HttpMethod.Post, PatientsPath, token, JsonContent.Create(ValidRequest("Asha Rao")));

        var response = await SendAsync(client, HttpMethod.Get, $"{PatientsPath}?search=asha", token, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PageBody>();
        page!.TotalCount.Should().Be(1);
        page.PageNumber.Should().Be(1);
        page.PageSize.Should().Be(20);
        page.Items.Should().ContainSingle().Which.FullName.Should().Be("Asha Rao");
    }

    [Fact]
    public async Task List_PageSizeAboveLimit_Returns400WithValidationCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);

        var response = await SendAsync(client, HttpMethod.Get, $"{PatientsPath}?pageSize=101", token, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("validation_failed");
    }

    private static PatientRequest ValidRequest(string fullName) => new(
        fullName, new DateOnly(1990, 5, 14), 2, "9876543210", "12 Lake Road");

    private async Task<HttpClient> StartAsync(TemporaryDatabase database)
    {
        await database.MigrateAsync();
        await database.SeedDoctorAsync(Password);
        return TestHost.With(factory, "Development", database.ConnectionString).CreateClient();
    }

    private static async Task<string> SignInAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("dev.doctor", Password));
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the sign-in must succeed before patients can be used");
        return (await response.Content.ReadFromJsonAsync<AccessTokenResponse>())!.AccessToken;
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        string token,
        HttpContent? content)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private sealed record PageBody(IReadOnlyList<ListItemBody> Items, int TotalCount, int PageNumber, int PageSize);

    private sealed record ListItemBody(int PatientId, string FullName, DateOnly DateOfBirth, int GenderId, string? PhoneNumber);
}
