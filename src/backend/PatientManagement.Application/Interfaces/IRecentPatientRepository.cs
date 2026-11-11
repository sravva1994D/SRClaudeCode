namespace PatientManagement.Application.Interfaces;

/// <summary>Records which patients a user opened. Runs inside a unit of work; opening a record is not audited (Q17).</summary>
public interface IRecentPatientRepository
{
    /// <summary>
    /// Marks the patient as opened by this user at the given time, then keeps only the newest
    /// <paramref name="keepCount"/> patients for that user.
    /// </summary>
    Task RecordViewAsync(
        int userId,
        int patientId,
        DateTime viewedUtc,
        int keepCount,
        CancellationToken cancellationToken);
}
