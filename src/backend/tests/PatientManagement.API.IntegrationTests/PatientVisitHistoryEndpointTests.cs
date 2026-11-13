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

public sealed class PatientVisitHistoryEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Passw0rd!";
    private const string PatientsPath = "/api/v1/patients";
    private const string AppointmentsPath = "/api/v1/appointments";

    [Fact]
    public async Task VisitHistory_VisitStartedToday_IsListedInARangeThatIncludesToday_AndNotInOneThatExcludesIt()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await AddPatientAsync(client, token);
        await StartVisitAsync(client, token, patientId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var included = await SendAsync(client, HttpMethod.Get,
            $"{PatientsPath}/{patientId}/visits?from={today.AddDays(-1):yyyy-MM-dd}&to={today.AddDays(1):yyyy-MM-dd}", token);
        var excluded = await SendAsync(client, HttpMethod.Get,
            $"{PatientsPath}/{patientId}/visits?from={today.AddDays(1):yyyy-MM-dd}", token);

        included.StatusCode.Should().Be(HttpStatusCode.OK);
        (await included.Content.ReadFromJsonAsync<PagedBody>())!.TotalCount.Should().Be(1);
        excluded.StatusCode.Should().Be(HttpStatusCode.OK);
        (await excluded.Content.ReadFromJsonAsync<PagedBody>())!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task VisitHistory_EndDateBeforeStartDate_Returns400WithAValidationError()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await AddPatientAsync(client, token);

        var response = await SendAsync(client, HttpMethod.Get,
            $"{PatientsPath}/{patientId}/visits?from=2026-11-30&to=2026-11-01", token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task VisitHistory_UnknownPatient_Returns404()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);

        var response = await SendAsync(client, HttpMethod.Get, $"{PatientsPath}/999999/visits", token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<int> AddPatientAsync(HttpClient client, string token)
    {
        var patient = await SendAsync(client, HttpMethod.Post, PatientsPath, token,
            JsonContent.Create(new PatientRequest("Asha Rao", new DateOnly(1990, 5, 14), 2, "9876543210", null)));
        return (await patient.Content.ReadFromJsonAsync<PatientBody>())!.PatientId;
    }

    private static async Task StartVisitAsync(HttpClient client, string token, int patientId)
    {
        var future = DateTime.UtcNow.AddDays(3);
        var slot = new DateTime(future.Year, future.Month, future.Day, future.Hour, future.Minute / 15 * 15, 0, DateTimeKind.Utc);
        var appointment = await SendAsync(client, HttpMethod.Post, AppointmentsPath, token,
            JsonContent.Create(new ScheduleAppointmentRequest(patientId, slot)));
        var appointmentId = (await appointment.Content.ReadFromJsonAsync<AppointmentBody>())!.AppointmentId;
        var started = await SendAsync(client, HttpMethod.Post, $"{AppointmentsPath}/{appointmentId}/start-visit", token);
        started.StatusCode.Should().Be(HttpStatusCode.Created);
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
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the sign-in must succeed before the history can be read");
        return (await response.Content.ReadFromJsonAsync<AccessTokenResponse>())!.AccessToken;
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        string token,
        HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private sealed record PatientBody(int PatientId);

    private sealed record AppointmentBody(int AppointmentId);

    private sealed record PagedBody(int TotalCount);
}
