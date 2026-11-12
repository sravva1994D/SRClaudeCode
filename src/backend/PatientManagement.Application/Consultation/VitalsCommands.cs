namespace PatientManagement.Application.Consultation;

/// <summary>Records the vitals for a visit. Each value is optional; a value that was not measured is left out.</summary>
public sealed record RecordVitalsCommand(
    int VisitId,
    decimal? Temperature,
    int? BloodPressureSystolic,
    int? BloodPressureDiastolic,
    int? Pulse,
    int ChangedByUserId);

public sealed record GetVitalsQuery(int VisitId);

/// <summary>The recorded vitals as the API returns them.</summary>
public sealed record VitalsDetails(
    int VitalsId,
    int VisitId,
    decimal? Temperature,
    int? BloodPressureSystolic,
    int? BloodPressureDiastolic,
    int? Pulse)
{
    public static VitalsDetails From(Domain.Consultation.Vitals vitals) => new(
        vitals.VitalsId,
        vitals.VisitId,
        vitals.Temperature,
        vitals.BloodPressureSystolic,
        vitals.BloodPressureDiastolic,
        vitals.Pulse);
}

/// <summary>The saved vitals and the warnings for values outside their normal range (they are saved anyway).</summary>
public sealed record RecordVitalsResult(VitalsDetails Vitals, IReadOnlyList<string> Warnings);
