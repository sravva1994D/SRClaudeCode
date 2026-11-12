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

public sealed class VisitStartEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Passw0rd!";
    private const string PatientsPath = "/api/v1/patients";
    private const string AppointmentsPath = "/api/v1/appointments";
    private const string VisitsPath = "/api/v1/visits";

    [Fact]
    public async Task StartVisit_ScheduledAppointment_Returns201WithLocationAndTheVisitCanBeRead()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await CreatePatientAsync(client, token);
        var appointmentId = await BookAsync(client, token, patientId);

        var started = await SendAsync(client, HttpMethod.Post, $"{AppointmentsPath}/{appointmentId}/start-visit", token, content: null);
        var fetched = await SendAsync(client, HttpMethod.Get, started.Headers.Location!.ToString(), token, content: null);

        started.StatusCode.Should().Be(HttpStatusCode.Created);
        started.Headers.Location.Should().NotBeNull();
        (await started.Content.ReadFromJsonAsync<VisitBody>())!.StatusId.Should().Be(1);
        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StartVisit_SecondTimeForTheSameAppointment_Returns409WithCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await CreatePatientAsync(client, token);
        var appointmentId = await BookAsync(client, token, patientId);
        await SendAsync(client, HttpMethod.Post, $"{AppointmentsPath}/{appointmentId}/start-visit", token, content: null);

        var second = await SendAsync(client, HttpMethod.Post, $"{AppointmentsPath}/{appointmentId}/start-visit", token, content: null);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await second.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("visit_already_started");
    }

    [Fact]
    public async Task StartVisit_CancelledAppointment_Returns422WithCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await CreatePatientAsync(client, token);
        var appointmentId = await BookAsync(client, token, patientId);
        await SendAsync(client, HttpMethod.Post, $"{AppointmentsPath}/{appointmentId}/cancel", token, content: null);

        var response = await SendAsync(client, HttpMethod.Post, $"{AppointmentsPath}/{appointmentId}/start-visit", token, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("appointment_not_startable");
    }

    [Fact]
    public async Task StartVisit_UnknownAppointment_Returns404()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);

        var response = await SendAsync(client, HttpMethod.Post, $"{AppointmentsPath}/9999/start-visit", token, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static DateTime FutureSlot()
    {
        var future = DateTime.UtcNow.AddDays(3);
        return new DateTime(future.Year, future.Month, future.Day, future.Hour, future.Minute / 15 * 15, 0, DateTimeKind.Utc);
    }

    private static async Task<int> BookAsync(HttpClient client, string token, int patientId)
    {
        var response = await SendAsync(client, HttpMethod.Post, AppointmentsPath, token,
            JsonContent.Create(new ScheduleAppointmentRequest(patientId, FutureSlot())));
        response.StatusCode.Should().Be(HttpStatusCode.Created, "the slot must be bookable before the visit starts");
        return (await response.Content.ReadFromJsonAsync<AppointmentBody>())!.AppointmentId;
    }

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
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the sign-in must succeed before visits can be used");
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

    private sealed record AppointmentBody(int AppointmentId, int PatientId, DateTime SlotStartUtc, int StatusId);

    private sealed record VisitBody(int VisitId, int PatientId, int? AppointmentId, int StatusId);
}
