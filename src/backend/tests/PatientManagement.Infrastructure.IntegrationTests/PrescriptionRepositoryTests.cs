using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Patients;
using PatientManagement.Domain.Prescriptions;
using PatientManagement.Domain.Visits;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class PrescriptionRepositoryTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    private static readonly DateTime Now = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Slot = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly IssuedOn = new(2026, 11, 12);

    [Fact]
    public async Task AddAsync_ThenFind_ReturnsTheIssuedPrescriptionAndAuditsTheInsert()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        var clock = new SystemClock();
        var audit = new SqlAuditLogWriter(clock, options);
        var connectionFactory = new SqlConnectionFactory(options);
        var unitOfWork = new SqlUnitOfWork(connectionFactory);
        var visitId = await StartVisitAsync(unitOfWork, connectionFactory, options, audit, clock);
        var prescriptions = new SqlPrescriptionRepository(audit, clock, connectionFactory, options);

        await unitOfWork.ExecuteAsync(
            token => prescriptions.AddAsync(Prescription.Issue(visitId, IssuedOn), 1, token),
            CancellationToken.None);

        var stored = await prescriptions.FindActiveForVisitAsync(visitId, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.PrescriptionDate.Should().Be(IssuedOn);
        var auditRows = await SqlQuery.ScalarIntAsync(
            database.ConnectionString,
            "SELECT COUNT(*) FROM dbo.AuditLogs WHERE TableName = N'Prescriptions' AND Action = N'Create';");
        auditRows.Should().Be(1);
    }

    [Fact]
    public async Task AddAsync_SecondActivePrescriptionForTheVisit_ThrowsConcurrencyConflict()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        var clock = new SystemClock();
        var audit = new SqlAuditLogWriter(clock, options);
        var connectionFactory = new SqlConnectionFactory(options);
        var unitOfWork = new SqlUnitOfWork(connectionFactory);
        var visitId = await StartVisitAsync(unitOfWork, connectionFactory, options, audit, clock);
        var prescriptions = new SqlPrescriptionRepository(audit, clock, connectionFactory, options);
        await unitOfWork.ExecuteAsync(
            token => prescriptions.AddAsync(Prescription.Issue(visitId, IssuedOn), 1, token),
            CancellationToken.None);

        var act = () => unitOfWork.ExecuteAsync(
            token => prescriptions.AddAsync(Prescription.Issue(visitId, IssuedOn), 1, token),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
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
}
