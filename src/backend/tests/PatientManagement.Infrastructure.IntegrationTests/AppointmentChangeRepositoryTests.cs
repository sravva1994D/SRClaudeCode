using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Patients;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class AppointmentChangeRepositoryTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    private static readonly DateTime Now = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SlotA = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SlotB = new(2026, 11, 10, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SaveAsync_RescheduleIntoAnotherBookingsSlot_ThrowsSlotAlreadyBooked()
    {
        await using var database = await TestDatabase.CreateAsync();
        await SetUpAsync(database);
        var (unitOfWork, patients, appointments) = Create(database);
        var patientId = await AddPatientAsync(unitOfWork, patients);
        var moving = await BookAsync(unitOfWork, appointments, patientId, SlotA);
        await BookAsync(unitOfWork, appointments, patientId, SlotB);

        var act = () => MoveAsync(unitOfWork, appointments, moving.AppointmentId, SlotB);

        await act.Should().ThrowAsync<SlotAlreadyBookedException>();
    }

    [Fact]
    public async Task SaveAsync_Reschedule_FreesTheOldSlot()
    {
        await using var database = await TestDatabase.CreateAsync();
        await SetUpAsync(database);
        var (unitOfWork, patients, appointments) = Create(database);
        var patientId = await AddPatientAsync(unitOfWork, patients);
        var moving = await BookAsync(unitOfWork, appointments, patientId, SlotA);

        await MoveAsync(unitOfWork, appointments, moving.AppointmentId, SlotB);
        var rebooked = await BookAsync(unitOfWork, appointments, patientId, SlotA);

        rebooked.AppointmentId.Should().BeGreaterThan(moving.AppointmentId);
    }

    [Fact]
    public async Task SaveAsync_Cancel_FreesTheSlot()
    {
        await using var database = await TestDatabase.CreateAsync();
        await SetUpAsync(database);
        var (unitOfWork, patients, appointments) = Create(database);
        var patientId = await AddPatientAsync(unitOfWork, patients);
        var booked = await BookAsync(unitOfWork, appointments, patientId, SlotA);

        await unitOfWork.ExecuteAsync(async token =>
        {
            var loaded = await appointments.FindActiveAsync(booked.AppointmentId, token);
            var before = loaded!.AuditValues();
            loaded.ChangeStatus(AppointmentStatuses.Cancelled);
            await appointments.SaveAsync(loaded, before, AuditAction.Update, 1, token);
        }, CancellationToken.None);
        var rebooked = await BookAsync(unitOfWork, appointments, patientId, SlotA);

        rebooked.AppointmentId.Should().BeGreaterThan(booked.AppointmentId);
    }

    private static async Task SetUpAsync(TestDatabase database)
    {
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
    }

    private static async Task<int> AddPatientAsync(IUnitOfWork unitOfWork, IPatientRepository patients)
    {
        var patient = Patient.Register("Asha Rao", new DateOnly(1990, 5, 14), 2, null, null, new DateOnly(2026, 11, 9));
        await unitOfWork.ExecuteAsync(token => patients.AddAsync(patient, 1, token), CancellationToken.None);
        return patient.PatientId;
    }

    private static async Task<Appointment> BookAsync(IUnitOfWork unitOfWork, IAppointmentRepository appointments, int patientId, DateTime slot)
    {
        var appointment = Appointment.Schedule(patientId, slot, Now);
        await unitOfWork.ExecuteAsync(token => appointments.AddAsync(appointment, 1, token), CancellationToken.None);
        return appointment;
    }

    private static Task MoveAsync(IUnitOfWork unitOfWork, IAppointmentRepository appointments, int appointmentId, DateTime slot) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var loaded = await appointments.FindActiveAsync(appointmentId, token);
            var before = loaded!.AuditValues();
            loaded.Reschedule(slot, Now);
            await appointments.SaveAsync(loaded, before, AuditAction.Update, 1, token);
        }, CancellationToken.None);

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
