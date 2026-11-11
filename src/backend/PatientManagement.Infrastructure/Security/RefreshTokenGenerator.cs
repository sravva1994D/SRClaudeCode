using System.Security.Cryptography;
using System.Text;
using PatientManagement.Application.Interfaces;

namespace PatientManagement.Infrastructure.Security;

/// <summary>Random 256-bit refresh tokens. Only their SHA-256 hash is stored, as 64 hex characters (CLAUDE.md 10.1).</summary>
public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenByteLength = 32;

    public string CreateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenByteLength))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    public string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
