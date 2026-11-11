using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Patients;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class RecentPatientRepositoryTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    private const string CountRecentRowsSql = "SELECT COUNT(*) FROM dbo.RecentPatients;";

    private static readonly DateOnly Today = new(2026, 11, 11);
    private static readonly DateTime BaseTime = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RecordViewAsync_MoreThanTheKeepCount_KeepsOnlyTheNewest()
    {
        await using var database = await TestDatabase.CreateAsync();
        await SetUpAsync(database);
        var (unitOfWork, patients, recents, search) = Create(database);
        var first = await AddPatientAsync(unitOfWork, patients, "Asha Rao");
        var second = await AddPatientAsync(unitOfWork, patients, "Bina Das");
        var third = await AddPatientAsync(unitOfWork, patients, "Chetan Iyer");

        await RecordAsync(unitOfWork, recents, first, BaseTime, keepCount: 2);
        await RecordAsync(unitOfWork, recents, second, BaseTime.AddMinutes(1), keepCount: 2);
        await RecordAsync(unitOfWork, recents, third, BaseTime.AddMinutes(2), keepCount: 2);

        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountRecentRowsSql)).Should().Be(2);
        var recent = await search.ListRecentAsync(1, 20, CancellationToken.None);
        recent.Select(item => item.FullName).Should().Equal("Chetan Iyer", "Bina Das");
    }

    [Fact]
    public async Task RecordViewAsync_OpeningAgain_MovesThePatientToTheTop()
    {
        await using var database = await TestDatabase.CreateAsync();
        await SetUpAsync(database);
        var (unitOfWork, patients, recents, search) = Create(database);
        var first = await AddPatientAsync(unitOfWork, patients, "Asha Rao");
        var second = await AddPatientAsync(unitOfWork, patients, "Bina Das");
        await RecordAsync(unitOfWork, recents, first, BaseTime, keepCount: 20);
        await RecordAsync(unitOfWork, recents, second, BaseTime.AddMinutes(1), keepCount: 20);

        await RecordAsync(unitOfWork, recents, first, BaseTime.AddMinutes(2), keepCount: 20);

        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountRecentRowsSql)).Should().Be(2);
        var recent = await search.ListRecentAsync(1, 20, CancellationToken.None);
        recent.Select(item => item.FullName).Should().Equal("Asha Rao", "Bina Das");
    }

    [Fact]
    public async Task ListRecentAsync_DeactivatedPatient_IsLeftOut()
    {
        await using var database = await TestDatabase.CreateAsync();
        await SetUpAsync(database);
        var (unitOfWork, patients, recents, search) = Create(database);
        var patient = await AddPatientAsync(unitOfWork, patients, "Asha Rao");
        await RecordAsync(unitOfWork, recents, patient, BaseTime, keepCount: 20);
        var auditValuesBefore = patient.AuditValues();
        patient.Deactivate();
        await unitOfWork.ExecuteAsync(
            token => patients.SaveAsync(patient, auditValuesBefore, AuditAction.Delete, 1, token),
            CancellationToken.None);

        var recent = await search.ListRecentAsync(1, 20, CancellationToken.None);

        recent.Should().BeEmpty();
    }

    private static async Task SetUpAsync(TestDatabase database)
    {
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
    }

    private static async Task<Patient> AddPatientAsync(IUnitOfWork unitOfWork, IPatientRepository patients, string fullName)
    {
        var patient = Patient.Register(fullName, new DateOnly(1990, 5, 14), 2, null, null, Today);
        await unitOfWork.ExecuteAsync(token => patients.AddAsync(patient, 1, token), CancellationToken.None);
        return patient;
    }

    private static Task RecordAsync(
        IUnitOfWork unitOfWork,
        IRecentPatientRepository recents,
        Patient patient,
        DateTime viewedUtc,
        int keepCount) =>
        unitOfWork.ExecuteAsync(
            token => recents.RecordViewAsync(1, patient.PatientId, viewedUtc, keepCount, token),
            CancellationToken.None);

    private static (IUnitOfWork UnitOfWork, IPatientRepository Patients, IRecentPatientRepository Recents, IPatientSearch Search) Create(
        TestDatabase database)
    {
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        var clock = new SystemClock();
        var audit = new SqlAuditLogWriter(clock, options);
        var connectionFactory = new SqlConnectionFactory(options);
        return (
            new SqlUnitOfWork(connectionFactory),
            new SqlPatientRepository(audit, clock, connectionFactory, options),
            new SqlRecentPatientRepository(options),
            new SqlPatientSearch(connectionFactory, options));
    }
}
