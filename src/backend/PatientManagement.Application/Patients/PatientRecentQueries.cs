namespace PatientManagement.Application.Patients;

/// <summary>Records that a user opened a patient's profile.</summary>
public sealed record RecordPatientViewedCommand(int PatientId, int UserId);

/// <summary>Lists the patients a user opened most recently.</summary>
public sealed record ListRecentPatientsQuery(int UserId);

/// <summary>How many recently opened patients each user keeps (plan Sprint 3, item 22).</summary>
public static class RecentPatientsPolicy
{
    public const int KeepCount = 20;
}
