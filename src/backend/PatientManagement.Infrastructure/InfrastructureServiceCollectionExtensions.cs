using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PatientManagement.Application.Auth;
using PatientManagement.Application.Interfaces;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.Health;
using PatientManagement.Infrastructure.Migrations;
using PatientManagement.Infrastructure.Persistence;
using PatientManagement.Infrastructure.Security;

namespace PatientManagement.Infrastructure;

/// <summary>Registers Infrastructure services (CLAUDE.md section 6.9).</summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Database:ConnectionString is required.")
            .ValidateOnStart();

        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Issuer) && !string.IsNullOrWhiteSpace(options.Audience),
                "Auth:Issuer and Auth:Audience are required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.SigningKeyPem),
                "Auth:SigningKeyPem is required. Set it with user secrets in development.")
            .ValidateOnStart();

        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddSingleton<MigrationRunner>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        services.AddScoped<IUnitOfWork, SqlUnitOfWork>();
        services.AddScoped<IAuditLogWriter, SqlAuditLogWriter>();
        services.AddScoped<IUserAccountRepository, SqlUserAccountRepository>();
        services.AddScoped<IRefreshTokenRepository, SqlRefreshTokenRepository>();
        services.AddScoped<IClinicProfileRepository, SqlClinicProfileRepository>();

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
        return services;
    }
}
