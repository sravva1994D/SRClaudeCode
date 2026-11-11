using PatientManagement.Domain.Users;

namespace PatientManagement.Application.Interfaces;

/// <summary>Reads and saves the doctor account. Saves must run inside a unit of work.</summary>
public interface IUserAccountRepository
{
    /// <summary>Finds an active account by username. Returns null when there is none.</summary>
    Task<UserAccount?> FindActiveByUsernameAsync(string username, CancellationToken cancellationToken);

    /// <summary>Finds an active account by id. Returns null when there is none.</summary>
    Task<UserAccount?> FindActiveByIdAsync(int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the account, checking its row version, and writes one audit row with the changed fields.
    /// <paramref name="auditValuesBefore"/> is the account's <see cref="UserAccount.AuditValues"/> before the change.
    /// </summary>
    Task SaveAsync(
        UserAccount account,
        IReadOnlyDictionary<string, object?> auditValuesBefore,
        int? changedByUserId,
        CancellationToken cancellationToken);
}
