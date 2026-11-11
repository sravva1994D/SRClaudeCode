using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace PatientManagement.API;

/// <summary>Named rate-limit policies (CLAUDE.md 10.4). Login is limited per client address.</summary>
public static class RateLimitPolicies
{
    public const string Login = "login";

    public const int LoginPermitsPerMinute = 10;

    public static void AddPolicies(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(Login, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = LoginPermitsPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
    }
}
