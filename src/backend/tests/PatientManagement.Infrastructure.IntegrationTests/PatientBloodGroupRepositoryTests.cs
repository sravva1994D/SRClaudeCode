using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Patients;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class PatientBloodGroupRepositoryTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    [Fact]
    public async Task AddAsync_BloodGroupAndChronicConditions_ReadBackUnchanged()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var (unitOfWork, patients) = Create(database);
        var patient = Patient.Register(
            "Asha Rao",
            new DateOnly(1990, 5, 14),
            2,
            null,
            null,
            new DateOnly(2026, 11, 11),
            bloodGroupId: 3,
            chronicConditions: "Type 2 diabetes; hypertension");

        await unitOfWork.ExecuteAsync(token => patients.AddAsync(patient, 1, token), CancellationToken.None);
        var stored = await unitOfWork.ExecuteAsync(token => patients.FindActiveAsync(patient.PatientId, token), CancellationToken.None);

        stored!.BloodGroupId.Should().Be(3);
        stored.ChronicConditions.Should().Be("Type 2 diabetes; hypertension");
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
