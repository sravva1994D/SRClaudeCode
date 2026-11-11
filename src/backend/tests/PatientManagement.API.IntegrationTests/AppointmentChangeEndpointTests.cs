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

public sealed class AppointmentChangeEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Passw0rd!";
    private const string PatientsPath = "/api/v1/patients";
    private const string AppointmentsPath = "/api/v1/appointments";

    [Fact]
    public async Task Reschedule_WithoutAccessToken_Returns401()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);

        var response = await client.PutAsJsonAsync($"{AppointmentsPath}/1/reschedule", new RescheduleAppointmentRequest(FutureSlot()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reschedule_ScheduledAppointment_Returns200WithTheNewSlot()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await CreatePatientAsync(client, token);
        var appointmentId = await BookAsync(client, token, patientId, FutureSlot());
        var moved = FutureSlot().AddHours(1);

        var response = await SendAsync(client, HttpMethod.Put, $"{AppointmentsPath}/{appointmentId}/reschedule", token, JsonContent.Create(new RescheduleAppointmentRequest(moved)));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<AppointmentBody>())!.SlotStartUtc.Should().Be(moved);
    }

    [Fact]
    public async Task Reschedule_IntoAnotherBookingsSlot_Returns409WithSlotCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await CreatePatientAsync(client, token);
        var first = FutureSlot();
        var second = FutureSlot().AddHours(1);
        var appointmentId = await BookAsync(client, token, patientId, first);
        await BookAsync(client, token, patientId, second);

        var response = await SendAsync(client, HttpMethod.Put, $"{AppointmentsPath}/{appointmentId}/reschedule", token, JsonContent.Create(new RescheduleAppointmentRequest(second)));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("slot_already_booked");
    }

    [Fact]
    public async Task Reschedule_CompletedAppointment_Returns422WithCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await CreatePatientAsync(client, token);
        var appointmentId = await BookAsync(client, token, patientId, FutureSlot());
        await SendAsync(client, HttpMethod.Post, $"{AppointmentsPath}/{appointmentId}/complete", token, content: null);

        var response = await SendAsync(client, HttpMethod.Put, $"{AppointmentsPath}/{appointmentId}/reschedule", token, JsonContent.Create(new RescheduleAppointmentRequest(FutureSlot().AddHours(1))));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("appointment_not_reschedulable");
    }

    [Fact]
    public async Task Cancel_ScheduledAppointment_Returns200AndFreesTheSlot()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await CreatePatientAsync(client, token);
        var slot = FutureSlot();
        var appointmentId = await BookAsync(client, token, patientId, slot);

        var cancelled = await SendAsync(client, HttpMethod.Post, $"{AppointmentsPath}/{appointmentId}/cancel", token, content: null);
        var rebooked = await SendAsync(client, HttpMethod.Post, AppointmentsPath, token, JsonContent.Create(new ScheduleAppointmentRequest(patientId, slot)));

        cancelled.StatusCode.Should().Be(HttpStatusCode.OK);
        (await cancelled.Content.ReadFromJsonAsync<AppointmentBody>())!.StatusId.Should().Be(3);
        rebooked.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task NoShow_ThenCancel_SecondChangeReturns422WithCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await CreatePatientAsync(client, token);
        var appointmentId = await BookAsync(client, token, patientId, FutureSlot());

        var noShow = await SendAsync(client, HttpMethod.Post, $"{AppointmentsPath}/{appointmentId}/no-show", token, content: null);
        var cancel = await SendAsync(client, HttpMethod.Post, $"{AppointmentsPath}/{appointmentId}/cancel", token, content: null);

        noShow.StatusCode.Should().Be(HttpStatusCode.OK);
        (await noShow.Content.ReadFromJsonAsync<AppointmentBody>())!.StatusId.Should().Be(4);
        cancel.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await cancel.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("appointment_status_locked");
    }

    private static DateTime FutureSlot()
    {
        var future = DateTime.UtcNow.AddDays(3);
        return new DateTime(future.Year, future.Month, future.Day, future.Hour, future.Minute / 15 * 15, 0, DateTimeKind.Utc);
    }

    private static async Task<int> BookAsync(HttpClient client, string token, int patientId, DateTime slot)
    {
        var response = await SendAsync(client, HttpMethod.Post, AppointmentsPath, token, JsonContent.Create(new ScheduleAppointmentRequest(patientId, slot)));
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
