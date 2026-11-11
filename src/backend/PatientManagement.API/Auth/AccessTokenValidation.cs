using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using PatientManagement.Application.Auth;

namespace PatientManagement.API.Auth;

/// <summary>Validates RS256 access tokens: issuer, audience, signature, and lifetime (CLAUDE.md 10.1).</summary>
public static class AccessTokenValidation
{
    public static TokenValidationParameters Create(AuthOptions options)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(options.SigningKeyPem);

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: false)),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    }
}
