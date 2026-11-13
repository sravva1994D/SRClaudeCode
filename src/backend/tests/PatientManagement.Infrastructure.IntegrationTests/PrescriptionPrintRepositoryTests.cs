using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Prescriptions;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Patients;
using PatientManagement.Domain.Prescriptions;
using PatientManagement.Domain.Visits;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class PrescriptionPrintRepositoryTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    private static readonly DateTime Now = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Slot = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AddAsync_ThenList_ReturnsTheRecordedPrintsOldestFirst()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var setup = await CreateIssuedPrescriptionAsync(database);
        var prints = new SqlPrescriptionPrintRepository(setup.ConnectionFactory, setup.Options);

        await setup.UnitOfWork.ExecuteAsync(async token =>
        {
            await prints.AddAsync(PrescriptionPrint.Record(setup.PrescriptionId, Now, "{\"first\":true}"), 1, token);
            await prints.AddAsync(PrescriptionPrint.Record(setup.PrescriptionId, Now.AddMinutes(5), "{\"second\":true}"), 1, token);
        }, CancellationToken.None);

        var listed = await prints.ListForPrescriptionAsync(setup.PrescriptionId, CancellationToken.None);
        listed.Select(print => print.PrintedAt).Should().Equal(Now, Now.AddMinutes(5));
    }

    [Fact]
    public async Task FindAsync_StoredPrint_ReturnsTheSnapshotThatWasRecorded()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var setup = await CreateIssuedPrescriptionAsync(database);
        var prints = new SqlPrescriptionPrintRepository(setup.ConnectionFactory, setup.Options);
        var print = PrescriptionPrint.Record(setup.PrescriptionId, Now, "{\"medications\":[\"Paracetamol\"]}");
        await setup.UnitOfWork.ExecuteAsync(token => prints.AddAsync(print, 1, token), CancellationToken.None);

        var found = await prints.FindAsync(setup.PrescriptionId, print.PrescriptionPrintId, CancellationToken.None);

        found.Should().NotBeNull();
        found!.SnapshotJson.Should().Be("{\"medications\":[\"Paracetamol\"]}");
        found.PrintedAt.Should().Be(Now);
    }

    [Fact]
    public async Task FindAsync_PrintIdOfAnotherPrescription_ReturnsNull()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var setup = await CreateIssuedPrescriptionAsync(database);
        var prints = new SqlPrescriptionPrintRepository(setup.ConnectionFactory, setup.Options);
        var print = PrescriptionPrint.Record(setup.PrescriptionId, Now, "{}");
        await setup.UnitOfWork.ExecuteAsync(token => prints.AddAsync(print, 1, token), CancellationToken.None);

        var found = await prints.FindAsync(setup.PrescriptionId + 1000, print.PrescriptionPrintId, CancellationToken.None);

        found.Should().BeNull();
    }

    private static async Task<IssuedSetup> CreateIssuedPrescriptionAsync(TestDatabase database)
    {
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        var clock = new SystemClock();
        var audit = new SqlAuditLogWriter(clock, options);
        var connectionFactory = new SqlConnectionFactory(options);
        var unitOfWork = new SqlUnitOfWork(connectionFactory);
        var visitId = await StartVisitAsync(unitOfWork, connectionFactory, options, audit, clock);
        var prescriptions = new SqlPrescriptionRepository(audit, clock, connectionFactory, options);
        var prescription = Prescription.Issue(visitId, new DateOnly(2026, 11, 9));
        await unitOfWork.ExecuteAsync(token => prescriptions.AddAsync(prescription, 1, token), CancellationToken.None);
        return new IssuedSetup(unitOfWork, connectionFactory, options, prescription.PrescriptionId);
    }

    private static async Task<int> StartVisitAsync(
        IUnitOfWork unitOfWork,
        ISqlConnectionFactory connectionFactory,
        IOptions<DatabaseOptions> options,
        IAuditLogWriter audit,
        IClock clock)
    {
        var patients = new SqlPatientRepository(audit, clock, connectionFactory, options);
        var appointments = new SqlAppointmentRepository(audit, clock, connectionFactory, options);
        var visits = new SqlVisitRepository(audit, clock, connectionFactory, options);

        var patient = Patient.Register("Asha Rao", new DateOnly(1990, 5, 14), 2, null, null, new DateOnly(2026, 11, 9));
        await unitOfWork.ExecuteAsync(token => patients.AddAsync(patient, 1, token), CancellationToken.None);
        var appointment = Appointment.Schedule(patient.PatientId, Slot, Now);
        await unitOfWork.ExecuteAsync(token => appointments.AddAsync(appointment, 1, token), CancellationToken.None);
        var visit = Visit.Start(patient.PatientId, appointment.AppointmentId);
        await unitOfWork.ExecuteAsync(token => visits.AddAsync(visit, 1, token), CancellationToken.None);
        return visit.VisitId;
    }

    private sealed record IssuedSetup(
        IUnitOfWork UnitOfWork,
        ISqlConnectionFactory ConnectionFactory,
        IOptions<DatabaseOptions> Options,
        int PrescriptionId);
}
