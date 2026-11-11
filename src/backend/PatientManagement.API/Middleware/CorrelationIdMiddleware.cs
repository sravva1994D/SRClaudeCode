using System.Text.RegularExpressions;

namespace PatientManagement.API.Middleware;

/// <summary>
/// Gives each request a correlation id. A valid incoming <c>X-Correlation-Id</c> is kept so a caller's trace
/// continues; anything else is replaced, so callers cannot inject arbitrary text into logs.
/// </summary>
public sealed partial class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers[HeaderName].ToString());
        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    /// <summary>Returns the incoming id when it is 1–64 letters, digits, hyphens or underscores; otherwise a new id.</summary>
    public static string ResolveCorrelationId(string incoming) =>
        ValidIdRegex().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N");

    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidIdRegex();
}
