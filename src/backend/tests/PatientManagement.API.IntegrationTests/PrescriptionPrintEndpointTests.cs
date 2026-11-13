using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using PatientManagement.API.Auth;
using PatientManagement.API.Controllers;
using PatientManagement.API.IntegrationTests.Support;

namespace PatientManagement.API.IntegrationTests;

public sealed class PrescriptionPrintEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Passw0rd!";
    private const string PatientsPath = "/api/v1/patients";
    private const string AppointmentsPath = "/api/v1/appointments";
    private const string VisitsPath = "/api/v1/visits";

    [Fact]
    public async Task Reprint_AfterTheMedicineChanges_ShowsWhatWasPrintedWhileTheCurrentPrescriptionShowsTheChange()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var visitId = await StartVisitAsync(client, token);
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/vitals", token,
            JsonContent.Create(new RecordVitalsRequest(36.6m, 120, 80, 72)));
        await SaveMedicinesAsync(client, token, visitId, "Paracetamol");
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/complete", token, content: null);
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/prescription", token, content: null);

        var printed = await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/prescription/prints", token, content: null);
        var printId = (await printed.Content.ReadFromJsonAsync<PrintBody>())!.PrescriptionPrintId;
        await SaveMedicinesAsync(client, token, visitId, "Amoxicillin");

        var reprint = await SendAsync(client, HttpMethod.Get, $"{VisitsPath}/{visitId}/prescription/prints/{printId}", token, content: null);
        var current = await SendAsync(client, HttpMethod.Get, $"{VisitsPath}/{visitId}/prescription", token, content: null);

        printed.StatusCode.Should().Be(HttpStatusCode.Created);
        reprint.StatusCode.Should().Be(HttpStatusCode.OK);
        var printedDocument = (await reprint.Content.ReadFromJsonAsync<PrintBody>())!.Document;
        printedDocument.Medications.Select(medication => medication.MedicineName).Should().Equal("Paracetamol");
        var currentDocument = (await current.Content.ReadFromJsonAsync<DocumentBody>())!;
        currentDocument.Medications.Select(medication => medication.MedicineName).Should().Equal("Amoxicillin");
    }

    [Fact]
    public async Task ListPrints_AfterTwoPrints_ReturnsBothInOrder_AndAnUnknownPrintIsNotFound()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        using var client = await StartAsync(database);
        var token = await SignInAsync(client);
        var visitId = await StartVisitAsync(client, token);
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/vitals", token,
            JsonContent.Create(new RecordVitalsRequest(36.6m, 120, 80, 72)));
        await SaveMedicinesAsync(client, token, visitId, "Paracetamol");
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/complete", token, content: null);
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/prescription", token, content: null);
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/prescription/prints", token, content: null);
        await SendAsync(client, HttpMethod.Post, $"{VisitsPath}/{visitId}/prescription/prints", token, content: null);

        var list = await SendAsync(client, HttpMethod.Get, $"{VisitsPath}/{visitId}/prescription/prints", token, content: null);
        var unknown = await SendAsync(client, HttpMethod.Get, $"{VisitsPath}/{visitId}/prescription/prints/999999", token, content: null);

        var prints = (await list.Content.ReadFromJsonAsync<List<SummaryBody>>())!;
        prints.Should().HaveCount(2);
        prints[0].PrintedAt.Should().BeOnOrBefore(prints[1].PrintedAt);
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task SaveMedicinesAsync(HttpClient client, string token, int visitId, string medicine)
    {
        var saved = await SendAsync(client, HttpMethod.Put, $"{VisitsPath}/{visitId}/medications", token,
            JsonContent.Create(new SaveMedicationsRequest(
                [new MedicationEntryRequest(medicine, "500mg", "Twice daily", "7 days", null)])));
        saved.StatusCode.Should().Be(HttpStatusCode.NoContent);
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
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the sign-in must succeed before a print can be recorded");
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

    private sealed record PrintBody(int PrescriptionPrintId, DateTime PrintedAt, DocumentBody Document);

    private sealed record SummaryBody(int PrescriptionPrintId, DateTime PrintedAt);

    private sealed record DocumentBody(IReadOnlyList<MedicationLineBody> Medications);

    private sealed record MedicationLineBody(string MedicineName);
}
