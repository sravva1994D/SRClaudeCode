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

public sealed class PatientsEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Passw0rd!";
    private const string PatientsPath = "/api/v1/patients";

    [Fact]
    public async Task Post_WithoutAccessToken_Returns401()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);

        var response = await client.PostAsJsonAsync(PatientsPath, ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ValidPatient_Returns201WithLocationAndDigitsOnlyPhone()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);

        var response = await SendAsync(
            client, HttpMethod.Post, PatientsPath, token, JsonContent.Create(ValidRequest()));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var created = await response.Content.ReadFromJsonAsync<PatientBody>();
        created!.PhoneNumber.Should().Be("919876543210");
    }

    [Fact]
    public async Task Get_CreatedPatient_ReturnsTheSamePatient()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var created = await CreateAsync(client, token, ValidRequest());

        var response = await SendAsync(client, HttpMethod.Get, $"{PatientsPath}/{created.PatientId}", token, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<PatientBody>())!.FullName.Should().Be("Asha Rao");
    }

    [Fact]
    public async Task Get_UnknownPatient_Returns404WithCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);

        var response = await SendAsync(client, HttpMethod.Get, $"{PatientsPath}/9999", token, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("record_not_found");
    }

    [Fact]
    public async Task Post_MissingGender_Returns400WithValidationCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var request = ValidRequest() with { GenderId = null };

        var response = await SendAsync(
            client, HttpMethod.Post, PatientsPath, token, JsonContent.Create(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("validation_failed");
    }

    [Fact]
    public async Task Post_DateOfBirthInFuture_Returns422WithCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var request = ValidRequest() with { DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2) };

        var response = await SendAsync(client, HttpMethod.Post, PatientsPath, token, JsonContent.Create(request));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("date_of_birth_in_future");
    }

    [Fact]
    public async Task Put_ExistingPatient_Returns200WithNewAddress()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var created = await CreateAsync(client, token, ValidRequest());
        var update = ValidRequest() with { Address = "7 Hill View" };

        var response = await SendAsync(
            client, HttpMethod.Put, $"{PatientsPath}/{created.PatientId}", token, JsonContent.Create(update));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<PatientBody>())!.Address.Should().Be("7 Hill View");
    }

    [Fact]
    public async Task Delete_ExistingPatient_Returns204AndHidesThePatient()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var created = await CreateAsync(client, token, ValidRequest());
        var path = $"{PatientsPath}/{created.PatientId}";

        var deleted = await SendAsync(client, HttpMethod.Delete, path, token, content: null);
        var afterDelete = await SendAsync(client, HttpMethod.Get, path, token, content: null);

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await CountPatientAuditRowsAsync(database, "Delete")).Should().Be(1);
    }

    private static PatientRequest ValidRequest() => new(
        "Asha Rao", new DateOnly(1990, 5, 14), 2, "+91 (98) 765-43210", "12 Lake Road");

    private static async Task<PatientBody> CreateAsync(HttpClient client, string token, PatientRequest request)
    {
        var response = await SendAsync(client, HttpMethod.Post, PatientsPath, token, JsonContent.Create(request));
        response.StatusCode.Should().Be(HttpStatusCode.Created, "the patient must exist before the test can use it");
        return (await response.Content.ReadFromJsonAsync<PatientBody>())!;
    }

    private static async Task<int> CountPatientAuditRowsAsync(TemporaryDatabase database, string action)
    {
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.AuditLogs WHERE TableName = N'Patients' AND Action = @Action;", connection);
        command.Parameters.AddWithValue("@Action", action);
        return (int)(await command.ExecuteScalarAsync())!;
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
        string? Address);
}
