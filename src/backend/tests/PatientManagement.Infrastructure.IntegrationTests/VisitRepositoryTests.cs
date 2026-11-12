using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Patients;
using PatientManagement.Domain.Visits;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class VisitRepositoryTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    private static readonly DateTime Now = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Slot = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AddAsync_SecondVisitForTheSameAppointment_ThrowsVisitAlreadyStarted()
    {
        await using var database = await TestDatabase.CreateAsync();
        await SetUpAsync(database);
        var (unitOfWork, patients, appointments, visits) = Create(database);
        var patientId = await AddPatientAsync(unitOfWork, patients);
        var appointmentId = await BookAsync(unitOfWork, appointments, patientId);
        await unitOfWork.ExecuteAsync(token => visits.AddAsync(Visit.Start(patientId, appointmentId), 1, token), CancellationToken.None);

        var act = () => unitOfWork.ExecuteAsync(token => visits.AddAsync(Visit.Start(patientId, appointmentId), 1, token), CancellationToken.None);

        await act.Should().ThrowAsync<VisitAlreadyStartedException>();
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

    private static async Task<int> BookAsync(IUnitOfWork unitOfWork, IAppointmentRepository appointments, int patientId)
    {
        var appointment = Appointment.Schedule(patientId, Slot, Now);
        await unitOfWork.ExecuteAsync(token => appointments.AddAsync(appointment, 1, token), CancellationToken.None);
        return appointment.AppointmentId;
    }

    private static (IUnitOfWork UnitOfWork, IPatientRepository Patients, IAppointmentRepository Appointments, IVisitRepository Visits) Create(TestDatabase database)
    {
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        var clock = new SystemClock();
        var audit = new SqlAuditLogWriter(clock, options);
        var connectionFactory = new SqlConnectionFactory(options);
        return (
            new SqlUnitOfWork(connectionFactory),
            new SqlPatientRepository(audit, clock, connectionFactory, options),
            new SqlAppointmentRepository(audit, clock, connectionFactory, options),
            new SqlVisitRepository(audit, clock, connectionFactory, options));
    }
}
