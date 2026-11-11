using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Interfaces;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Milestone M2 gate (plan Sprint 3–4, BRD performance target): patient search completes within 2 seconds on the
/// synthetic 10,000-patient set. Uses synthetic rows generated in SQL, never real patient data.
/// </summary>
public sealed class PatientSearchPerformanceTests
{
    private const int SyntheticPatientCount = 10_000;
    private const long TargetMilliseconds = 2_000;

    private const string GeneratePatientsSql = """
        ;WITH numbers AS (
            SELECT TOP (10000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
            FROM sys.all_objects AS a CROSS JOIN sys.all_objects AS b
        )
        INSERT INTO dbo.Patients (FullName, DateOfBirth, GenderId, PhoneNumber, Address)
        SELECT CONCAT(N'Synthetic Patient ', i),
               DATEADD(DAY, -(i % 20000), '2000-01-01'),
               1 + i % 3,
               CONCAT('9', RIGHT(CONCAT('000000000', CAST(i AS varchar(10))), 9)),
               NULL
        FROM numbers;
        """;

    [Theory]
    [InlineData("synthetic", null)]
    [InlineData("Patient 1234", null)]
    [InlineData(null, "9000001234")]
    [InlineData(null, null)]
    public async Task SearchActiveAsync_TenThousandPatients_CompletesWithinTheTarget(string? nameTerm, string? phoneDigits)
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, GeneratePatientsSql);
        var search = CreateSearch(database);

        // The first call warms the plan cache; only the second call is timed.
        await search.SearchActiveAsync(nameTerm, phoneDigits, 1, 20, CancellationToken.None);
        var stopwatch = Stopwatch.StartNew();
        var page = await search.SearchActiveAsync(nameTerm, phoneDigits, 1, 20, CancellationToken.None);
        stopwatch.Stop();

        page.TotalCount.Should().BeGreaterThan(0, "the synthetic set must contain matches for the search");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(TargetMilliseconds);
    }

    [Fact]
    public async Task SearchActiveAsync_TenThousandPatients_CountsEveryPatient()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, GeneratePatientsSql);
        var search = CreateSearch(database);

        var page = await search.SearchActiveAsync(null, null, 500, 20, CancellationToken.None);

        page.TotalCount.Should().Be(SyntheticPatientCount);
        page.Items.Should().HaveCount(20);
    }

    private static IPatientSearch CreateSearch(TestDatabase database)
    {
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        return new SqlPatientSearch(new SqlConnectionFactory(options), options);
    }
}
