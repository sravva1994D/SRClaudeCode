using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using PatientManagement.API.Auth;
using PatientManagement.API.Controllers;
using PatientManagement.API.IntegrationTests.Support;

namespace PatientManagement.API.IntegrationTests;

public sealed class MedicationsEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Passw0rd!";
    private const string PatientsPath = "/api/v1/patients";
    private const string AppointmentsPath = "/api/v1/appointments";
    private const string VisitsPath = "/api/v1/visits";
    private const string SuggestionsPath = "/api/v1/medicines/suggestions";

    [Fact]
    public async Task SaveMedications_ValidList_IsReturnedByNotes()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var visitId = await StartVisitAsync(client, token);

        var saved = await SendAsync(client, HttpMethod.Put, $"{VisitsPath}/{visitId}/medications", token,
            JsonContent.Create(new SaveMedicationsBody([new MedicationBody("Paracetamol", "500mg", "Twice daily", "7 days", "After food")])));

        saved.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var notes = await SendAsync(client, HttpMethod.Get, $"{VisitsPath}/{visitId}/notes", token, content: null);
        (await notes.Content.ReadFromJsonAsync<NotesBody>())!.Medications.Should()
            .ContainSingle().Which.Should().BeEquivalentTo(new MedicationBody("Paracetamol", "500mg", "Twice daily", "7 days", "After food"));
    }

    [Fact]
    public async Task SaveMedications_MedicineMissingDosage_Returns422()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var visitId = await StartVisitAsync(client, token);

        var response = await SendAsync(client, HttpMethod.Put, $"{VisitsPath}/{visitId}/medications", token,
            JsonContent.Create(new SaveMedicationsBody([new MedicationBody("Paracetamol", "", "Twice daily", "7 days", null)])));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Suggestions_ReturnsNamesFromTheDoctorsOwnHistory()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var visitId = await StartVisitAsync(client, token);
        await SendAsync(client, HttpMethod.Put, $"{VisitsPath}/{visitId}/medications", token,
            JsonContent.Create(new SaveMedicationsBody([new MedicationBody("Paracetamol", "500mg", "Twice daily", "7 days", null)])));

        var response = await SendAsync(client, HttpMethod.Get, $"{SuggestionsPath}?search=Par", token, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<List<string>>()).Should().Equal("Paracetamol");
    }

    [Fact]
    public async Task Suggestions_LimitOutOfRange_Returns400()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);

        var response = await SendAsync(client, HttpMethod.Get, $"{SuggestionsPath}?search=Par&limit=500", token, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
        started.StatusCode.Should().Be(HttpStatusCode.Created, "a visit must be in progress before medications are saved");
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
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the sign-in must succeed before medications can be saved");
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

    private sealed record MedicationBody(string? MedicineName, string? Dosage, string? Frequency, string? Duration, string? SpecialInstructions);

    private sealed record SaveMedicationsBody(IReadOnlyList<MedicationBody> Medications);

    private sealed record NotesBody(int VisitId, IReadOnlyList<string> Complaints, IReadOnlyList<object> Diagnoses, IReadOnlyList<MedicationBody> Medications);
}
