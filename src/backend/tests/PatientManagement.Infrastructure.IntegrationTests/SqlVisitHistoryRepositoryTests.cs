using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Visits;
using PatientManagement.Domain.Patients;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class SqlVisitHistoryRepositoryTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    [Fact]
    public async Task ListForPatientAsync_DateRange_IncludesBothEndDaysAndLeavesOthersOut()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var patientId = await AddPatientAsync(database);
        await InsertVisitAsync(database, patientId, "2026-11-01T00:00:00");
        await InsertVisitAsync(database, patientId, "2026-11-30T23:30:00");
        await InsertVisitAsync(database, patientId, "2026-12-01T00:10:00");
        var history = CreateRepository(database);

        var page = await history.ListForPatientAsync(
            new PatientVisitHistoryQuery(patientId, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), 1, 20),
            CancellationToken.None);

        page.TotalCount.Should().Be(2);
        page.Items.Select(item => item.StartedAt.Date).Should().Equal(
            new DateTime(2026, 11, 30),
            new DateTime(2026, 11, 1));
    }

    [Fact]
    public async Task ListForPatientAsync_NoBounds_PagesNewestFirstWithTheTotal()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var patientId = await AddPatientAsync(database);
        await InsertVisitAsync(database, patientId, "2026-10-01T09:00:00");
        await InsertVisitAsync(database, patientId, "2026-11-01T09:00:00");
        await InsertVisitAsync(database, patientId, "2026-09-01T09:00:00");
        var history = CreateRepository(database);

        var firstPage = await history.ListForPatientAsync(
            new PatientVisitHistoryQuery(patientId, null, null, 1, 2),
            CancellationToken.None);
        var secondPage = await history.ListForPatientAsync(
            new PatientVisitHistoryQuery(patientId, null, null, 2, 2),
            CancellationToken.None);

        firstPage.TotalCount.Should().Be(3);
        firstPage.Items.Select(item => item.StartedAt.Month).Should().Equal(11, 10);
        secondPage.Items.Select(item => item.StartedAt.Month).Should().Equal(9);
    }

    [Fact]
    public async Task ListForPatientAsync_InactiveVisit_IsNotListed()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var patientId = await AddPatientAsync(database);
        await InsertVisitAsync(database, patientId, "2026-11-01T09:00:00", isActive: false);
        var history = CreateRepository(database);

        var page = await history.ListForPatientAsync(
            new PatientVisitHistoryQuery(patientId, null, null, 1, 20),
            CancellationToken.None);

        page.TotalCount.Should().Be(0);
    }

    private static SqlVisitHistoryRepository CreateRepository(TestDatabase database) =>
        new(new SqlConnectionFactory(Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString })),
            Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString }));

    private static async Task<int> AddPatientAsync(TestDatabase database)
    {
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        var clock = new SystemClock();
        var audit = new SqlAuditLogWriter(clock, options);
        var connectionFactory = new SqlConnectionFactory(options);
        var unitOfWork = new SqlUnitOfWork(connectionFactory);
        var patients = new SqlPatientRepository(audit, clock, connectionFactory, options);
        var patient = Patient.Register("Asha Rao", new DateOnly(1990, 5, 14), 2, null, null, new DateOnly(2026, 11, 9));
        await unitOfWork.ExecuteAsync(token => patients.AddAsync(patient, 1, token), CancellationToken.None);
        return patient.PatientId;
    }

    /// <summary>Inserts a visit with a fixed start time. Visits made in tests need no appointment, so none is created.</summary>
    private static Task InsertVisitAsync(TestDatabase database, int patientId, string startedAt, bool isActive = true) =>
        SqlQuery.ExecuteAsync(
            database.ConnectionString,
            $"INSERT INTO dbo.Visits (PatientId, VisitStatusId, IsActive, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy) "
            + $"VALUES ({patientId}, 2, {(isActive ? 1 : 0)}, '{startedAt}', 1, '{startedAt}', 1);");
}
