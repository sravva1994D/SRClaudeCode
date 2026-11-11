using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using PatientManagement.API.Auth;
using PatientManagement.API.Controllers;
using PatientManagement.API.Errors;
using PatientManagement.API.IntegrationTests.Support;

namespace PatientManagement.API.IntegrationTests;

public sealed class ClinicProfileEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Passw0rd!";
    private const string ProfilePath = "/api/v1/clinic-profile";

    [Fact]
    public async Task Get_WithoutAccessToken_Returns401()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);

        var response = await client.GetAsync(ProfilePath);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_SignedIn_ReturnsTheSeededClinicProfile()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);

        var response = await SendAsync(client, HttpMethod.Get, ProfilePath, token, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ClinicProfileBody>();
        profile!.ClinicName.Should().Be("Synthetic Clinic");
    }

    [Fact]
    public async Task Put_PhoneWithSymbols_StoresAndReturnsDigitsOnly()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var update = new UpdateClinicProfileRequest(
            "Riverside Clinic", "Dr. Asha Rao", "MBBS, MD", "KMC-12345", "12 Lake Road", "+91 (98) 765-43210", "Call before 6pm.");

        var response = await SendAsync(client, HttpMethod.Put, ProfilePath, token, JsonContent.Create(update));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ClinicProfileBody>();
        profile!.ClinicName.Should().Be("Riverside Clinic");
        profile.PhoneNumber.Should().Be("919876543210");
    }

    [Fact]
    public async Task Put_ChangedProfile_WritesOneAuditRowWithTheChangedField()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var update = new UpdateClinicProfileRequest(
            "Riverside Clinic", "Dr. Asha Rao", null, null, null, null, null);

        var response = await SendAsync(client, HttpMethod.Put, ProfilePath, token, JsonContent.Create(update));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auditRows = await CountClinicProfileAuditRowsAsync(database);
        auditRows.Should().Be(1);
        (await ReadNewValuesAsync(database)).Should().Contain("\"ClinicName\":\"Riverside Clinic\"");
    }

    [Fact]
    public async Task Put_ClinicNameTooLong_Returns400WithValidationCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var update = new UpdateClinicProfileRequest(
            new string('x', 201), null, null, null, null, null, null);

        var response = await SendAsync(client, HttpMethod.Put, ProfilePath, token, JsonContent.Create(update));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("validation_failed");
    }

    private static async Task<int> CountClinicProfileAuditRowsAsync(TemporaryDatabase database)
    {
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.AuditLogs WHERE TableName = N'ClinicProfile';", connection);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<string> ReadNewValuesAsync(TemporaryDatabase database)
    {
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT NewValues FROM dbo.AuditLogs WHERE TableName = N'ClinicProfile';", connection);
        return (string)(await command.ExecuteScalarAsync())!;
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
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the sign-in must succeed before the profile can be read");
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

    private sealed record ClinicProfileBody(
        string? ClinicName,
        string? DoctorName,
        string? Qualifications,
        string? RegistrationNumber,
        string? Address,
        string? PhoneNumber,
        string? FooterNotes);
}
