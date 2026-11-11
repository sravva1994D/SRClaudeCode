using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Auth;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Users;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class AuthRepositoryTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    private const string CountRefreshAuditRowsSql =
        "SELECT COUNT(*) FROM dbo.AuditLogs WHERE TableName = N'RefreshTokens';";

    private const string CountActiveTokensSql =
        "SELECT COUNT(*) FROM dbo.RefreshTokens WHERE RevokedDate IS NULL;";

    private const string RevokedReasonSql = "SELECT RevokedReason FROM dbo.RefreshTokens WHERE RefreshTokenId = 1;";

    [Fact]
    public async Task SaveAsync_StaleRowVersion_ThrowsConcurrencyConflict()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var (unitOfWork, users, _) = Create(database);
        var first = await LoadAsync(unitOfWork, users);
        var stale = await LoadAsync(unitOfWork, users);
        await unitOfWork.ExecuteAsync(
            token => users.SaveAsync(first, first.AuditValues(), null, token),
            CancellationToken.None);

        var act = () => unitOfWork.ExecuteAsync(
            token => users.SaveAsync(stale, stale.AuditValues(), null, token),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task AddAsync_NewRefreshToken_WritesACreateAuditRow()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var (unitOfWork, _, tokens) = Create(database);
        var token = RefreshToken.Issue(1, Guid.NewGuid(), new string('a', 64), DateTime.UtcNow, DateTime.UtcNow.AddHours(8));

        await unitOfWork.ExecuteAsync(t => tokens.AddAsync(token, t), CancellationToken.None);

        token.RefreshTokenId.Should().BeGreaterThan(0);
        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountRefreshAuditRowsSql)).Should().Be(1);
    }

    [Fact]
    public async Task RevokeFamilyAsync_ActiveTokensOfTheFamily_RevokesThemWithAuditRows()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
        var (unitOfWork, _, tokens) = Create(database);
        var family = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var token = RefreshToken.Issue(1, family, new string('b', 64), now, now.AddHours(8));
        await unitOfWork.ExecuteAsync(t => tokens.AddAsync(token, t), CancellationToken.None);

        await unitOfWork.ExecuteAsync(
            t => tokens.RevokeFamilyAsync(family, RefreshToken.RevocationReasons.ReuseDetected, now, t),
            CancellationToken.None);

        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountActiveTokensSql)).Should().Be(0);
        (await SqlQuery.ScalarStringAsync(database.ConnectionString, RevokedReasonSql)).Should().Be("reuse_detected");
        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountRefreshAuditRowsSql)).Should().Be(2);
    }

    private static async Task<UserAccount> LoadAsync(IUnitOfWork unitOfWork, IUserAccountRepository users) =>
        await unitOfWork.ExecuteAsync(
            token => users.FindActiveByIdAsync(1, token),
            CancellationToken.None) ?? throw new InvalidOperationException("The test user is missing.");

    private static (IUnitOfWork UnitOfWork, IUserAccountRepository Users, IRefreshTokenRepository Tokens) Create(
        TestDatabase database)
    {
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        var clock = new SystemClock();
        var audit = new SqlAuditLogWriter(clock, options);
        return (
            new SqlUnitOfWork(new SqlConnectionFactory(options)),
            new SqlUserAccountRepository(audit, clock, options),
            new SqlRefreshTokenRepository(audit, clock, options));
    }
}
