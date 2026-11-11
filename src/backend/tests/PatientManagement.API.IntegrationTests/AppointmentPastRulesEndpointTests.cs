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

public sealed class AppointmentPastRulesEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Passw0rd!";
    private const string PatientsPath = "/api/v1/patients";
    private const string AppointmentsPath = "/api/v1/appointments";

    [Fact]
    public async Task Reschedule_AppointmentWhoseSlotHasPassed_Returns422WithTimeLockedCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await CreatePatientAsync(client, token);
        var appointmentId = await BookAsync(client, token, patientId);
        await MoveSlotIntoThePastAsync(database, appointmentId);

        var response = await SendAsync(client, HttpMethod.Put, $"{AppointmentsPath}/{appointmentId}/reschedule", token,
            JsonContent.Create(new RescheduleAppointmentRequest(FutureSlot())));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("appointment_time_locked");
    }

    [Fact]
    public async Task Complete_AppointmentWhoseSlotHasPassed_Returns200ThenSecondChangeIsLocked()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await CreatePatientAsync(client, token);
        var appointmentId = await BookAsync(client, token, patientId);
        await MoveSlotIntoThePastAsync(database, appointmentId);

        var completed = await SendAsync(client, HttpMethod.Post, $"{AppointmentsPath}/{appointmentId}/complete", token, content: null);
        var again = await SendAsync(client, HttpMethod.Post, $"{AppointmentsPath}/{appointmentId}/no-show", token, content: null);

        completed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await completed.Content.ReadFromJsonAsync<AppointmentBody>())!.StatusId.Should().Be(2);
        again.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await again.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("appointment_status_locked");
    }

    /// <summary>Booking never allows a past slot, so the test moves a booked slot into the past directly in the database.</summary>
    private static async Task MoveSlotIntoThePastAsync(TemporaryDatabase database, int appointmentId)
    {
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "UPDATE dbo.Appointments SET SlotStart = '2020-01-06T09:00:00' WHERE AppointmentId = @AppointmentId;", connection);
        command.Parameters.AddWithValue("@AppointmentId", appointmentId);
        (await command.ExecuteNonQueryAsync()).Should().Be(1);
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
        response.StatusCode.Should().Be(HttpStatusCode.Created, "the slot must be bookable before the test changes it");
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

    private sealed record AppointmentBody(int AppointmentId, int PatientId, DateTime SlotStartUtc, int StatusId);
}
