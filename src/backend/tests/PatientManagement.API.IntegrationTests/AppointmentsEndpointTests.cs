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

public sealed class AppointmentsEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Passw0rd!";
    private const string PatientsPath = "/api/v1/patients";
    private const string AppointmentsPath = "/api/v1/appointments";

    [Fact]
    public async Task Schedule_WithoutAccessToken_Returns401()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);

        var response = await client.PostAsJsonAsync(AppointmentsPath, new ScheduleAppointmentRequest(1, FutureSlot()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Schedule_FutureSlot_Returns201WithLocationAndGetReturnsIt()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await CreatePatientAsync(client, token);
        var slot = FutureSlot();

        var created = await SendAsync(client, HttpMethod.Post, AppointmentsPath, token, JsonContent.Create(new ScheduleAppointmentRequest(patientId, slot)));
        var body = await created.Content.ReadFromJsonAsync<AppointmentBody>();
        var fetched = await SendAsync(client, HttpMethod.Get, created.Headers.Location!.ToString(), token, content: null);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.Location.Should().NotBeNull();
        body!.SlotStartUtc.Should().Be(slot);
        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Schedule_SameSlotTwice_Returns409WithSlotCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await CreatePatientAsync(client, token);
        var request = JsonContent.Create(new ScheduleAppointmentRequest(patientId, FutureSlot()));
        await SendAsync(client, HttpMethod.Post, AppointmentsPath, token, request);

        var second = await SendAsync(client, HttpMethod.Post, AppointmentsPath, token, JsonContent.Create(new ScheduleAppointmentRequest(patientId, FutureSlot())));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await second.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("slot_already_booked");
    }

    [Fact]
    public async Task Schedule_SlotNotOnQuarterHour_Returns422WithCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await CreatePatientAsync(client, token);
        var slot = FutureSlot().AddMinutes(7);

        var response = await SendAsync(client, HttpMethod.Post, AppointmentsPath, token, JsonContent.Create(new ScheduleAppointmentRequest(patientId, slot)));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("slot_not_aligned");
    }

    [Fact]
    public async Task Schedule_SlotInThePast_Returns422WithCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var patientId = await CreatePatientAsync(client, token);
        var slot = DateTime.UtcNow.AddDays(-2);
        slot = new DateTime(slot.Year, slot.Month, slot.Day, slot.Hour, slot.Minute / 15 * 15, 0, DateTimeKind.Utc);

        var response = await SendAsync(client, HttpMethod.Post, AppointmentsPath, token, JsonContent.Create(new ScheduleAppointmentRequest(patientId, slot)));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("appointment_in_past");
    }

    [Fact]
    public async Task Schedule_UnknownPatient_Returns404WithCode()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);

        var response = await SendAsync(client, HttpMethod.Post, AppointmentsPath, token, JsonContent.Create(new ScheduleAppointmentRequest(9999, FutureSlot())));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("record_not_found");
    }

    [Fact]
    public async Task Get_UnknownAppointment_Returns404()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);

        var response = await SendAsync(client, HttpMethod.Get, $"{AppointmentsPath}/9999", token, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>A slot three days ahead, on the quarter hour, in UTC.</summary>
    private static DateTime FutureSlot()
    {
        var future = DateTime.UtcNow.AddDays(3);
        return new DateTime(future.Year, future.Month, future.Day, future.Hour, future.Minute / 15 * 15, 0, DateTimeKind.Utc);
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
