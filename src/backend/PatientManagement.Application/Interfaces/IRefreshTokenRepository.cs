using PatientManagement.Domain.Auth;

namespace PatientManagement.Application.Interfaces;

/// <summary>Stores refresh tokens by hash. Writes must run inside a unit of work and are audited.</summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);

    /// <summary>Saves a changed token, checking its row version, and audits the changed fields.</summary>
    Task SaveAsync(RefreshToken token, IReadOnlyDictionary<string, object?> auditValuesBefore, CancellationToken cancellationToken);

    /// <summary>Revokes every active token in the family with the given reason.</summary>
    Task RevokeFamilyAsync(Guid familyId, string reason, DateTime nowUtc, CancellationToken cancellationToken);

    /// <summary>Revokes every active token of the user with the given reason.</summary>
    Task RevokeAllForUserAsync(int userId, string reason, DateTime nowUtc, CancellationToken cancellationToken);
}
