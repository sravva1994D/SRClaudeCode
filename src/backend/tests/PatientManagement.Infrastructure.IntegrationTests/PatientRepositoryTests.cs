using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Patients;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class PatientRepositoryTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    private const string CountPatientAuditRowsSql =
        "SELECT COUNT(*) FROM dbo.AuditLogs WHERE TableName = N'Patients' AND Action = N'Create';";

    [Fact]
    public async Task AddAsync_NewPatient_AssignsIdAndWritesCreateAuditRow()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var (unitOfWork, patients) = Create(database);
        var today = new DateOnly(2026, 11, 11);
        var patient = Patient.Register("Asha Rao", new DateOnly(1990, 5, 14), 2, "919876543210", null, today);

        await unitOfWork.ExecuteAsync(token => patients.AddAsync(patient, 1, token), CancellationToken.None);

        patient.PatientId.Should().BeGreaterThan(0);
        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountPatientAuditRowsSql)).Should().Be(1);
    }

    [Fact]
    public async Task SaveAsync_DeactivatedPatient_IsNoLongerFoundAsActive()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var (unitOfWork, patients) = Create(database);
        var today = new DateOnly(2026, 11, 11);
        var patient = Patient.Register("Asha Rao", new DateOnly(1990, 5, 14), 2, null, null, today);
        await unitOfWork.ExecuteAsync(token => patients.AddAsync(patient, 1, token), CancellationToken.None);
        var loaded = await FindAsync(unitOfWork, patients, patient.PatientId);
        var auditValuesBefore = loaded!.AuditValues();
        loaded.Deactivate();

        await unitOfWork.ExecuteAsync(
            token => patients.SaveAsync(loaded, auditValuesBefore, AuditAction.Delete, 1, token),
            CancellationToken.None);

        var found = await FindAsync(unitOfWork, patients, patient.PatientId);
        found.Should().BeNull();
    }

    private static Task<Patient?> FindAsync(IUnitOfWork unitOfWork, IPatientRepository patients, int patientId) =>
        unitOfWork.ExecuteAsync(token => patients.FindActiveAsync(patientId, token), CancellationToken.None);

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
