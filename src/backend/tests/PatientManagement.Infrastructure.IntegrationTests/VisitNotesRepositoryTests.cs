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

public sealed class VisitNotesRepositoryTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    private static readonly DateTime Now = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Slot = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ListActiveForVisitAsync_ComplaintsAddedOutOfOrder_ReturnsThemInSortOrder()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var setup = CreateSetup(database);
        var visitId = await StartVisitAsync(setup);
        var complaints = new SqlComplaintRepository(setup.Audit, setup.Clock, setup.ConnectionFactory, setup.Options);

        await setup.UnitOfWork.ExecuteAsync(
            token => complaints.AddAsync(Complaint.Record(visitId, "Fever", 1), 1, token),
            CancellationToken.None);
        await setup.UnitOfWork.ExecuteAsync(
            token => complaints.AddAsync(Complaint.Record(visitId, "Cough", 0), 1, token),
            CancellationToken.None);

        var listed = await complaints.ListActiveForVisitAsync(visitId, CancellationToken.None);

        listed.Select(complaint => complaint.Text).Should().Equal("Cough", "Fever");
    }

    [Fact]
    public async Task DeactivateAsync_ReplacedComplaint_LeavesTheListAndIsAudited()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var setup = CreateSetup(database);
        var visitId = await StartVisitAsync(setup);
        var complaints = new SqlComplaintRepository(setup.Audit, setup.Clock, setup.ConnectionFactory, setup.Options);
        var cough = Complaint.Record(visitId, "Cough", 0);
        await setup.UnitOfWork.ExecuteAsync(token => complaints.AddAsync(cough, 1, token), CancellationToken.None);

        await setup.UnitOfWork.ExecuteAsync(token => complaints.DeactivateAsync(cough, 1, token), CancellationToken.None);

        (await complaints.ListActiveForVisitAsync(visitId, CancellationToken.None)).Should().BeEmpty();
        var auditRows = await SqlQuery.ScalarIntAsync(
            database.ConnectionString,
            "SELECT COUNT(*) FROM dbo.AuditLogs WHERE TableName = N'Complaints';");
        auditRows.Should().Be(2, "one row for the insert and one for the deactivation");
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
