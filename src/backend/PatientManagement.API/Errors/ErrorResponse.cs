namespace PatientManagement.API.Errors;

/// <summary>The one error shape returned by the API (CLAUDE.md section 6.5). Never carries stack traces or SQL text.</summary>
/// <param name="Code">Stable machine-readable code.</param>
/// <param name="Message">Message that is safe to show to the user.</param>
/// <param name="CorrelationId">Matches the value in the response header and the server logs.</param>
public sealed record ErrorResponse(string Code, string Message, string CorrelationId);
