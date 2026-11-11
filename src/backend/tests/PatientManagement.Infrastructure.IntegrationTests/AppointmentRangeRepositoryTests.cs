using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Patients;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class AppointmentRangeRepositoryTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    private static readonly DateTime Now = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ListActiveBetweenAsync_ReturnsOnlyTheRangeInSlotOrderWithTheName()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var (unitOfWork, patients, appointments) = Create(database);
        var asha = await AddPatientAsync(unitOfWork, patients, "Asha Rao");
        var vikram = await AddPatientAsync(unitOfWork, patients, "Vikram Shah");
        await BookAsync(unitOfWork, appointments, vikram, new DateTime(2026, 11, 10, 9, 30, 0, DateTimeKind.Utc));
        await BookAsync(unitOfWork, appointments, asha, new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc));
        await BookAsync(unitOfWork, appointments, asha, new DateTime(2026, 11, 12, 9, 0, 0, DateTimeKind.Utc));

        var items = await appointments.ListActiveBetweenAsync(
            new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        items.Select(item => item.PatientFullName).Should().Equal("Asha Rao", "Vikram Shah");
        items.Select(item => item.SlotStartUtc.Kind).Should().OnlyContain(kind => kind == DateTimeKind.Utc);
    }

    private static async Task BookAsync(IUnitOfWork unitOfWork, IAppointmentRepository appointments, int patientId, DateTime slot) =>
        await unitOfWork.ExecuteAsync(
            token => appointments.AddAsync(Appointment.Schedule(patientId, slot, Now), 1, token),
            CancellationToken.None);

    private static async Task<int> AddPatientAsync(IUnitOfWork unitOfWork, IPatientRepository patients, string fullName)
    {
        var patient = Patient.Register(fullName, new DateOnly(1990, 5, 14), 2, null, null, new DateOnly(2026, 11, 9));
        await unitOfWork.ExecuteAsync(token => patients.AddAsync(patient, 1, token), CancellationToken.None);
        return patient.PatientId;
    }

    private static (IUnitOfWork UnitOfWork, IPatientRepository Patients, IAppointmentRepository Appointments) Create(TestDatabase database)
    {
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        var clock = new SystemClock();
        var audit = new SqlAuditLogWriter(clock, options);
        var connectionFactory = new SqlConnectionFactory(options);
        return (
            new SqlUnitOfWork(connectionFactory),
            new SqlPatientRepository(audit, clock, connectionFactory, options),
            new SqlAppointmentRepository(audit, clock, connectionFactory, options));
    }
}
