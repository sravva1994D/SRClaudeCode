using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Patients;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class PatientDuplicateRepositoryTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    private static readonly DateOnly Today = new(2026, 11, 11);
    private static readonly DateOnly DateOfBirth = new(1990, 5, 14);

    [Fact]
    public async Task FindActiveDuplicatesAsync_SameNameDifferentCase_ReturnsThePatient()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var (unitOfWork, patients) = Create(database);
        await AddAsync(unitOfWork, patients, "Asha Rao");

        var matches = await patients.FindActiveDuplicatesAsync("ASHA RAO", DateOfBirth, null, 5, CancellationToken.None);

        matches.Should().ContainSingle().Which.FullName.Should().Be("Asha Rao");
    }

    [Fact]
    public async Task FindActiveDuplicatesAsync_DeactivatedPatient_IsNotReturned()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var (unitOfWork, patients) = Create(database);
        var patient = await AddAsync(unitOfWork, patients, "Asha Rao");
        var auditValuesBefore = patient.AuditValues();
        patient.Deactivate();
        await unitOfWork.ExecuteAsync(
            token => patients.SaveAsync(patient, auditValuesBefore, AuditAction.Delete, 1, token),
            CancellationToken.None);

        var matches = await patients.FindActiveDuplicatesAsync("Asha Rao", DateOfBirth, null, 5, CancellationToken.None);

        matches.Should().BeEmpty();
    }

    [Fact]
    public async Task FindActiveDuplicatesAsync_ExcludedPatient_IsNotReturned()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var (unitOfWork, patients) = Create(database);
        var patient = await AddAsync(unitOfWork, patients, "Asha Rao");

        var matches = await patients.FindActiveDuplicatesAsync("Asha Rao", DateOfBirth, patient.PatientId, 5, CancellationToken.None);

        matches.Should().BeEmpty();
    }

    private static async Task<Patient> AddAsync(IUnitOfWork unitOfWork, IPatientRepository patients, string fullName)
    {
        var patient = Patient.Register(fullName, DateOfBirth, 2, "919876543210", null, Today);
        await unitOfWork.ExecuteAsync(token => patients.AddAsync(patient, 1, token), CancellationToken.None);
        return patient;
    }

    private static (IUnitOfWork UnitOfWork, IPatientRepository Patients) Create(TestDatabase database)
    {
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        var clock = new SystemClock();
        var audit = new SqlAuditLogWriter(clock, options);
        var connectionFactory = new SqlConnectionFactory(options);
        return (
            new SqlUnitOfWork(connectionFactory),
            new SqlPatientRepository(audit, clock, connectionFactory, options));
    }
}
