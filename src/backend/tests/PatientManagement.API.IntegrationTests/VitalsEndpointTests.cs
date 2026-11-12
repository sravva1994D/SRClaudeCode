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

public sealed class VitalsEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Passw0rd!";
    private const string PatientsPath = "/api/v1/patients";
    private const string AppointmentsPath = "/api/v1/appointments";
    private const string VisitsPath = "/api/v1/visits";

    // A test-only pulse range, inserted into each test's own database. It is not a clinical reference value; the real
    // bounds are set when the doctor confirms them (Q7).
    private const string InsertTestPulseRangeSql =
        "INSERT INTO dbo.VitalRanges (VitalRangeId, VitalSign, Unit, NormalMinimum, NormalMaximum, PlausibleMinimum, PlausibleMaximum) " +
        "VALUES (1, N'Pulse', N'bpm', 60, 100, 20, 250);";

    [Fact]
    public async Task RecordVitals_NoRangesConfigured_Returns201WithNoWarnings()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var visitId = await StartVisitAsync(client, token);

        var response = await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/vitals", token,
            JsonContent.Create(new RecordVitalsRequest(36.6m, 120, 80, 72)));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.Content.ReadFromJsonAsync<VitalsBody>())!.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordVitals_AbnormalPulseWithRangeConfigured_Returns201WithWarning()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        await ExecuteSqlAsync(database, InsertTestPulseRangeSql);
        var token = await SignInAsync(client);
        var visitId = await StartVisitAsync(client, token);

        var response = await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/vitals", token,
            JsonContent.Create(new RecordVitalsRequest(null, null, null, 130)));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.Content.ReadFromJsonAsync<VitalsBody>())!.Warnings.Should().Equal("Pulse");
    }

    [Fact]
    public async Task RecordVitals_ImplausiblePulse_Returns400WithCodeAndSavesNothing()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        await ExecuteSqlAsync(database, InsertTestPulseRangeSql);
        var token = await SignInAsync(client);
        var visitId = await StartVisitAsync(client, token);

        var response = await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/vitals", token,
            JsonContent.Create(new RecordVitalsRequest(null, null, null, 900)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("vitals_implausible");
        var stored = await SendAsync(client, HttpMethod.Get, $"{VisitsPath}/{visitId}/vitals", token, content: null);
        stored.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateVitals_CompletedVisit_Returns200AndTheCorrectionIsStored()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var visitId = await StartVisitAsync(client, token);
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/vitals", token,
            JsonContent.Create(new RecordVitalsRequest(36.6m, 120, 80, 72)));
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/complete", token, content: null);

        var corrected = await SendAsync(client, HttpMethod.Put, $"{VisitsPath}/{visitId}/vitals", token,
            JsonContent.Create(new RecordVitalsRequest(36.6m, 120, 80, 76)));

        corrected.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await SendAsync(client, HttpMethod.Get, $"{VisitsPath}/{visitId}/vitals", token, content: null);
        (await stored.Content.ReadFromJsonAsync<VitalsDetailsBody>())!.Pulse.Should().Be(76);
    }

    [Fact]
    public async Task UpdateVitals_NoVitalsRecorded_Returns404()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var visitId = await StartVisitAsync(client, token);

        var response = await SendAsync(client, HttpMethod.Put, $"{VisitsPath}/{visitId}/vitals", token,
            JsonContent.Create(new RecordVitalsRequest(null, null, null, 76)));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateVitals_ImplausiblePulse_Returns400AndKeepsTheStoredValue()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        await ExecuteSqlAsync(database, InsertTestPulseRangeSql);
        var token = await SignInAsync(client);
        var visitId = await StartVisitAsync(client, token);
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/vitals", token,
            JsonContent.Create(new RecordVitalsRequest(null, null, null, 72)));

        var response = await SendAsync(client, HttpMethod.Put, $"{VisitsPath}/{visitId}/vitals", token,
            JsonContent.Create(new RecordVitalsRequest(null, null, null, 900)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("vitals_implausible");
        var stored = await SendAsync(client, HttpMethod.Get, $"{VisitsPath}/{visitId}/vitals", token, content: null);
        (await stored.Content.ReadFromJsonAsync<VitalsDetailsBody>())!.Pulse.Should().Be(72);
    }

    [Fact]
    public async Task RecordVitals_SecondTimeForTheSameVisit_Returns409WithCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var visitId = await StartVisitAsync(client, token);
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/vitals", token,
            JsonContent.Create(new RecordVitalsRequest(36.6m, 120, 80, 72)));

        var second = await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/vitals", token,
            JsonContent.Create(new RecordVitalsRequest(37m, 118, 78, 70)));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await second.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("vitals_already_recorded");
    }

    private static async Task ExecuteSqlAsync(TemporaryDatabase database, string sql)
    {
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> StartVisitAsync(HttpClient client, string token)
    {
        var patient = await SendAsync(client, HttpMethod.Post, PatientsPath, token,
            JsonContent.Create(new PatientRequest("Asha Rao", new DateOnly(1990, 5, 14), 2, "9876543210", null)));
        var patientId = (await patient.Content.ReadFromJsonAsync<PatientBody>())!.PatientId;
        var future = DateTime.UtcNow.AddDays(3);
        var slot = new DateTime(future.Year, future.Month, future.Day, future.Hour, future.Minute / 15 * 15, 0, DateTimeKind.Utc);
        var appointment = await SendAsync(client, HttpMethod.Post, AppointmentsPath, token,
            JsonContent.Create(new ScheduleAppointmentRequest(patientId, slot)));
        var appointmentId = (await appointment.Content.ReadFromJsonAsync<AppointmentBody>())!.AppointmentId;
        var started = await SendAsync(client, HttpMethod.Post, $"{AppointmentsPath}/{appointmentId}/start-visit", token, content: null);
        started.StatusCode.Should().Be(HttpStatusCode.Created, "a visit must be in progress before vitals are recorded");
        return (await started.Content.ReadFromJsonAsync<VisitBody>())!.VisitId;
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
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the sign-in must succeed before vitals can be recorded");
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

    private sealed record AppointmentBody(int AppointmentId);

    private sealed record VisitBody(int VisitId);

    private sealed record VitalsBody(VitalsDetailsBody Vitals, IReadOnlyList<string> Warnings);

    private sealed record VitalsDetailsBody(int VitalsId, int VisitId, int? Pulse);
}
