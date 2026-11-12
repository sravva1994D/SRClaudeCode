using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Consultation;
using PatientManagement.Domain.Patients;
using PatientManagement.Domain.Visits;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class MedicationRepositoryTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    private static readonly DateTime Now = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Slot = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task FindNamesStartingWithAsync_PrefixMatch_ReturnsDistinctNamesAlphabetically()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var setup = CreateSetup(database);
        var visitId = await StartVisitAsync(setup);
        await AddAsync(setup, visitId, "Paracetamol", 0);
        await AddAsync(setup, visitId, "Paracetamol", 1);
        await AddAsync(setup, visitId, "Pantoprazole", 2);
        await AddAsync(setup, visitId, "Amoxicillin", 3);
        var suggestions = new SqlMedicineSuggestionRepository(setup.ConnectionFactory, setup.Options);

        var names = await suggestions.FindNamesStartingWithAsync("Pa", 10, CancellationToken.None);

        names.Should().Equal("Pantoprazole", "Paracetamol");
    }

    [Fact]
    public async Task FindNamesStartingWithAsync_SearchWithWildcards_MatchesTheCharactersLiterally()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var setup = CreateSetup(database);
        var visitId = await StartVisitAsync(setup);
        await AddAsync(setup, visitId, "5% Dextrose", 0);
        await AddAsync(setup, visitId, "Paracetamol", 1);
        var suggestions = new SqlMedicineSuggestionRepository(setup.ConnectionFactory, setup.Options);

        (await suggestions.FindNamesStartingWithAsync("5%", 10, CancellationToken.None)).Should().Equal("5% Dextrose");
        (await suggestions.FindNamesStartingWithAsync("%", 10, CancellationToken.None)).Should().BeEmpty();
    }

    [Fact]
    public async Task FindNamesStartingWithAsync_DeactivatedMedication_IsNotSuggested()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var setup = CreateSetup(database);
        var visitId = await StartVisitAsync(setup);
        var medication = Medication.Record(visitId, "Pantoprazole", "40mg", "Once daily", "5 days", null, 0);
        var medications = new SqlMedicationRepository(setup.Audit, setup.Clock, setup.ConnectionFactory, setup.Options);
        await setup.UnitOfWork.ExecuteAsync(token => medications.AddAsync(medication, 1, token), CancellationToken.None);
        await setup.UnitOfWork.ExecuteAsync(token => medications.DeactivateAsync(medication, 1, token), CancellationToken.None);
        var suggestions = new SqlMedicineSuggestionRepository(setup.ConnectionFactory, setup.Options);

        (await suggestions.FindNamesStartingWithAsync("Pa", 10, CancellationToken.None)).Should().BeEmpty();
    }

    private static async Task AddAsync(Setup setup, int visitId, string name, int sortOrder)
    {
        var medications = new SqlMedicationRepository(setup.Audit, setup.Clock, setup.ConnectionFactory, setup.Options);
        var medication = Medication.Record(visitId, name, "500mg", "Twice daily", "7 days", null, sortOrder);
        await setup.UnitOfWork.ExecuteAsync(token => medications.AddAsync(medication, 1, token), CancellationToken.None);
    }

    private static Setup CreateSetup(TestDatabase database)
    {
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        var clock = new SystemClock();
        var audit = new SqlAuditLogWriter(clock, options);
        var connectionFactory = new SqlConnectionFactory(options);
        return new Setup(options, clock, audit, connectionFactory, new SqlUnitOfWork(connectionFactory));
    }

    private static async Task<int> StartVisitAsync(Setup setup)
    {
        var patients = new SqlPatientRepository(setup.Audit, setup.Clock, setup.ConnectionFactory, setup.Options);
        var appointments = new SqlAppointmentRepository(setup.Audit, setup.Clock, setup.ConnectionFactory, setup.Options);
        var visits = new SqlVisitRepository(setup.Audit, setup.Clock, setup.ConnectionFactory, setup.Options);

        var patient = Patient.Register("Asha Rao", new DateOnly(1990, 5, 14), 2, null, null, new DateOnly(2026, 11, 9));
        await setup.UnitOfWork.ExecuteAsync(token => patients.AddAsync(patient, 1, token), CancellationToken.None);
        var appointment = Appointment.Schedule(patient.PatientId, Slot, Now);
        await setup.UnitOfWork.ExecuteAsync(token => appointments.AddAsync(appointment, 1, token), CancellationToken.None);
        var visit = Visit.Start(patient.PatientId, appointment.AppointmentId);
        await setup.UnitOfWork.ExecuteAsync(token => visits.AddAsync(visit, 1, token), CancellationToken.None);
        return visit.VisitId;
    }

    private sealed record Setup(
        IOptions<DatabaseOptions> Options,
        IClock Clock,
        IAuditLogWriter Audit,
        ISqlConnectionFactory ConnectionFactory,
        IUnitOfWork UnitOfWork);
}
