using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Patients;
using PatientManagement.Domain.Visits;
using PatientManagement.Domain.Consultation;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class VitalsRepositoryTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    // A test-only range row, so the range read can be checked. It is not a clinical reference value.
    private const string InsertTestPulseRangeSql =
        "INSERT INTO dbo.VitalRanges (VitalRangeId, VitalSign, Unit, NormalMinimum, NormalMaximum, PlausibleMinimum, PlausibleMaximum) " +
        "VALUES (1, N'Pulse', N'bpm', 60, 100, 20, 250);";

    private static readonly DateTime Now = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Slot = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AddAsync_SecondVitalsForTheSameVisit_ThrowsVitalsAlreadyRecorded()
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
        var vitals = new SqlVitalsRepository(audit, clock, connectionFactory, options);
        await unitOfWork.ExecuteAsync(
            token => vitals.AddAsync(Vitals.Record(visitId, 36.6m, 120, 80, 72, NoRanges(), out _), 1, token),
            CancellationToken.None);

        var act = () => unitOfWork.ExecuteAsync(
            token => vitals.AddAsync(Vitals.Record(visitId, 37m, 118, 78, 70, NoRanges(), out _), 1, token),
            CancellationToken.None);

        await act.Should().ThrowAsync<VitalsAlreadyRecordedException>();
    }

    [Fact]
    public async Task GetActiveAsync_TestRangeRow_IsReadBack()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertTestPulseRangeSql);
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        var ranges = new SqlVitalRangeRepository(new SqlConnectionFactory(options), options);

        var configured = await ranges.GetActiveAsync(CancellationToken.None);

        configured.Should().ContainKey(VitalSigns.Pulse);
        configured[VitalSigns.Pulse].PlausibleMaximum.Should().Be(250);
    }

    private static IReadOnlyDictionary<string, VitalRange> NoRanges() => new Dictionary<string, VitalRange>();

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
