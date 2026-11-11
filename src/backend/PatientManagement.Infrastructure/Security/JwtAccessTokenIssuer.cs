using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PatientManagement.Application.Auth;
using PatientManagement.Application.Interfaces;

namespace PatientManagement.Infrastructure.Security;

/// <summary>Issues RS256 access tokens with the user id, username and a unique token id (CLAUDE.md 10.1).</summary>
public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    public const string UsernameClaimType = "username";

    private readonly AuthOptions _options;
    private readonly SigningCredentials _credentials;

    public JwtAccessTokenIssuer(IOptions<AuthOptions> options)
    {
        _options = options.Value;

        // The key is copied into the security key, so no RSA handle is kept that a container could dispose.
        using var rsa = RSA.Create();
        rsa.ImportFromPem(_options.SigningKeyPem);
        _credentials = new SigningCredentials(
            new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: true)),
            SecurityAlgorithms.RsaSha256);
    }

    public AccessToken Issue(int userId, string username, DateTime nowUtc)
    {
        var expiresUtc = nowUtc.AddMinutes(_options.AccessTokenMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString(CultureInfo.InvariantCulture)),
                new Claim(UsernameClaimType, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            ]),
            IssuedAt = nowUtc,
            NotBefore = nowUtc,
            Expires = expiresUtc,
            SigningCredentials = _credentials,
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AccessToken(token, expiresUtc);
    }
}
