namespace PatientManagement.Application.Interfaces;

/// <summary>Hashes and checks passwords (bcrypt, CLAUDE.md 10.2). Plain-text passwords are never stored or logged.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);

    /// <summary>Spends the same time as a real check. Used when the username does not exist, so timing does not reveal it.</summary>
    void VerifyAgainstDummy(string password);
}

/// <summary>A signed access token and when it expires.</summary>
public sealed record AccessToken(string Value, DateTime ExpiresUtc);

/// <summary>Issues RS256 access tokens (CLAUDE.md 10.1).</summary>
public interface IAccessTokenIssuer
{
    AccessToken Issue(int userId, string username, DateTime nowUtc);
}

/// <summary>Creates refresh tokens and their hashes (CLAUDE.md 10.1).</summary>
public interface IRefreshTokenGenerator
{
    /// <summary>A new random token to send to the client once. Only its hash is stored.</summary>
    string CreateToken();

    string HashToken(string token);
}
