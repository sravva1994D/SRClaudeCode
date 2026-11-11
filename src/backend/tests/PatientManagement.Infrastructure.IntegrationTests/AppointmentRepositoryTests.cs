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

public sealed class AppointmentRepositoryTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    private static readonly DateTime Now = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AddAsync_SecondActiveBookingForTheSameSlot_ThrowsSlotAlreadyBooked()
    {
        await using var database = await TestDatabase.CreateAsync();
        await SetUpAsync(database);
        var (unitOfWork, patients, appointments) = Create(database);
        var patientId = await AddPatientAsync(unitOfWork, patients);
        var slot = new DateTime(2026, 11, 11, 10, 15, 0, DateTimeKind.Utc);
        await unitOfWork.ExecuteAsync(
            token => appointments.AddAsync(Appointment.Schedule(patientId, slot, Now), 1, token),
            CancellationToken.None);

        var act = () => unitOfWork.ExecuteAsync(
            token => appointments.AddAsync(Appointment.Schedule(patientId, slot, Now), 1, token),
            CancellationToken.None);

        await act.Should().ThrowAsync<SlotAlreadyBookedException>();
    }

    [Fact]
    public async Task AddAsync_DifferentSlots_BothAreStored()
    {
        await using var database = await TestDatabase.CreateAsync();
        await SetUpAsync(database);
        var (unitOfWork, patients, appointments) = Create(database);
        var patientId = await AddPatientAsync(unitOfWork, patients);
        var first = Appointment.Schedule(patientId, new DateTime(2026, 11, 11, 10, 15, 0, DateTimeKind.Utc), Now);
        var second = Appointment.Schedule(patientId, new DateTime(2026, 11, 11, 10, 30, 0, DateTimeKind.Utc), Now);

        await unitOfWork.ExecuteAsync(token => appointments.AddAsync(first, 1, token), CancellationToken.None);
        await unitOfWork.ExecuteAsync(token => appointments.AddAsync(second, 1, token), CancellationToken.None);

        first.AppointmentId.Should().BeGreaterThan(0);
        second.AppointmentId.Should().BeGreaterThan(first.AppointmentId);
    }

    private static async Task SetUpAsync(TestDatabase database)
    {
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
    }

    private static async Task<int> AddPatientAsync(IUnitOfWork unitOfWork, IPatientRepository patients)
    {
        var patient = Patient.Register("Asha Rao", new DateOnly(1990, 5, 14), 2, null, null, new DateOnly(2026, 11, 11));
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
