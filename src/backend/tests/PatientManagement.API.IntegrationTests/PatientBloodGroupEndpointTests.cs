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

public sealed class PatientBloodGroupEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Passw0rd!";
    private const string PatientsPath = "/api/v1/patients";

    [Fact]
    public async Task CreateThenGet_BloodGroupAndChronicConditions_AreReturned()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var request = new PatientRequest(
            "Asha Rao", new DateOnly(1990, 5, 14), 2, "9876543210", null, 3, "Asthma");

        var created = await SendAsync(client, HttpMethod.Post, PatientsPath, token, JsonContent.Create(request));
        var patientId = (await created.Content.ReadFromJsonAsync<PatientBody>())!.PatientId;
        var fetched = await SendAsync(client, HttpMethod.Get, $"{PatientsPath}/{patientId}", token, content: null);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await fetched.Content.ReadFromJsonAsync<PatientBody>();
        body!.BloodGroupId.Should().Be(3);
        body.ChronicConditions.Should().Be("Asthma");
    }

    [Fact]
    public async Task Create_BloodGroupOutsideSeededRange_Returns400WithValidationCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var request = new PatientRequest(
            "Asha Rao", new DateOnly(1990, 5, 14), 2, null, null, 42, null);

        var response = await SendAsync(client, HttpMethod.Post, PatientsPath, token, JsonContent.Create(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("validation_failed");
    }

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

    private sealed record PatientBody(
        int PatientId,
        string FullName,
        DateOnly DateOfBirth,
        int GenderId,
        string? PhoneNumber,
        string? Address,
        int? BloodGroupId,
        string? ChronicConditions);
}
