namespace PatientManagement.Application.Interfaces;

/// <summary>
/// Runs a write operation in one database transaction (CLAUDE.md 6.3). Commits when the work finishes and rolls back
/// when it throws. Calling it inside another unit of work joins the outer transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken);

    Task ExecuteAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken);
}
