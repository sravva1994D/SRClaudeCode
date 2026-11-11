using PatientManagement.Application.Interfaces;

namespace PatientManagement.Infrastructure.Security;

/// <summary>
/// bcrypt hashing (CLAUDE.md 10.2). The salt is generated per password and stored inside the hash. The work factor
/// (12) comes from the Sprint 1 spike (documents/spike-results.md); it moves to configuration in a later change.
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public const int WorkFactor = 12;

    // Hashed once, so a lookup for an unknown user costs the same as a real check.
    private readonly Lazy<string> _dummyHash = new(() => BCrypt.Net.BCrypt.HashPassword("dummy-password-for-timing", WorkFactor));

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash) => BCrypt.Net.BCrypt.Verify(password, passwordHash);

    public void VerifyAgainstDummy(string password) => BCrypt.Net.BCrypt.Verify(password, _dummyHash.Value);
}
