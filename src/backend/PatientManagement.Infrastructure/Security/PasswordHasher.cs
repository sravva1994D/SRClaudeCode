namespace PatientManagement.Infrastructure.Security;

/// <summary>
/// bcrypt hashing (CLAUDE.md 10.2). The salt is generated per password and stored inside the hash.
/// The work factor comes from the Sprint 1 spike (documents/spike-results.md); login will read it from configuration.
/// </summary>
public static class PasswordHasher
{
    public const int DefaultWorkFactor = 12;

    public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, DefaultWorkFactor);

    public static bool Verify(string password, string passwordHash) => BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
