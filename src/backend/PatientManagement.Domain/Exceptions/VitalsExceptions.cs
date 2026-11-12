namespace PatientManagement.Domain.Exceptions;

/// <summary>
/// A vital sign is outside its plausible range, so the value cannot be right. The API maps it to 400 (CLAUDE.md 6.5),
/// and the message names the signs without echoing the values.
/// </summary>
public sealed class VitalsImplausibleException(IReadOnlyList<string> vitalSigns)
    : DomainException("vitals_implausible", "One or more vital signs are outside the plausible range.")
{
    public IReadOnlyList<string> VitalSigns { get; } = vitalSigns;
}

/// <summary>The visit already has vitals recorded. The API maps it to 409 (CLAUDE.md 6.5).</summary>
public sealed class VitalsAlreadyRecordedException()
    : DomainException("vitals_already_recorded", "Vitals have already been recorded for this visit.")
{
}
