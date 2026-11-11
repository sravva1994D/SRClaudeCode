using Microsoft.Extensions.Options;
using NSubstitute;
using PatientManagement.Application.Auth;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Auth;
using PatientManagement.Domain.Users;

namespace PatientManagement.Application.UnitTests;

/// <summary>Runs the work directly. Transaction behaviour is covered by the Infrastructure integration tests.</summary>
internal sealed class InlineUnitOfWork : IUnitOfWork
{
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken) =>
        work(cancellationToken);

    public Task ExecuteAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken) =>
        work(cancellationToken);
}

internal static class AuthTestData
{
    public static readonly DateTime Now = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    public const string Password = "Passw0rd!";
    public const string PasswordHash = "hash-of-password";
    public const string PlainRefreshToken = "plain-refresh-token";
    public const string RefreshTokenHash = "hash-of-refresh-token";

    public static AuthOptions DefaultOptions() => new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenMinutes = 15,
        RefreshTokenHours = 8,
        IdleTimeoutMinutes = 30,
    };

    public static IOptions<AuthOptions> OptionsWrapper() => Options.Create(DefaultOptions());

    public static UserAccount Account(
        int failedLoginCount = 0,
        DateTime? failedLoginWindowStartUtc = null,
        DateTime? lockedUntilUtc = null) =>
        UserAccount.Reconstitute(
            userId: 1,
            username: "dev.doctor",
            passwordHash: PasswordHash,
            isActive: true,
            failedLoginCount: failedLoginCount,
            failedLoginWindowStartUtc: failedLoginWindowStartUtc,
            lockedUntilUtc: lockedUntilUtc,
            lastLoginUtc: null,
            passwordChangedUtc: null,
            rowVersion: [1]);

    public static RefreshToken Token(
        DateTime? issuedUtc = null,
        DateTime? expiresUtc = null,
        DateTime? lastUsedUtc = null,
        string? revokedReason = null) =>
        RefreshToken.Reconstitute(
            refreshTokenId: 5,
            userId: 1,
            familyId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            tokenHash: RefreshTokenHash,
            issuedUtc: issuedUtc ?? Now.AddMinutes(-1),
            expiresUtc: expiresUtc ?? Now.AddHours(7),
            lastUsedUtc: lastUsedUtc ?? Now.AddMinutes(-1),
            revokedUtc: revokedReason is null ? null : Now.AddMinutes(-1),
            revokedReason: revokedReason,
            rowVersion: [1]);
}

/// <summary>Substitutes for the security and time dependencies, set to the fixed test time.</summary>
internal sealed class AuthDependencies
{
    public IClock Clock { get; } = Substitute.For<IClock>();
    public IPasswordHasher PasswordHasher { get; } = Substitute.For<IPasswordHasher>();
    public IRefreshTokenGenerator Generator { get; } = Substitute.For<IRefreshTokenGenerator>();
    public IAccessTokenIssuer AccessTokenIssuer { get; } = Substitute.For<IAccessTokenIssuer>();
    public IUserAccountRepository Users { get; } = Substitute.For<IUserAccountRepository>();
    public IRefreshTokenRepository RefreshTokens { get; } = Substitute.For<IRefreshTokenRepository>();

    public AuthDependencies()
    {
        Clock.UtcNow.Returns(AuthTestData.Now);
        Generator.CreateToken().Returns(AuthTestData.PlainRefreshToken);
        Generator.HashToken(Arg.Any<string>()).Returns(AuthTestData.RefreshTokenHash);
        AccessTokenIssuer.Issue(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(call => new AccessToken("access-token", call.ArgAt<DateTime>(2).AddMinutes(15)));
        PasswordHasher.Verify(AuthTestData.Password, AuthTestData.PasswordHash).Returns(true);
    }
}
