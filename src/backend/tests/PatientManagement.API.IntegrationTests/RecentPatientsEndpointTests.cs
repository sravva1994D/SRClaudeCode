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

public sealed class RecentPatientsEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Passw0rd!";
    private const string PatientsPath = "/api/v1/patients";
    private const string RecentPath = "/api/v1/patients/recent";

    [Fact]
    public async Task Recent_WithoutAccessToken_Returns401()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);

        var response = await client.GetAsync(RecentPath);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RecordViewed_ThenRecent_ListsThePatient()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var created = await SendAsync(client, HttpMethod.Post, PatientsPath, token, JsonContent.Create(ValidRequest()));
        var patientId = (await created.Content.ReadFromJsonAsync<PatientBody>())!.PatientId;

        var viewed = await SendAsync(client, HttpMethod.Post, $"{PatientsPath}/{patientId}/viewed", token, content: null);
        var recent = await SendAsync(client, HttpMethod.Get, RecentPath, token, content: null);

        viewed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        recent.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await recent.Content.ReadFromJsonAsync<List<RecentBody>>();
        items.Should().ContainSingle().Which.PatientId.Should().Be(patientId);
    }

    [Fact]
    public async Task RecordViewed_UnknownPatient_Returns404WithCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);

        var response = await SendAsync(client, HttpMethod.Post, $"{PatientsPath}/9999/viewed", token, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("record_not_found");
    }

    private static PatientRequest ValidRequest() => new(
        "Asha Rao", new DateOnly(1990, 5, 14), 2, "9876543210", "12 Lake Road");

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

    private sealed record PatientBody(int PatientId, string FullName);

    private sealed record RecentBody(int PatientId, string FullName, DateOnly DateOfBirth, int GenderId, string? PhoneNumber);
}
