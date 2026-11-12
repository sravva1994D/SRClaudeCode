using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Patients;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Milestone M3 gate (plan Sprint 5–6): double-booking is prevented. Several requests race for the same slot at the same
/// time; the database's unique index must let exactly one of them through.
/// </summary>
public sealed class AppointmentConcurrencyTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    private const string CountActiveBookingsSql =
        "SELECT COUNT(*) FROM dbo.Appointments WHERE SlotStart = '2026-11-10T09:00:00' AND IsActive = 1 AND AppointmentStatusId <> 3;";

    private const int RacingRequests = 8;

    private static readonly DateTime Now = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Slot = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AddAsync_ManySimultaneousBookingsForOneSlot_ExactlyOneSucceeds()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        var clock = new SystemClock();
        var audit = new SqlAuditLogWriter(clock, options);
        var connectionFactory = new SqlConnectionFactory(options);
        var patientId = await AddPatientAsync(new SqlUnitOfWork(connectionFactory), new SqlPatientRepository(audit, clock, connectionFactory, options));

        var attempts = Enumerable.Range(0, RacingRequests).Select(_ => TryBookAsync(options, connectionFactory, audit, clock, patientId));
        var outcomes = await Task.WhenAll(attempts);

        outcomes.Count(outcome => outcome == Outcome.Booked).Should().Be(1, "only one request may hold the slot");
        outcomes.Count(outcome => outcome == Outcome.SlotTaken).Should().Be(RacingRequests - 1);
        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountActiveBookingsSql)).Should().Be(1);
    }

    private enum Outcome
    {
        Booked,
        SlotTaken,
    }

    private static async Task<Outcome> TryBookAsync(
        IOptions<DatabaseOptions> options,
        ISqlConnectionFactory connectionFactory,
        IAuditLogWriter audit,
        IClock clock,
        int patientId)
    {
        var unitOfWork = new SqlUnitOfWork(connectionFactory);
        var appointments = new SqlAppointmentRepository(audit, clock, connectionFactory, options);
        try
        {
            await unitOfWork.ExecuteAsync(
                token => appointments.AddAsync(Appointment.Schedule(patientId, Slot, Now), 1, token),
                CancellationToken.None);
            return Outcome.Booked;
        }
        catch (SlotAlreadyBookedException)
        {
            return Outcome.SlotTaken;
        }
    }

    private static async Task<int> AddPatientAsync(IUnitOfWork unitOfWork, IPatientRepository patients)
    {
        var patient = Patient.Register("Asha Rao", new DateOnly(1990, 5, 14), 2, null, null, new DateOnly(2026, 11, 9));
        await unitOfWork.ExecuteAsync(token => patients.AddAsync(patient, 1, token), CancellationToken.None);
        return patient.PatientId;
    }
}
