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

public sealed class PrescriptionEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Passw0rd!";
    private const string PatientsPath = "/api/v1/patients";
    private const string AppointmentsPath = "/api/v1/appointments";
    private const string VisitsPath = "/api/v1/visits";

    [Fact]
    public async Task GeneratePrescription_VisitStillInProgress_Returns422AndIssuesNothing()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var visitId = await StartVisitAsync(client, token);

        var response = await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/prescription", token, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("visit_not_completed");
    }

    [Fact]
    public async Task GeneratePrescription_CompletedVisitWithoutMedications_Returns422()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var visitId = await StartVisitAsync(client, token);
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/vitals", token,
            JsonContent.Create(new RecordVitalsRequest(36.6m, 120, 80, 72)));
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/complete", token, content: null);

        var response = await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/prescription", token, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Code.Should().Be("prescription_no_medications");
    }

    [Fact]
    public async Task GeneratePrescription_CompletedVisitWithMedication_IssuesOnceThenReturnsTheSameOne()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var visitId = await StartVisitAsync(client, token);
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/vitals", token,
            JsonContent.Create(new RecordVitalsRequest(36.6m, 120, 80, 72)));
        await SendAsync(client, HttpMethod.Put, $"{VisitsPath}/{visitId}/medications", token,
            JsonContent.Create(new SaveMedicationsRequest(
                [new MedicationEntryRequest("Paracetamol", "500mg", "Twice daily", "7 days", "After food")])));
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/complete", token, content: null);

        var issued = await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/prescription", token, content: null);
        var again = await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/prescription", token, content: null);

        issued.StatusCode.Should().Be(HttpStatusCode.Created);
        again.StatusCode.Should().Be(HttpStatusCode.OK);
        var first = (await issued.Content.ReadFromJsonAsync<PrescriptionBody>())!;
        var second = (await again.Content.ReadFromJsonAsync<PrescriptionBody>())!;
        second.PrescriptionId.Should().Be(first.PrescriptionId);
    }

    [Fact]
    public async Task GetPrescription_BeforeIssue_Returns404_AndAfterIssue_ShowsTheMedicationsAndVitals()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var visitId = await StartVisitAsync(client, token);
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/vitals", token,
            JsonContent.Create(new RecordVitalsRequest(36.6m, 120, 80, 72)));
        await SendAsync(client, HttpMethod.Put, $"{VisitsPath}/{visitId}/medications", token,
            JsonContent.Create(new SaveMedicationsRequest(
                [new MedicationEntryRequest("Paracetamol", "500mg", "Twice daily", "7 days", null)])));
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/complete", token, content: null);

        var beforeIssue = await SendAsync(client, HttpMethod.Get, $"{VisitsPath}/{visitId}/prescription", token, content: null);
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/prescription", token, content: null);
        var afterIssue = await SendAsync(client, HttpMethod.Get, $"{VisitsPath}/{visitId}/prescription", token, content: null);

        beforeIssue.StatusCode.Should().Be(HttpStatusCode.NotFound);
        afterIssue.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = (await afterIssue.Content.ReadFromJsonAsync<DocumentBody>())!;
        document.Medications.Should().ContainSingle(medication => medication.MedicineName == "Paracetamol");
        document.Vitals!.Pulse.Should().Be(72);
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
        started.StatusCode.Should().Be(HttpStatusCode.Created, "a visit must be started before it can be completed");
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
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the sign-in must succeed before a prescription can be issued");
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

    private sealed record PrescriptionBody(int PrescriptionId, int VisitId, DateOnly PrescriptionDate);

    private sealed record DocumentBody(
        IReadOnlyList<MedicationLineBody> Medications,
        VitalsLineBody? Vitals);

    private sealed record MedicationLineBody(string MedicineName);

    private sealed record VitalsLineBody(int? Pulse);
}
