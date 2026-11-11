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

public sealed class AppointmentsListEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Passw0rd!";
    private const string PatientsPath = "/api/v1/patients";
    private const string AppointmentsPath = "/api/v1/appointments";

    [Fact]
    public async Task List_BookedSlotInRange_ReturnsIt()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await CreatePatientAsync(client, token);
        var slot = new DateTime(2099, 1, 5, 9, 15, 0, DateTimeKind.Utc);
        await SendAsync(client, HttpMethod.Post, AppointmentsPath, token, JsonContent.Create(new ScheduleAppointmentRequest(patientId, slot)));

        var response = await SendAsync(client, HttpMethod.Get, ListPath(new DateTime(2099, 1, 5), new DateTime(2099, 1, 6)), token, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<ListItemBody>>();
        items.Should().ContainSingle().Which.SlotStartUtc.Should().Be(slot);
    }

    [Fact]
    public async Task List_BookedSlotOutsideRange_IsNotReturned()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await CreatePatientAsync(client, token);
        await SendAsync(client, HttpMethod.Post, AppointmentsPath, token, JsonContent.Create(new ScheduleAppointmentRequest(patientId, new DateTime(2099, 1, 5, 9, 15, 0, DateTimeKind.Utc))));

        var response = await SendAsync(client, HttpMethod.Get, ListPath(new DateTime(2099, 1, 6), new DateTime(2099, 1, 7)), token, content: null);

        (await response.Content.ReadFromJsonAsync<List<ListItemBody>>()).Should().BeEmpty();
    }

    [Fact]
    public async Task List_EndBeforeStart_Returns400WithValidationCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);

        var response = await SendAsync(client, HttpMethod.Get, ListPath(new DateTime(2099, 1, 7), new DateTime(2099, 1, 6)), token, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("validation_failed");
    }

    private static string ListPath(DateTime from, DateTime to) =>
        $"{AppointmentsPath}?from={Uri.EscapeDataString(DateTime.SpecifyKind(from, DateTimeKind.Utc).ToString("O"))}" +
        $"&to={Uri.EscapeDataString(DateTime.SpecifyKind(to, DateTimeKind.Utc).ToString("O"))}";

    private static async Task<int> CreatePatientAsync(HttpClient client, string token)
    {
        var request = new PatientRequest("Asha Rao", new DateOnly(1990, 5, 14), 2, "9876543210", null);
        var response = await SendAsync(client, HttpMethod.Post, PatientsPath, token, JsonContent.Create(request));
        response.StatusCode.Should().Be(HttpStatusCode.Created, "the patient must exist before an appointment can be booked");
        return (await response.Content.ReadFromJsonAsync<PatientBody>())!.PatientId;
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
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the sign-in must succeed before appointments can be used");
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

    private sealed record PatientBody(int PatientId);

    private sealed record ListItemBody(int AppointmentId, int PatientId, string PatientFullName, DateTime SlotStartUtc, int StatusId);
}
