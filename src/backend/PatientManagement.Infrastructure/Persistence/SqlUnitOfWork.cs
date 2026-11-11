using PatientManagement.Application.Interfaces;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Runs work in one SQL Server transaction: commit on success, rollback on any exception (CLAUDE.md 6.3).</summary>
public sealed class SqlUnitOfWork(ISqlConnectionFactory connectionFactory) : IUnitOfWork
{
    public async Task ExecuteAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken) =>
        await ExecuteAsync<bool>(
            async token =>
            {
                await work(token);
                return true;
            },
            cancellationToken);

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        // Nested calls join the outer transaction, so one failure rolls back the whole operation.
        if (UnitOfWorkScope.TryGetCurrent() is not null)
        {
            return await work(cancellationToken);
        }

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        UnitOfWorkScope.Enter(new UnitOfWorkScope(connection, transaction));
        try
        {
            var result = await work(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception)
        {
            // Rollback runs even when the caller was cancelled, so no partial change survives.
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            UnitOfWorkScope.Exit();
        }
    }
}
