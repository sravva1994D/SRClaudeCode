using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class UnitOfWorkAndAuditTests
{
    private const string CountAuditRowsSql = "SELECT COUNT(*) FROM dbo.AuditLogs;";
    private const string UpdateAuditRowSql = "UPDATE dbo.AuditLogs SET TableName = N'Changed';";
    private const string DeleteAuditRowSql = "DELETE FROM dbo.AuditLogs;";
    private const string InsertAuditRowSql =
        "INSERT INTO dbo.AuditLogs (TableName, RecordId, Action) VALUES (N'Users', N'1', N'Create');";
    private const string NewValuesSql = "SELECT NewValues FROM dbo.AuditLogs;";

    [Fact]
    public async Task ExecuteAsync_WorkSucceeds_CommitsTheAuditRow()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        var (unitOfWork, writer) = Create(database);

        await unitOfWork.ExecuteAsync(token => writer.WriteAsync(CreateEntry(), token), CancellationToken.None);

        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountAuditRowsSql)).Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WorkThrowsAfterWriting_RollsBackTheAuditRow()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        var (unitOfWork, writer) = Create(database);

        var act = () => unitOfWork.ExecuteAsync<bool>(
            async token =>
            {
                await writer.WriteAsync(CreateEntry(), token);
                throw new InvalidOperationException("Simulated failure after the write.");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountAuditRowsSql)).Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_NestedWorkFails_RollsBackTheWholeOperation()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        var (unitOfWork, writer) = Create(database);

        var act = () => unitOfWork.ExecuteAsync(
            async outerToken =>
            {
                await writer.WriteAsync(CreateEntry(), outerToken);
                await unitOfWork.ExecuteAsync(
                    _ => Task.FromException(new InvalidOperationException("Inner step failed.")),
                    outerToken);
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountAuditRowsSql)).Should().Be(0);
    }

    [Fact]
    public async Task WriteAsync_OutsideUnitOfWork_IsRefused()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        var (_, writer) = Create(database);

        var act = () => writer.WriteAsync(CreateEntry(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task WriteAsync_UpdateEntry_StoresOnlyTheChangedFields()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        var (unitOfWork, writer) = Create(database);
        var entry = new AuditEntry(
            "Users",
            "7",
            AuditAction.Update,
            new Dictionary<string, object?> { ["FailedLoginCount"] = 0, ["Username"] = "doctor" },
            new Dictionary<string, object?> { ["FailedLoginCount"] = 1, ["Username"] = "doctor" },
            UserId: null);

        await unitOfWork.ExecuteAsync(token => writer.WriteAsync(entry, token), CancellationToken.None);

        (await SqlQuery.ScalarStringAsync(database.ConnectionString, NewValuesSql))
            .Should().Be("{\"FailedLoginCount\":1}");
    }

    [Fact]
    public async Task Update_ExistingAuditRow_IsRefused()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertAuditRowSql);

        var act = () => SqlQuery.ExecuteAsync(database.ConnectionString, UpdateAuditRowSql);

        await act.Should().ThrowAsync<SqlException>().WithMessage("*append-only*");
    }

    [Fact]
    public async Task Delete_ExistingAuditRow_IsRefused()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertAuditRowSql);

        var act = () => SqlQuery.ExecuteAsync(database.ConnectionString, DeleteAuditRowSql);

        await act.Should().ThrowAsync<SqlException>().WithMessage("*append-only*");
    }

    private static (IUnitOfWork UnitOfWork, IAuditLogWriter Writer) Create(TestDatabase database)
    {
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        var connectionFactory = new SqlConnectionFactory(options);
        return (new SqlUnitOfWork(connectionFactory), new SqlAuditLogWriter(new SystemClock(), options));
    }

    private static AuditEntry CreateEntry() => new(
        "Users",
        "1",
        AuditAction.Create,
        null,
        new Dictionary<string, object?> { ["Username"] = "doctor" },
        UserId: null);
}
